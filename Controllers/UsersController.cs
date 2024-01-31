using System.Dynamic;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Web;
using Azure;
using Azure.Communication.Email;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using woodgrove_portal.Helpers;

namespace woodgrove_portal.Controllers;

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

            // Add to the cache and 
            await AddOrUpdateCacheAsync(newUser.GivenName + " " + newUser.Surname, newUser.Email, this.HttpContext.User.GetObjectId());

            // Send invite email
            await Invite.SendInviteAsync(_configuration, this.Request, newUser.Email);

            // Return the result
            return Ok(result);
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task AddOrUpdateCacheAsync(string uniqueName, string employeeEmail, string managerID)
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
            UniqueName = uniqueName,
            EmployeeEmail = employeeEmail,
            ManagerEmail = managerEmail
        };

        _cache.Set(cache.UniqueName, cache.ToString(), DateTimeOffset.Now.AddHours(24));
    }

    [HttpGet("/api/users/invite")]
    public async Task<IActionResult> InviteUserAsync(string oid)
    {
        try
        {
            User user = await _graphServiceClient.Users[oid].GetAsync();

            if (user != null)
            {
                // Send invite email
                await Invite.SendInviteAsync(_configuration, this.Request, user.Mail);
            }

            // Return the result
            return Ok();
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

            // Add to the cache and 
            await AddOrUpdateCacheAsync(user.GivenName + " " + user.Surname, user.Email, this.HttpContext.User.GetObjectId());

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
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        return Ok();
    }

    [HttpGet("/api/users/tap")]
    public async Task<IActionResult> GetTapAsync(string oid)
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
            await Invite.SendTapAsync(_configuration, this.Request, user.Result.Mail, tap.TemporaryAccessPass);

            return Ok();
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

}
