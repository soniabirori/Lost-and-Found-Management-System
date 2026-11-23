using System.Security.Claims;
using LostAndFoundSystem.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace LostAndFoundSystem.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly string _connectionString;

        public LoginModel(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("LostAndFoundDB")!;
        }

        [BindProperty] public string? Email { get; set; }
        [BindProperty] public string? Password { get; set; }

        public string ErrorMessage { get; set; } = "";

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            // 1. Validate the form
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Email and password are required.";
                return Page();
            }

            int userId = 0;
            string fullName = "";
            string storedHash = "";

            // 2. Find the user by email
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                string sql = "SELECT Id, FullName, PasswordHash FROM Users WHERE Email = @Email";
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Email", Email.Trim());

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    userId = reader.GetInt32(0);
                    fullName = reader.GetString(1);
                    storedHash = reader.GetString(2);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }

            // 3. Check the password
            if (userId == 0 || !PasswordHelper.Verify(Password, storedHash))
            {
                ErrorMessage = "Invalid email or password.";
                return Page();
            }

            // 4. Log the user in (saves their Id and name in a secure cookie)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Email, Email.Trim())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                          new ClaimsPrincipal(identity));

            // 5. Go back to the page they wanted, or the home page
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToPage("/Index");
        }
    }
}