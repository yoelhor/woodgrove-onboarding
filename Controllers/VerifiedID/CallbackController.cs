using System.Net;
using System.Text.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WoodgroveDemo.Helpers;
using WoodgroveDemo.Models;
using Microsoft.Extensions.Logging;

namespace WoodgroveDemo.Controllers;

[ApiController]
[Route("[controller]")]
public class CallbackController : ControllerBase
{

    private readonly IConfiguration _configuration;
    private TelemetryClient _telemetry;
    private IMemoryCache _cache;
    protected readonly ILogger<CallbackController> _log;


    public CallbackController(TelemetryClient telemetry, IConfiguration configuration, IMemoryCache cache, ILogger<CallbackController> log)
    {
        _configuration = configuration;
        _cache = cache;
        _telemetry = telemetry;
        _log = log;
    }

    [AllowAnonymous]
    [HttpPost("/api/callback")]
    public async Task<ActionResult> HandleRequestCallback()
    {

        // Local variables
        EventTelemetry eventTelemetry = new EventTelemetry("Callback");
        bool rc = false;
        List<string> presentationStatus = new List<string>() { "request_retrieved", "presentation_verified", "presentation_error" };
        List<string> issuanceStatus = new List<string>() { "request_retrieved", "issuance_successful", "issuance_error" };
        List<string> selfieStatus = new List<string>() { "selfie_taken" };

        string state = "abcd", flow = "", body = "";

        try
        {
            // Get the requst body
            body = await new System.IO.StreamReader(this.Request.Body).ReadToEndAsync();

            _log.LogTrace("Reqeust body: " + body);

            // Parse the request body
            CallbackEvent callback = CallbackEvent.Parse(body);
            state = callback.state;

            // This endpoint is called by Microsoft Entra Verified ID which passes an API key.
            // Validate that the API key is valid.
            this.Request.Headers.TryGetValue("api-key", out var apiKey);
            if (_configuration["VerifiedID:ApiKey"] != apiKey)
            {
                return ErrorHandling(eventTelemetry, "Api-key wrong or missing", true, callback.state, callback.requestStatus);
            }

            // Add telemetry to the application insights
            eventTelemetry.Properties.Add("State", callback.state);
            eventTelemetry.Properties.Add("RequestId", callback.requestId);
            eventTelemetry.Properties.Add("RequestStatus", callback.requestStatus);

            // Get the current status from the cache and add the flow telemetry
            Status currentStatus = new Status();
            if (_cache.TryGetValue(callback.state, out string requestState))
            {
                currentStatus = Status.Parse(requestState);
                flow = currentStatus.Flow;
                eventTelemetry.Properties.Add("Type", "Callback");
                eventTelemetry.Properties.Add("Scenario", currentStatus.Scenario);
                eventTelemetry.Properties.Add("Action", currentStatus.Flow);
                eventTelemetry.Properties.Add("ExecutionTime", currentStatus.CalculateExecutionTime());
                eventTelemetry.Properties.Add("ExecutionSeconds", currentStatus.CalculateExecutionSeconds().ToString());
            }

            // Handle issuance, presentation adn selfie requests
            if (
                (presentationStatus.Contains(callback.requestStatus))
                || (issuanceStatus.Contains(callback.requestStatus))
                || selfieStatus.Contains(callback.requestStatus))
            {

                // Set the request status object into the global cache using the state ID key
                Status status = new Status()
                {
                    RequestStateId = callback.state,
                    RequestStatus = callback.requestStatus,
                    StartTime = currentStatus.StartTime,
                    JsonPayload = body,
                    Scenario = currentStatus.Scenario,
                    Flow = currentStatus.Flow
                };

                // Track the execution history
                status.History = currentStatus.History;
                status.AddHistory(callback.requestStatus, currentStatus.CalculateExecutionTime(), body);

                // Add the indexed claim value to search and revoke the credential
                // Note, this code is relevant only to the gift card demo
                if (callback.requestStatus == Constants.RequestStatus.PRESENTATION_VERIFIED &&
                    callback.verifiedCredentialsData.Count == 1 && callback.verifiedCredentialsData[0].type.Contains(_configuration.GetSection("VerifiedID:RevokeCredentialsDemo:Type").Value))
                {
                    status.IndexedClaimValue = callback.verifiedCredentialsData[0].claims.id;

                    // In every Microsoft issued verifiable credential, there's a claim called credential Status indicates whether the credential is revoked.
                    // But to avoid a case where a user may reuse the card before Entra ID manages to complete the revolution, check its itnernal (in this app) status using the cache object
                    _cache.TryGetValue(status.IndexedClaimValue, out string credentialAppStatus);

                    if (credentialAppStatus == "revoked")
                    {
                        status.Message = "Certificate validation failed (Woodgrove app)";
                        status.RequestStatus = "presentation_error";
                        status.JsonPayload = "{ \"requestStatus\": \"presentation_error\", \"error\": { \"code\": \"tokenError\", \"message\": \"The presented verifiable credential with jti is revoked.\"}}";
                    }
                }

                // Add the status object to the cheace
                _cache.Set(callback.state, status.ToString(), DateTimeOffset.Now.AddMinutes(Constants.AppSettings.CACHE_EXPIRES_IN_MINUTES));

                // Add the error message to the telemetry
                if (callback.requestStatus.Contains("_error"))
                {
                    this.TrackError(eventTelemetry, body, false);
                }

                _telemetry.TrackEvent(eventTelemetry);

                rc = true;
            }
            else
            {
                return ErrorHandling(eventTelemetry, $"Unknown request status '{callback.requestStatus}'", false, callback.state, callback.requestStatus);
            }

            if (!rc)
            {
                return ErrorHandling(eventTelemetry, body, false, callback.state, callback.requestStatus);
            }

            return new OkResult();
        }
        catch (Exception ex)
        {
            return ErrorHandling(eventTelemetry, ex.Message, true, state);
        }
    }

    private BadRequestObjectResult ErrorHandling(EventTelemetry eventTelemetry, string errorMessage, bool internl, string state, string requestStatus = "")
    {

        // Track the error 
        TrackError(eventTelemetry, errorMessage, internl);

        // Set the request status object into the global cache using the state ID key
        Status status = new Status()
        {
            RequestStateId = state,
            RequestStatus = requestStatus,
            JsonPayload = errorMessage
        };

        // Add the error to the cache, so we can show it in the UI
        _cache.Set(state, status.ToString(), DateTimeOffset.Now.AddMinutes(Constants.AppSettings.CACHE_EXPIRES_IN_MINUTES));

        // Return bad reqeust HTTP error message to the caller
        return BadRequest(new { error = "400", error_description = errorMessage });
    }

    /// <summary>
    /// Track the error message into application insights
    /// </summary>
    /// <param name="eventTelemetry"></param>
    /// <param name="errorMessage"></param>
    /// <param name="internl"></param>
    private void TrackError(EventTelemetry eventTelemetry, string errorMessage, bool internl)
    {
        string ErrorName = string.Empty;

        // Check the type of the error
        if (internl)
            ErrorName = Constants.ErrorMessages.API_CALLBACK_INTERANL_ERROR;
        else
            ErrorName = Constants.ErrorMessages.API_CALLBACK_ENTRA_ERROR;

        // Create an exception telemetry with the error name
        ExceptionTelemetry expTelemetry = new ExceptionTelemetry(new Exception(ErrorName));

        // Add the body of the error
        expTelemetry.Properties.Add("Error", errorMessage);

        // Add the other properties we already collected
        foreach (var item in eventTelemetry.Properties)
        {
            expTelemetry.Properties.Add(item.Key, item.Value);
        }

        // Finally track the exception
        this._telemetry.TrackException(expTelemetry);
    }

}