
using Azure.Communication.Email;

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

}