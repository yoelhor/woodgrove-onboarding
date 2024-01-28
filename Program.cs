using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);


// The following line enables Application Insights telemetry collection.
//builder.Services.AddApplicationInsightsTelemetry();

// Add authentication for the multi-tenant app and access Microsoft Graph as the user
// https://learn.microsoft.com/entra/identity-platform/multi-service-web-app-access-microsoft-graph-as-user
IEnumerable<string>? initialScopes = builder.Configuration["DownstreamApi:Scopes"]?.Split(' ');

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
        .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
                .AddMicrosoftGraph(builder.Configuration.GetSection("DownstreamApi"))
                .AddInMemoryTokenCaches();


// Add services to the container.
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI(); ;

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapControllers();

app.Run();
