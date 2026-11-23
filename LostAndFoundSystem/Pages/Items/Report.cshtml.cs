using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace LostAndFoundSystem.Pages.Items
{
    // Only logged-in users can report an item.
    // If someone is not logged in, the cookie settings in Program.cs
    // send them to /Account/Login and bring them back here afterwards.
    [Authorize]
    public class ReportModel : PageModel
    {
        private readonly string _connectionString;

        public ReportModel(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("LostAndFoundDB")!;
        }

        // Choices shown in the Category dropdown
        public static readonly string[] Categories =
        {
            "Phone", "Wallet", "Keys", "Bag", "ID / Cards", "Electronics",
            "Clothing", "Books", "Jewelry", "Other"
        };

        // [BindProperty] fills these automatically from the form
        [BindProperty] public string? ItemType { get; set; } = "Lost";   // 'Lost' or 'Found'
        [BindProperty] public string? Title { get; set; }
        [BindProperty] public string? Description { get; set; }
        [BindProperty] public string? Category { get; set; }
        [BindProperty] public string? Location { get; set; }
        [BindProperty] public DateTime? EventDate { get; set; }

        public string ErrorMessage { get; set; } = "";

        public void OnGet(string? type = null)
        {
            // Lets other pages link to /Items/Report?type=Found
            if (type == "Lost" || type == "Found")
                ItemType = type;

            EventDate = DateTime.Today;
        }

        public IActionResult OnPost()
        {
            // 1. Validate the form (rules match the limits in CreateTables.sql)
            if (ItemType != "Lost" && ItemType != "Found")
            {
                ErrorMessage = "Please choose whether the item was lost or found.";
                return Page();
            }
            if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Category) ||
                string.IsNullOrWhiteSpace(Location) || EventDate == null)
            {
                ErrorMessage = "Title, category, location and date are required.";
                return Page();
            }
            if (Title.Trim().Length > 100)
            {
                ErrorMessage = "Title must be 100 characters or fewer.";
                return Page();
            }
            if (Location.Trim().Length > 150)
            {
                ErrorMessage = "Location must be 150 characters or fewer.";
                return Page();
            }
            if (!string.IsNullOrWhiteSpace(Description) && Description.Trim().Length > 500)
            {
                ErrorMessage = "Description must be 500 characters or fewer.";
                return Page();
            }
            if (!Categories.Contains(Category))
            {
                ErrorMessage = "Please choose a category from the list.";
                return Page();
            }
            if (EventDate.Value.Date > DateTime.Today)
            {
                ErrorMessage = "The date cannot be in the future.";
                return Page();
            }

            // 2. Who is reporting? Login saved their Id in the cookie.
            var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdText, out int userId))
            {
                return RedirectToPage("/Account/Login");
            }

            // 3. Save the report
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // PhotoPath is left empty here. The Photo Upload module
                // fills it in.
                string sql = "INSERT INTO Items (UserId, ItemType, Title, Description, Category, Location, EventDate) " +
                             "VALUES (@UserId, @ItemType, @Title, @Description, @Category, @Location, @EventDate)";
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@ItemType", ItemType);
                command.Parameters.AddWithValue("@Title", Title.Trim());
                command.Parameters.AddWithValue("@Description",
                    string.IsNullOrWhiteSpace(Description) ? DBNull.Value : Description.Trim());
                command.Parameters.AddWithValue("@Category", Category);
                command.Parameters.AddWithValue("@Location", Location.Trim());
                command.Parameters.AddWithValue("@EventDate", EventDate.Value.Date);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }

            // 4. Show the user's reports so they can see it was saved
            TempData["SuccessMessage"] = "Your report was submitted successfully.";
            return RedirectToPage("/Items/MyReports");
        }
    }
}