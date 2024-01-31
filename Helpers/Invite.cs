
using Azure.Communication.Email;
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

    public static async Task SendSuccessfullyVerifiedAsync(IConfiguration configuration, HttpRequest request, UsersCache usersCache)
    {
        var emailClient = new EmailClient(configuration.GetSection("AppSettings:EmailConnectionString").Value);

        var subject = "Your identity successfully verified";
        var htmlContent = $"<html><body><h1>Thank you</h1><br/><p>Thank you {usersCache.UniqueName}! Your identity successfully verified.</p></body></html>";
        var sender = "donotreply@woodgrovedemo.com";

        // Email to the employee
        EmailSendOperation emailSendOperation = await emailClient.SendAsync(
            Azure.WaitUntil.Completed,
            sender,
            usersCache.EmployeeEmail,
            subject,
            htmlContent);

        subject = "Action required: Complete new hire onboarding";
        var link = request.Scheme + "://" + request.Host + "/users";
        htmlContent = $"<html><body><h1>Send your new employee a access pass</h1><br/><p>Please use <a href='{link}'>this link</a> and follow the guidance.</p></body></html>";
        // Email to the manager

    }

}