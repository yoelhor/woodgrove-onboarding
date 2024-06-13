using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Woodgrove.Onboarding.Pages
{
    public class UsersModel : PageModel
    {
        public string DomainName = string.Empty;

        public void OnGet()
        {
            if (User.Identity!.IsAuthenticated && @User.Identity?.Name.Split("@").Length == 2)
            {
                DomainName = User.Identity?.Name.Split("@")[1];
            }

        }
    }
}
