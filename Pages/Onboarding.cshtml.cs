using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using woodgrove_portal.Controllers;
using woodgrove_portal.Models;
using WoodgroveDemo.Helpers;

namespace woodgrove_portal.Pages
{
    public class OnboardingModel : PageModel
    {
        public string Error { get; set; }
        public string DisplayName { get; set; }
        private IMemoryCache _Cache;
        protected readonly IConfiguration _Configuration;

        public OnboardingModel(IConfiguration configuration, IMemoryCache cache)
        {
            _Cache = cache;
            _Configuration = configuration;
        }

        public void OnGet(string? token = "")
        {

            try
            {
                UsersCache usersCache = TokenValidation(_Configuration, token, _Cache);
                DisplayName = usersCache.DisplayName;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }

        public static UsersCache TokenValidation(IConfiguration configuration, string? token, IMemoryCache cache)
        {
            // Check if the token parameter is presented in the HTTP request
            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("The token parameter is missing.");
            }

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "https://woodgrove.com",
                    IssuerSigningKey = new X509SecurityKey(MsalAccessTokenHandler.ReadCertificate(configuration.GetSection("AzureAd:ClientCertificates:0:CertificateThumbprint").Value)),
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var oid = jwtToken.Claims.First(x => x.Type == "id").Value;
                var session = jwtToken.Claims.First(x => x.Type == "session").Value;
                


                // return user id from JWT token if validation successful

                // Get the user's cache object
                UsersCache usersCache = null;
                if (cache.TryGetValue(oid, out string cacheValue))
                {
                    usersCache = UsersCache.Parse(cacheValue);
                }
                else
                {
                    throw new Exception("Cannot find your verification link in our system.");
                }

                // Check the session ID
                if (session != usersCache.Session)
                {
                    throw new Exception("The session ID in the verification link is invalid.");
                }

                // Check the status of the verification link
                if (usersCache.Status != UserStatus.Invited)
                {
                    throw new Exception("This verification link has already been used.");
                }

                return usersCache;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
