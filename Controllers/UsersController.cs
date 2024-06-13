using System.Dynamic;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Web;
using Azure;
using Azure.Communication.Email;
using Azure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Woodgrove.Onboarding.Helpers;
using Woodgrove.Onboarding.Models;

namespace Woodgrove.Onboarding.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private IMemoryCache _cache;
    private readonly ILogger<UsersController> _logger;
    private readonly GraphServiceClient _graphServiceClient;


    public UsersController(ILogger<UsersController> logger, IMemoryCache cache, IConfiguration configuration, GraphServiceClient graphServiceClient)
    {
        _logger = logger;
        _configuration = configuration;
        _graphServiceClient = graphServiceClient;
        _cache = cache;
    }

    [HttpGet("/api/users")]
    public async Task<IActionResult> UsersListAsync(string search = null, string searchBy = null, string nextPage = null, string oid = null)
    {
        UserCollectionResponse? users = null;
        try
        {
            if (string.IsNullOrEmpty(nextPage))
            {
                // First page of the list
                users = await _graphServiceClient.Users.GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = new string[] { "Id", "UserPrincipalName", "DisplayName", "GivenName", "Surname", "EmployeeId", "EmployeeHireDate", "Department", "mail", "jobTitle" };
                        requestConfiguration.QueryParameters.Top = 10;

                        if (!string.IsNullOrEmpty(oid) && oid.Length <= 36)
                        {
                            // Get user by user object ID. Note, you can also get a user without search. This approach is for code simplicity.
                            requestConfiguration.QueryParameters.Filter = $"Id eq '{oid}'";
                            requestConfiguration.QueryParameters.Expand = new string[] { "Manager" };
                        }
                        else if (!string.IsNullOrEmpty(search) && search.Length <= 15)
                        {
                            // Search users in the directory
                            if (string.IsNullOrEmpty(searchBy) || (new string[] { "mail", "department", "jobTitle" }.Contains(searchBy) == false))
                                // Search by name and UPN
                                requestConfiguration.QueryParameters.Filter = $"startswith(displayName,'{search}') or startswith(UserPrincipalName,'{search}')";
                            else
                                // Search by other attributes, such as eamil and job title
                                requestConfiguration.QueryParameters.Filter = $"startswith({searchBy},'{search}')";

                            // TBD filter by manager using direct reports
                            //https://learn.microsoft.com/graph/api/user-list-directreports
                        }
                        else
                        {
                            requestConfiguration.QueryParameters.Orderby = new string[] { "DisplayName" };
                        }
                    });
            }
            else
            {
                // Next or previous pages
                users = await _graphServiceClient.Users.WithUrl(nextPage).GetAsync();
            }


            WgUsers rv = new WgUsers();
            foreach (var user in users.Value)
            {
                WgUser wgUser = new WgUser()
                {
                    Id = user.Id!,
                    UPN = user.UserPrincipalName!,
                    DisplayName = user.DisplayName ?? "",
                    GivenName = user.GivenName ?? "",
                    Surname = user.Surname ?? "",
                    Department = user.Department ?? "",
                    EmployeeId = user.EmployeeId ?? "",
                    EmployeeHireDate = user.EmployeeHireDate != null ? user.EmployeeHireDate.Value.ToString("d") : "",
                    Email = user.Mail ?? "",
                    jobTitle = user.JobTitle ?? ""
                };

                // Get the user's manager
                if (user.Manager != null)
                {
                    User manager = await _graphServiceClient.Users[user.Manager.Id].GetAsync();

                    if (manager != null)
                    {
                        wgUser.ManagerUpn = manager.UserPrincipalName!;
                    }
                }

                rv.Users.Add(wgUser);
            }

            // Get the next page URL
            if (!string.IsNullOrEmpty(users.OdataNextLink))
            {
                rv.NextPage = HttpUtility.UrlEncode(users.OdataNextLink!);
            }
            else
            {
                rv.NextPage = "";
            }

            return Ok(rv);
        }
        catch (MicrosoftIdentityWebChallengeUserException ex)
        {
            string message = string.Empty;

            if (ex.InnerException != null)
            {
                message = ex.InnerException.Message;
            }
            else
            {
                message = ex.Message;
            }

            return BadRequest(new { error = message, identityError = true });
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("/api/users")]
    public async Task<IActionResult> UsersCreateAsync([FromForm] WgNewUser newUser)
    {
        // TBD:
        // 1. Input validation of the UPN format

        Random r = new Random();

        var requestBody = new User
        {
            AccountEnabled = true,
            DisplayName = newUser.DisplayName,
            GivenName = newUser.GivenName,
            Surname = newUser.Surname,
            Department = newUser.Department,
            EmployeeType = "Employee",
            EmployeeId = r.NextInt64(1234567, 9976654).ToString(),
            EmployeeHireDate = DateTime.UtcNow,
            JobTitle = newUser.JobTitle,
            Mail = newUser.Email,
            MailNickname = newUser.UPN.Split("@")[0],
            UserPrincipalName = newUser.UPN,
            PasswordProfile = new PasswordProfile
            {
                ForceChangePasswordNextSignIn = true,
                // Generate a random password
                Password = Guid.NewGuid().ToString(),
            },
        };

        try
        {
            // https://learn.microsoft.com/graph/api/user-post-users
            var result = await _graphServiceClient.Users.PostAsync(requestBody);

            // Add a manager
            if (this.HttpContext.User.GetObjectId() != null)
            {
                // https://learn.microsoft.com/graph/api/user-post-manager
                var managerRequestBody = new ReferenceUpdate
                {
                    OdataId = $"https://graph.microsoft.com/v1.0/users/{this.HttpContext.User.GetObjectId()}",
                };

                await _graphServiceClient.Users[result!.Id].Manager.Ref.PutAsync(managerRequestBody);
            }

            string session = Guid.NewGuid().ToString();

            // Add email authentication method so users can reset their password
            // https://learn.microsoft.com/graph/authenticationmethods-get-started?tabs=csharp
            // var authMethodRequestBody = new EmailAuthenticationMethod
            // {
            //     EmailAddress = newUser.Email
            // };

            // await _graphServiceClient.Users[result!.Id].Authentication.EmailMethods.PostAsync(authMethodRequestBody);

            // Send invite email
            string link = await Invite.SendInviteAsync(_configuration, this.Request, result!.Id!, result!.DisplayName!, result!.Mail!, session);

            // Add the user to the cache  
            await AddOrUpdateCacheAsync(result!.Id!, result!.UserPrincipalName, result!.DisplayName!, result!.GivenName!, result!.Surname!, newUser.Email, this.HttpContext.User.GetObjectId(), UserStatus.Invited, session);

            // Return the result
            return Ok(new { link = link, email = newUser.Email });
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task AddOrUpdateCacheAsync(string oid, string upn,
            string displayName, string givenName, string surname,
            string employeeEmail, string managerID, string status, string session)
    {
        // Get the manager email address
        var manager = await _graphServiceClient.Users[managerID].GetAsync();
        string managerEmail = string.Empty;

        if (manager.Mail != null)
            managerEmail = manager.Mail;
        else
            managerEmail = manager.UserPrincipalName;

        UsersCache cache = new UsersCache()
        {
            ID = oid,
            UPN = upn,
            Session = session,
            DisplayName = displayName,
            GivenName = givenName,
            Surname = surname,
            Email = employeeEmail,
            ManagerEmail = managerEmail,
            Status = status,
            StatusTime = DateTime.UtcNow
        };

        _cache.Set(cache.ID, cache.ToString(), DateTimeOffset.Now.AddHours(24));
    }

    [HttpGet("/api/users/invite")]
    public async Task<IActionResult> InviteUserAsync(string oid)
    {
        try
        {
            User user = await _graphServiceClient.Users[oid].GetAsync();

            if (user != null)
            {
                string session = Guid.NewGuid().ToString();
                // Send invite email
                string link = await Invite.SendInviteAsync(_configuration, this.Request, user.Id, user.DisplayName, user.Mail, session);

                // Add the user to the cache  
                await AddOrUpdateCacheAsync(user!.Id!, user!.UserPrincipalName, user.DisplayName, user.GivenName, user.Surname, user.Mail, this.HttpContext.User.GetObjectId(), UserStatus.Invited, session);

                // Return the result
                return Ok(new { link = link, email = user.Mail });
            }

            return BadRequest(new { error = "User not found" });

        }
        catch (System.Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("/api/users")]
    public async Task<IActionResult> UpdateUserAsync([FromForm] WgNewUser user)
    {
        // TBD:
        // 1. Input validation of the UPN format

        var requestBody = new User
        {
            Id = user.ID,
            AccountEnabled = false,
            DisplayName = user.DisplayName,
            GivenName = user.GivenName,
            Surname = user.Surname,
            Department = user.Department,
            //EmployeeHireDate = DateTime.UtcNow,
            JobTitle = user.JobTitle,
            Mail = user.Email,
            MailNickname = user.UPN.Split("@")[0],
        };

        try
        {
            // https://learn.microsoft.com/graph/api/user-update
            var result = await _graphServiceClient.Users[user.ID].PatchAsync(requestBody);

            // Return the result
            return Ok(result);
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("/api/users")]
    public async Task<IActionResult> DeleteUserAsync(string oid)
    {

        try
        {
            // https://learn.microsoft.com/graph/api/user-delete
            await _graphServiceClient.Users[oid].DeleteAsync();

            // TBD: update the cache
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        return Ok();
    }

    [HttpGet("/api/users/tap")]
    public async Task<IActionResult> GetManagerTapAsync(string oid)
    {
        try
        {
            // Get the user email address
            var user = _graphServiceClient.Users[oid].GetAsync();

            // Try to get the existing TAP
            var existingTap = _graphServiceClient.Users[oid].Authentication.TemporaryAccessPassMethods.GetAsync();

            if (existingTap != null && existingTap.Result != null && existingTap.Result != null && existingTap!.Result!.Value!.Count > 1)
            {
                foreach (var eTap in existingTap.Result.Value)
                {
                    // Delete any old TAPs so we can create a new one
                    await _graphServiceClient.Users[oid].Authentication.TemporaryAccessPassMethods[eTap.Id].DeleteAsync();
                }
            }

            // Create a new TAP code
            // https://learn.microsoft.com/graph/api/authentication-post-temporaryaccesspassmethods
            var requestBody = new TemporaryAccessPassAuthenticationMethod
            {
                LifetimeInMinutes = 60,
                IsUsableOnce = true,
            };

            var tap = await _graphServiceClient.Users[oid].Authentication.TemporaryAccessPassMethods.PostAsync(requestBody);

            // Send the TAP to the employee
            await Invite.SendTapAsync(_configuration, this.Request, user.Result.UserPrincipalName, user.Result.Mail, tap.TemporaryAccessPass);

            return Ok();
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("/api/users/usertap")]
    public async Task<IActionResult> GetUserTapAsync()
    {
        try
        {

            // Get the current user's state ID from the user's session
            string state = this.HttpContext.Session.GetString("state");

            // If the state object was not found, return an error message
            if (string.IsNullOrEmpty(state))
            {
                return BadRequest(new { error = "Cannot find the state object." });
            }

            // Try to read the request status object from the global cache using the state ID key
            UserFlowStatus status = null;
            if (_cache.TryGetValue(state, out string requestState))
            {
                status = UserFlowStatus.Parse(requestState);
            }
            else
            {
                return BadRequest(new { error = "Cannot find the Verified ID state object." });
            }

            // Get the user's status object from the ceche.
            UsersCache usersCache = null;
            string userObjectID = status.RequestStateId.Split("|")[0];
            if (_cache.TryGetValue(userObjectID, out string cacheValue))
            {
                usersCache = UsersCache.Parse(cacheValue);
            }

            var scopes = new[] { "https://graph.microsoft.com/.default" };

            X509Certificate2 clientCertificate = MsalAccessTokenHandler.ReadCertificate(_configuration.GetSection("AzureAd:ClientCertificates:0:CertificateThumbprint").Value);

            // using Azure.Identity;
            var options = new ClientCertificateCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            };


            // https://learn.microsoft.com/dotnet/api/azure.identity.clientcertificatecredential
            var clientCertCredential = new ClientCertificateCredential(
                usersCache.UPN.Split("@")[1], // This is the user's tenant ID
                _configuration.GetSection("AzureAd:ClientId").Value, clientCertificate, options);

            var graphClient = new GraphServiceClient(clientCertCredential, scopes);

            // Try to get the existing TAP
            var existingTap = await graphClient.Users[userObjectID].Authentication.TemporaryAccessPassMethods.GetAsync();

            if (existingTap != null && existingTap.Value != null && existingTap!.Value!.Count > 1)
            {
                foreach (var eTap in existingTap.Value)
                {
                    // Delete any old TAPs so we can create a new one
                    await graphClient.Users[userObjectID].Authentication.TemporaryAccessPassMethods[eTap.Id].DeleteAsync();
                }
            }

            // Create a new TAP code
            // https://learn.microsoft.com/graph/api/authentication-post-temporaryaccesspassmethods
            var requestBody = new TemporaryAccessPassAuthenticationMethod
            {
                LifetimeInMinutes = 60,
                IsUsableOnce = true,
            };

            var tap = await graphClient.Users[userObjectID].Authentication.TemporaryAccessPassMethods.PostAsync(requestBody);

            // Don't wait for the email to be sent
            await Invite.SendTapAsync(_configuration, Request, usersCache.UPN, usersCache.Email, tap.TemporaryAccessPass);

            // Update the cache that the process successfully completed
            usersCache.Status = UserStatus.Completed;
            usersCache.StatusTime = DateTime.UtcNow;
            _cache.Set(usersCache.ID, usersCache.ToString(), DateTimeOffset.Now.AddHours(24));

            // Retrun the TAP
            return Ok(new { tap = tap.TemporaryAccessPass, upn = usersCache.UPN });
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }


    }
}
