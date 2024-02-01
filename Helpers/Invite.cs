
using Azure.Communication.Email;
using Microsoft.Extensions.Caching.Memory;
using woodgrove_portal.Controllers;

namespace woodgrove_portal.Helpers;


public class Invite
{
    public static async Task SendInviteAsync(IConfiguration configuration, HttpRequest request, string email)
    {
        var emailClient = new EmailClient(configuration.GetSection("AppSettings:EmailConnectionString").Value);

        var subject = "Welcome new employee";
        var link = request.Scheme + "://" + request.Host + "/onboarding";
        var htmlContent = $"<html><body><h1>Welcome aboard</h1><br/><p>A big congratulations on your new role! On behalf of the members and supervisors, we would like to welcome you to the team.</p><h2>Create your account</h2><p> To create your account, we need you to identify yourself. Please use <a href='{link}'>this link</a> and follow the guidance.</p></body></html>";
        var sender = "donotreply@woodgrovedemo.com";

        EmailSendOperation emailSendOperation = await emailClient.SendAsync(
            Azure.WaitUntil.Completed,
            sender,
            email,
            subject,
            htmlContent);
    }

    public static async Task SendTapAsync(IConfiguration configuration, HttpRequest request, string email, string tap)
    {
        var emailClient = new EmailClient(configuration.GetSection("AppSettings:EmailConnectionString").Value);

        var subject = "Welcome new employee";
        var link = request.Scheme + "://" + request.Host + "/onboarding";
        var htmlContent = @$"<html><body>
            <h1>Welcome aboard</h1>
            <p>A big congratulations on your new role! On behalf of the members and supervisors, we would like to welcome you to the team.</p>
            <h2>You Temporary Access Pass</h2>
            <p>The Temporary Access Pass is a time-limited passcode that you use to sign-in and then set up your password. User the following Temporary Access Pass</p>
            
            <h2>Next steps</h2>
            <ol>
                <li><a href='https://aka.ms/sspr'>Reset your password</a>. On the password reset page use the following access pass <code>{tap}</code></li>
                <li>Go to <a href='https://www.microsoft365.com'>M365 portal</a> and upload a photo of yourself.</li>
                <li>In <a href='https://myaccount.microsoft.com'>MyAccount</a>, issue yourself a VerifiedEmployee Verified ID credential.</li>
                <li>Go to <a href='https://myapplications.microsoft.com'>MyApplications</a> to find applications to use.</li>
            <ol>
            </body></html>";

        var sender = "donotreply@woodgrovedemo.com";

        EmailSendOperation emailSendOperation = await emailClient.SendAsync(
            Azure.WaitUntil.Completed,
            sender,
            email,
            subject,
            htmlContent);
    }

    public static async Task SendSuccessfullyVerifiedAsync(IConfiguration configuration, HttpRequest request, UsersCache usersCache, IMemoryCache cache)
    {
        var emailClient = new EmailClient(configuration.GetSection("AppSettings:EmailConnectionString").Value);
        var sender = "donotreply@woodgrovedemo.com";

        try
        {
            // Email to the employee
            var subject = "Your identity successfully verified";
            var htmlContent = @$"<html><body>
            <h1>Thank you</h1>
            <p>Thank you {usersCache.DisplayName}! Your identity successfully verified. 
            Your manager will contact you soon and provide you with guidance how to proceed</p>
            </body></html>";

            EmailSendOperation emailSendOperation1 = await emailClient.SendAsync(
                Azure.WaitUntil.Completed,
                sender,
                usersCache.EmployeeEmail,
                subject,
                htmlContent);
        }
        catch (System.Exception ex)
        {
            usersCache.Error = $"Cannot send email to employee ({usersCache.EmployeeEmail}). Reason: {ex.Message}";
            usersCache.StatusTime = DateTime.UtcNow;
            cache.Set(usersCache.UniqueID, usersCache.ToString(), DateTimeOffset.Now.AddHours(24));
        }

        // Email to the manager
        try
        {
            var subject = "Action required: Complete new hire onboarding";
            var link = request.Scheme + "://" + request.Host + "/users";
            var htmlContent = @$"<html><body>
                    <h1>Send your new employee a access pass</h1>
                    <p>Your new employee {usersCache.DisplayName} identity successfully verified.
                    Please use <a href='{link}'>this link</a> and follow the guidance.</p>
                </body></html>";

            EmailSendOperation emailSendOperation2 = await emailClient.SendAsync(
                Azure.WaitUntil.Completed,
                sender,
                usersCache.ManagerEmail,
                subject,
                htmlContent);
        }
        catch (System.Exception ex)
        {
            usersCache.Error = $"Cannot send email to manager ({usersCache.ManagerEmail}). Reason: {ex.Message}";
            usersCache.StatusTime = DateTime.UtcNow;
            cache.Set(usersCache.UniqueID, usersCache.ToString(), DateTimeOffset.Now.AddHours(24));
        }

    }

}