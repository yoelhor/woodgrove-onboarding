using System.Dynamic;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
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
    public async Task<IActionResult> UsersListAsync()
    {
        var users = await _graphServiceClient.Users.GetAsync();
        return Ok(users);
    }
}
