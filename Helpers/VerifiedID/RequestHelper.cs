
using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using WoodgroveDemo.Models;
using WoodgroveDemo.Models.Presentation;

namespace WoodgroveDemo.Helpers;

public class RequestHelper
{
    public static string GetRequestHostName(HttpRequest request)
    {
        string scheme = "https";// : this.Request.Scheme;
        string originalHost = request.Headers["x-original-host"];
        string hostname = "";
        if (!string.IsNullOrEmpty(originalHost))
            hostname = string.Format("{0}://{1}", scheme, originalHost);
        else hostname = string.Format("{0}://{1}", scheme, request.Host);
        return hostname;
    }

    public static bool IsMobile(HttpRequest request)
    {
        string userAgent = request.Headers["User-Agent"];
        return (userAgent.Contains("Android") || userAgent.Contains("iPhone"));
    }

    /// <summary>
    /// This method creates a presentation request object instance
    /// </summary>
    /// <param name="settings">The app settings configuration object</param>
    /// <param name="httpRequest">The HTTP request</param>
    /// <param name="acceptedIssuers">Array of accepted issuers</param>
    /// <returns>PresentationRequest</returns>
    public static PresentationRequest CreatePresentationRequest(
            Settings settings,
            HttpRequest httpRequest,
            string state,
            string[] acceptedIssuers = null,
            bool faceCheck = false)
    {
        PresentationRequest request = new PresentationRequest()
        {
            includeQRCode = settings.UX.IncludeQRCode,
            authority = settings.EntraID.DidAuthority,
            registration = new Models.Presentation.Registration()
            {
                clientName = settings.UX.ClientName,
                purpose = settings.UX.Purpose
            },
            callback = new Models.Presentation.Callback()
            {
                url = settings.Api.URL(httpRequest),
                state = state + "|" +  Guid.NewGuid().ToString(),
                headers = new Dictionary<string, string>() { { "api-key", settings.Api.ApiKey } }
            },
            includeReceipt = settings.UX.IncludeReceipt,
            requestedCredentials = new List<RequestedCredential>(),
        };
        if ("" == request.registration.purpose)
        {
            request.registration.purpose = null;
        }

        List<string> okIssuers;
        if (acceptedIssuers == null)
        {
            okIssuers = new List<string>(settings.EntraID.AcceptedIssuers.Split(","));
        }
        else
        {
            okIssuers = new List<string>(acceptedIssuers);
        }
        bool allowRevoked = settings.UX.AllowRevoked;
        bool validateLinkedDomain = settings.UX.ValidateLinkedDomain;
        AddRequestedCredential(request, settings.CredentialType, okIssuers, allowRevoked, validateLinkedDomain, faceCheck);
        return request;
    }

    private static PresentationRequest AddRequestedCredential(PresentationRequest request,
                    string credentialType,
                    List<string> acceptedIssuers,
                    bool allowRevoked = false,
                    bool validateLinkedDomain = true,
                    bool faceCheck = false)
    {
        request.requestedCredentials.Add(new RequestedCredential()
        {
            type = credentialType,
            acceptedIssuers = (null == acceptedIssuers ? new List<string>() : acceptedIssuers),
            configuration = new Configuration()
            {
                validation = new Validation()
                {
                    allowRevoked = allowRevoked,
                    validateLinkedDomain = validateLinkedDomain
                }
            }
        });

        // Face check validation
        if (faceCheck)
        {
            // Receipt is not supported while doing faceCheck
            request.includeReceipt = false;

            request.requestedCredentials[0].configuration.validation.faceCheck = new FaceCheck()
            {
                sourcePhotoClaimName = "photo",
                matchConfidenceThreshold = 70
            };
        }



        return request;
    }
}