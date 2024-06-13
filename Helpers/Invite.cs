
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Azure.Communication.Email;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Woodgrove.Onboarding.Controllers;
using Woodgrove.Onboarding.Helpers;

namespace Woodgrove.Onboarding.Helpers;


public class Invite
{
    public static async Task<string> SendInviteAsync(IConfiguration configuration, HttpRequest request, string oid, string displayName, string email, string session)
    {
        var emailClient = new EmailClient(configuration.GetSection("AppSettings:EmailConnectionString").Value);

        var subject = "Welcome new employee";
        var link = request.Scheme + "://" + request.Host + "/onboarding?token=" + GenerateJwtToken(configuration, oid, session);
        var htmlContent = @$"<html><body>
                <h1>Welcome aboard</h1>
                <p>A big congratulations <b>{displayName}</b> on your new role! On behalf of the members and supervisors, we would like to welcome you to the team.</p>
                <h2>Create your account</h2>
                <p>Dear {displayName}, to create your account, we need you to identify yourself. 
                    Please use <a href='{link}'>this link</a> and follow the guidance.</p>
            </body></html>";
        
        var sender = configuration.GetSection("AppSettings:EmailSender").Value;

        EmailSendOperation emailSendOperation = await emailClient.SendAsync(
            Azure.WaitUntil.Started,
            sender,
            email,
            subject,
            htmlContent);

        return link;
    }

    private static string GenerateJwtToken(IConfiguration configuration, string oid, string session)
    {
        // generate token that is valid for 7 days
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("id", oid), new Claim("session", session) }),
            Issuer = "https://woodgrove.com",
            Expires = DateTime.UtcNow.AddHours(24),
            SigningCredentials = new X509SigningCredentials(MsalAccessTokenHandler.ReadCertificate(configuration.GetSection("AzureAd:ClientCertificates:0:CertificateThumbprint").Value))
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public static async Task SendTapAsync(IConfiguration configuration, HttpRequest request, string upn, string email, string tap)
    {
        var emailClient = new EmailClient(configuration.GetSection("AppSettings:EmailConnectionString").Value);

        var subject = "Welcome new employee";
        var link = request.Scheme + "://" + request.Host + "/onboarding";
        var htmlContent = @$"<html><body>
            <h1>Welcome aboard</h1>
            <p>A big congratulations on your new role! On behalf of the members and supervisors, we would like to welcome you to the team.</p>
            <h2>Your Temporary Access Pass</h2>
            <p>The Temporary Access Pass is a time-limited passcode that you use to sign-in and then set up your password. User the following Temporary Access Pass</p>
            
            <h2>Next steps</h2>
            <ol>
                <li><a href='https://aka.ms/sspr'>Reset your password</a>. On the password reset page use the following username <code>{upn}</code></li>
                <li>You temporary access pass <code>{tap}</code></li>
                <li>Go to <a href='https://www.microsoft365.com'>M365 portal</a> and upload a photo of yourself.</li>
                <li>In <a href='https://myaccount.microsoft.com'>MyAccount</a>, issue yourself a VerifiedEmployee Verified ID credential.</li>
                <li>Go to <a href='https://myapplications.microsoft.com'>MyApplications</a> to find applications to use.</li>
            <ol>
            </body></html>";

        var sender = configuration.GetSection("AppSettings:EmailSender").Value;

        EmailSendOperation emailSendOperation = await emailClient.SendAsync(
            Azure.WaitUntil.Started,
            sender,
            email,
            subject,
            htmlContent);
    }

    // public static async Task SendSuccessfullyVerifiedAsync(IConfiguration configuration, HttpRequest request, UsersCache usersCache, IMemoryCache cache)
    // {
    //     var emailClient = new EmailClient(configuration.GetSection("AppSettings:EmailConnectionString").Value);
    //     var sender = configuration.GetSection("AppSettings:EmailSender").Value;;

    //     try
    //     {
    //         // Email to the employee
    //         var subject = "Your identity successfully verified";
    //         var htmlContent = @$"<html><body>
    //         <h1>Thank you</h1>
    //         <p>Thank you {usersCache.DisplayName}! Your identity successfully verified. 
    //         Your manager will contact you soon and provide you with guidance how to proceed</p>
    //         </body></html>";

    //         EmailSendOperation emailSendOperation1 = await emailClient.SendAsync(
    //             Azure.WaitUntil.Started,
    //             sender,
    //             usersCache.Email,
    //             subject,
    //             htmlContent);
    //     }
    //     catch (System.Exception ex)
    //     {
    //         usersCache.Error = $"Cannot send email to employee ({usersCache.Email}). Reason: {ex.Message}";
    //         usersCache.StatusTime = DateTime.UtcNow;
    //         cache.Set(usersCache.ID, usersCache.ToString(), DateTimeOffset.Now.AddHours(24));
    //     }

    //     // Email to the manager
    //     try
    //     {
    //         var subject = "Action required: Complete new hire onboarding";
    //         var link = request.Scheme + "://" + request.Host + "/users";
    //         var htmlContent = @$"<html><body>
    //                 <h1>Send your new employee a access pass</h1>
    //                 <p>Your new employee {usersCache.DisplayName} identity successfully verified.
    //                 Please use <a href='{link}'>this link</a> and follow the guidance.</p>
    //             </body></html>";

    //         EmailSendOperation emailSendOperation2 = await emailClient.SendAsync(
    //             Azure.WaitUntil.Started,
    //             sender,
    //             usersCache.ManagerEmail,
    //             subject,
    //             htmlContent);
    //     }
    //     catch (System.Exception ex)
    //     {
    //         usersCache.Error = $"Cannot send email to manager ({usersCache.ManagerEmail}). Reason: {ex.Message}";
    //         usersCache.StatusTime = DateTime.UtcNow;
    //         cache.Set(usersCache.ID, usersCache.ToString(), DateTimeOffset.Now.AddHours(24));
    //     }

    // }

}