using System.Dynamic;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Web;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Abstractions;

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

    [HttpGet("list")]
    public async Task<IActionResult> UsersListAsync(string search = null, string nextPage = null)
    {
        UserCollectionResponse? users = null;

        if (string.IsNullOrEmpty(nextPage))
        {
            // First page of the list
            users = await _graphServiceClient.Users.GetAsync(requestConfiguration =>
                        {
                            requestConfiguration.QueryParameters.Select = new string[] { "Id", "UserPrincipalName", "DisplayName", "GivenName", "Surname", "EmployeeId", "EmployeeHireDate", "Department", "mail", "jobTitle" };
                            requestConfiguration.QueryParameters.Top = 10;

                            if (!string.IsNullOrEmpty(search) && search.Length <= 15)
                            {
                                requestConfiguration.QueryParameters.Filter = $"startswith(displayName,'{search}') or startswith(UserPrincipalName,'{search}')";
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
            rv.Users.Add(new WgUser()
            {
                Id = user.Id!,
                UPN = user.UserPrincipalName!,
                DisplayName = user.DisplayName ?? "",
                GivenName = user.GivenName ?? "",
                Surname = user.Surname ?? "",
                Department = user.Department ?? "",
                EmployeeId = user.EmployeeId ?? "",
                EmployeeHireDate = "" ?? "",
                Email = user.Mail ?? "",
                jobTitle = user.JobTitle ?? ""
            });
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

    [HttpPost("create")]
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
                Password = Guid.NewGuid().ToString() + Guid.NewGuid().ToString(),
            },
        };


        try
        {
            // https://learn.microsoft.com/graph/api/user-post-users
            var result = await _graphServiceClient.Users.PostAsync(requestBody);

            // Return the result
            return Ok(result);
        }
        catch (System.Exception ex)
        {

            return Ok(ex.Message);
        }



    }

}
