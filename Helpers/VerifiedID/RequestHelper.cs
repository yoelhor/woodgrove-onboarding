using Microsoft.Identity.VerifiedID;
using Microsoft.Identity.VerifiedID.Presentation;
using Woodgrove.Onboarding.Models;

namespace Woodgrove.Onboarding.Helpers;

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
            string state)
    {
        PresentationRequest request = new PresentationRequest()
        {
            IncludeQRCode = settings.UX.IncludeQRCode,
            Authority = settings.EntraID.DidAuthority,
            Registration = new Microsoft.Identity.VerifiedID.Presentation.RequestRegistration()
            {
                ClientName = settings.UX.ClientName,
                Purpose = settings.UX.Purpose
            },
            Callback = new CallbackDefinition()
            {
                Url = settings.Api.URL(httpRequest),
                State = state + "|" + Guid.NewGuid().ToString(),
                Headers = new Dictionary<string, string>() { { "api-key", settings.Api.ApiKey } }
            },
            IncludeReceipt = settings.UX.IncludeReceipt,
            RequestedCredentials = new List<RequestedCredential>(),
        };
        if ("" == request.Registration.Purpose)
        {
            request.Registration.Purpose = null;
        }

        List<string> okIssuers = new List<string>(settings.EntraID.AcceptedIssuers.Split(","));

        bool allowRevoked = settings.UX.AllowRevoked;
        bool validateLinkedDomain = settings.UX.ValidateLinkedDomain;
        AddRequestedCredential(request, settings.CredentialType, okIssuers, allowRevoked, validateLinkedDomain);
        return request;
    }

    private static PresentationRequest AddRequestedCredential(PresentationRequest request,
                    string credentialType,
                    List<string> acceptedIssuers,
                    bool allowRevoked = false,
                    bool validateLinkedDomain = true)
    {
        request.RequestedCredentials.Add(new RequestedCredential()
        {
            Type = credentialType,
            AcceptedIssuers = (null == acceptedIssuers ? new List<string>() : acceptedIssuers),
            Configuration = new Configuration()
            {
                Validation = new Validation()
                {
                    AllowRevoked = allowRevoked,
                    ValidateLinkedDomain = validateLinkedDomain
                }
            }
        });

        // Face check validation
        // request.requestedCredentials[0].configuration.validation.faceCheck = new FaceCheck()
        // {
        //     sourcePhotoClaimName = "photo",
        //     matchConfidenceThreshold = 2
        // };

        return request;
    }
}