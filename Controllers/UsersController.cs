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
using woodgrove_portal.Helpers;

namespace woodgrove_portal.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly IConfiguration _configuration;

    private readonly ILogger<UsersController> _logger;
    private readonly GraphServiceClient _graphServiceClient;


    public UsersController(ILogger<UsersController> logger, IConfiguration configuration, GraphServiceClient graphServiceClient)
    {
        _logger = logger;
        _configuration = configuration;
        _graphServiceClient = graphServiceClient;
    }

    [HttpGet("/api/users")]
    public async Task<IActionResult> UsersListAsync(string search = null, string searchBy = null, string nextPage = null, string oid = null)
    {
        UserCollectionResponse? users = null;

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

            // Get the special diet from the extension attributes
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

    [HttpPost("/api/users")]
    public async Task<IActionResult> UsersCreateAsync([FromForm] WgNewUser newUser)
    {
        // TBD:
        // 1. Input validation of the UPN format

        Random r = new Random();

        var requestBody = new User
        {
            AccountEnabled = false,
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

    [HttpPatch("/api/users")]
    public async Task<IActionResult> UpdateUserAsync([FromForm] WgNewUser newUser)
    {
        // TBD:
        // 1. Input validation of the UPN format

        var requestBody = new User
        {
            Id = newUser.ID,
            AccountEnabled = false,
            DisplayName = newUser.DisplayName,
            GivenName = newUser.GivenName,
            Surname = newUser.Surname,
            Department = newUser.Department,
            //EmployeeHireDate = DateTime.UtcNow,
            JobTitle = newUser.JobTitle,
            Mail = newUser.Email,
            MailNickname = newUser.UPN.Split("@")[0],
        };

        try
        {
            // https://learn.microsoft.com/graph/api/user-update
            var result = await _graphServiceClient.Users[newUser.ID].PatchAsync(requestBody);

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

}
