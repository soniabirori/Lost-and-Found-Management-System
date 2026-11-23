using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LostAndFoundSystem.Pages.Account
{
    public class LogoutModel : PageModel
    {
        // If someone just visits /Account/Logout, send them home
        public IActionResult OnGet()
        {
            return RedirectToPage("/Index");
        }

        // The Logout button posts here
        public async Task<IActionResult> OnPostAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Index");
        }
    }
}