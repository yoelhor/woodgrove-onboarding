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
    public async Task<IActionResult> UsersListAsync(string nextPage = null)
    {
        UserCollectionResponse? users = null;

        if (string.IsNullOrEmpty(nextPage))
        {
            // First page of the list
            users = await _graphServiceClient.Users.GetAsync(requestConfiguration =>
                        {
                            requestConfiguration.QueryParameters.Select = new string[] { "Id", "UserPrincipalName", "DisplayName", "GivenName", "Surname", "EmployeeId", "EmployeeHireDate", "Department", "mail", "jobTitle" };
                            requestConfiguration.QueryParameters.Orderby = new string[] { "DisplayName" };
                            requestConfiguration.QueryParameters.Top = 10;
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
}
