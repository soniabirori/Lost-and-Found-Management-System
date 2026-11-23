using LostAndFoundSystem.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace LostAndFoundSystem.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly string _connectionString;

        // Reads the connection string from appsettings.json
        public RegisterModel(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("LostAndFoundDB")!;
        }

        // [BindProperty] fills these automatically from the form
        [BindProperty] public string? FullName { get; set; }
        [BindProperty] public string? Email { get; set; }
        [BindProperty] public string? Phone { get; set; }
        [BindProperty] public string? Password { get; set; }
        [BindProperty] public string? ConfirmPassword { get; set; }

        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public void OnGet()
        {
        }

        public void OnPost()
        {
            // 1. Validate the form
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ErrorMessage = "Full name, email and password are required.";
                return;
            }
            if (Password.Length < 6)
            {
                ErrorMessage = "Password must be at least 6 characters.";
                return;
            }
            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return;
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // 2. Make sure the email is not already used
                using (var check = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Email = @Email", connection))
                {
                    check.Parameters.AddWithValue("@Email", Email.Trim());
                    int count = (int)check.ExecuteScalar();
                    if (count > 0)
                    {
                        ErrorMessage = "An account with this email already exists.";
                        return;
                    }
                }

                // 3. Save the new user with a hashed password
                string sql = "INSERT INTO Users (FullName, Email, Phone, PasswordHash) " +
                             "VALUES (@FullName, @Email, @Phone, @PasswordHash)";
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@FullName", FullName.Trim());
                command.Parameters.AddWithValue("@Email", Email.Trim());
                command.Parameters.AddWithValue("@Phone",
                    string.IsNullOrWhiteSpace(Phone) ? DBNull.Value : Phone.Trim());
                command.Parameters.AddWithValue("@PasswordHash", PasswordHelper.Hash(Password));
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return;
            }

            // 4. Success: clear the form
            SuccessMessage = "Account created successfully! You can now log in.";
            FullName = Email = Phone = "";
            ModelState.Clear();
        }
    }
}