using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace LostAndFoundSystem.Pages.Items
{
    [Authorize] // only logged-in users can report items
    public class ReportModel : PageModel
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _env;

        public static readonly string[] Categories =
        {
            "Phone", "Wallet", "Keys", "Bag", "ID / Documents",
            "Electronics", "Clothing", "Jewelry", "Books", "Other"
        };

        public ReportModel(IConfiguration config, IWebHostEnvironment env)
        {
            _connectionString = config.GetConnectionString("LostAndFoundDB")!;
            _env = env;
        }

        [BindProperty(SupportsGet = true)] public string? ItemType { get; set; }
        [BindProperty] public string? Title { get; set; }
        [BindProperty] public string? Description { get; set; }
        [BindProperty] public string? Category { get; set; }
        [BindProperty] public string? Location { get; set; }
        [BindProperty] public DateTime? EventDate { get; set; }
        [BindProperty] public IFormFile? Photo { get; set; }

        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public void OnGet()
        {
            if (ItemType != "Found") ItemType = "Lost";
            EventDate = DateTime.Today;
        }

        public IActionResult OnPost()
        {
            // 1. Validate the form
            if (ItemType != "Lost" && ItemType != "Found")
            {
                ErrorMessage = "Please choose Lost or Found.";
                return Page();
            }
            if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Category) ||
                string.IsNullOrWhiteSpace(Location) || EventDate == null)
            {
                ErrorMessage = "Title, category, location and date are required.";
                return Page();
            }
            if (!Categories.Contains(Category))
            {
                ErrorMessage = "Please choose a valid category.";
                return Page();
            }
            if (EventDate > DateTime.Today)
            {
                ErrorMessage = "The date cannot be in the future.";
                return Page();
            }

            // 2. Save the photo (optional)
            string? photoPath = null;
            if (Photo != null && Photo.Length > 0)
            {
                string[] allowed = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                string ext = Path.GetExtension(Photo.FileName).ToLowerInvariant();

                if (!allowed.Contains(ext))
                {
                    ErrorMessage = "Photo must be a JPG, PNG, GIF or WEBP image.";
                    return Page();
                }
                if (Photo.Length > 5 * 1024 * 1024)
                {
                    ErrorMessage = "Photo must be smaller than 5 MB.";
                    return Page();
                }

                // Save into wwwroot/uploads with a unique name
                string folder = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(folder);
                string fileName = Guid.NewGuid().ToString() + ext;

                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                {
                    Photo.CopyTo(stream);
                }
                photoPath = "/uploads/" + fileName;
            }

            // 3. Get the logged-in user's Id (saved in the cookie at login)
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // 4. Save the item
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                string sql = "INSERT INTO Items (UserId, ItemType, Title, Description, Category, Location, EventDate, PhotoPath) " +
                             "VALUES (@UserId, @ItemType, @Title, @Description, @Category, @Location, @EventDate, @PhotoPath)";
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@ItemType", ItemType);
                command.Parameters.AddWithValue("@Title", Title.Trim());
                command.Parameters.AddWithValue("@Description",
                    string.IsNullOrWhiteSpace(Description) ? DBNull.Value : Description.Trim());
                command.Parameters.AddWithValue("@Category", Category);
                command.Parameters.AddWithValue("@Location", Location.Trim());
                command.Parameters.AddWithValue("@EventDate", EventDate.Value.Date);
                command.Parameters.AddWithValue("@PhotoPath", (object?)photoPath ?? DBNull.Value);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }

            // 5. Success: clear the form but keep Lost/Found choice
            SuccessMessage = $"Your {ItemType.ToLower()} item has been reported successfully!";
            Title = Description = Category = Location = "";
            EventDate = DateTime.Today;
            ModelState.Clear();
            return Page();
        }
    }
}