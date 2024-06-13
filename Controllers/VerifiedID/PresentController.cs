using System.Net;
using System.Text.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Woodgrove.Onboarding.Helpers;
using Woodgrove.Onboarding.Models;
using System.Net.Http.Headers;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Woodgrove.Onboarding.Controllers;
using Woodgrove.Onboarding.Pages;
using Microsoft.Identity.VerifiedID.Presentation;
using Microsoft.Identity.VerifiedID;

namespace Woodgrove.Onboarding.Controllers;

[ApiController]
[Route("[controller]")]
public class PresentController : ControllerBase
{

    protected readonly IConfiguration _Configuration;
    protected TelemetryClient _Telemetry;
    protected IMemoryCache _Cache;
    protected Settings _Settings { get; set; }
    protected readonly IHttpClientFactory _HttpClientFactory;
    public ResponseToClient _Response { get; set; } = new ResponseToClient();

    public PresentController(TelemetryClient telemetry, IConfiguration configuration, IMemoryCache cache, IHttpClientFactory httpClientFactory)
    {
        _Configuration = configuration;
        _Cache = cache;
        _Telemetry = telemetry;
        _HttpClientFactory = httpClientFactory;

        // Load the settings of this demo
        _Settings = new Settings(configuration);
    }

    [AllowAnonymous]
    [HttpGet("/api/Present")]

    public async Task<ResponseToClient> Get(string token)
    {
        // Clear session
        this.HttpContext.Session.Clear();

        // Initiate the status object
        UserFlowStatus status = new UserFlowStatus("IdTokenHint", "Present");
        UsersCache usersCache = null;

        try
        {
            usersCache = OnboardingModel.TokenValidation(_Configuration, token, _Cache);
        }
        catch (Exception ex)
        {
            _Response.ErrorMessage = "Invalid request";
            _Response.ErrorUserMessage = ex.Message;
            return _Response;
        }

        try
        {
            // Create a presentation request object
            PresentationRequest request = RequestHelper.CreatePresentationRequest(_Settings, this.Request, usersCache.ID);

            // Serialize the request object to JSON HTML format
            _Response.RequestPayload = request.ToHtml();

            // Prepare the HTTP request with the Bearer access token and the request body
            var client = _HttpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await MsalAccessTokenHandler.AcquireToken(_Settings, _Cache));

            // Call the Microsoft Entra ID request endpoint        
            HttpResponseMessage res = await client.PostAsync(
                _Settings.RequestUrl,
                new StringContent(request.ToString(), Encoding.UTF8, "application/json"));

            _Response.ResponseBody = await res.Content.ReadAsStringAsync();
            HttpStatusCode statusCode = res.StatusCode;

            if (statusCode == HttpStatusCode.Created)
            {
                PresentationResponse presentationResponse = PresentationResponse.Parse(_Response.ResponseBody);
                _Response.ResponseBody = presentationResponse.ToHtml();

                _Response.QrCodeUrl = presentationResponse.URL;

                // Add the state ID to the user's session object 
                this.HttpContext.Session.SetString("state", request.Callback.State);

                // Add the global cache with the request status
                status.RequestStateId = request.Callback.State;
                status.RequestStatus = UserFlowStatusCodes.REQUEST_CREATED;
                status.AddHistory(status.RequestStatus, status.CalculateExecutionTime());

                // Send telemetry from this web app to Application Insights.
                AppInsightsHelper.TrackApi(_Telemetry, this.Request, status);

                // Add the status object to the cache
                _Cache.Set(request.Callback.State, status.ToString(), DateTimeOffset.Now.AddMinutes(Settings.CACHE_EXPIRES_IN_MINUTES));
            }
            else
            {
                AppInsightsHelper.TrackError(_Telemetry, this.Request, UserMessages.ERROR_API_ERROR, _Response.ResponseBody);
                _Response.ErrorMessage = _Response.ResponseBody;
                _Response.ErrorUserMessage = ResponseError.Parse(_Response.ResponseBody).GetUserMessage();
            }
        }
        catch (Exception ex)
        {
            AppInsightsHelper.TrackError(_Telemetry, this.Request, ex);
            _Response.ErrorMessage = ex.Message;
        }

        return _Response;
    }

}