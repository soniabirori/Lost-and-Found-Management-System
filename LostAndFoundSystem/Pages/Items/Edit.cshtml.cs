using System.Security.Claims;
using LostAndFoundSystem.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace LostAndFoundSystem.Pages.Items
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _env;

        public EditModel(IConfiguration config, IWebHostEnvironment env)
        {
            _connectionString = config.GetConnectionString("LostAndFoundDB")!;
            _env = env;
        }

        [BindProperty] public string? ItemType { get; set; }
        [BindProperty] public string? Title { get; set; }
        [BindProperty] public string? Description { get; set; }
        [BindProperty] public string? Category { get; set; }
        [BindProperty] public string? Location { get; set; }
        [BindProperty] public DateTime? EventDate { get; set; }
        [BindProperty] public IFormFile? Photo { get; set; }

        public string? CurrentPhotoPath { get; set; }
        public string ErrorMessage { get; set; } = "";

        [TempData] public string? SuccessMessage { get; set; } // shown on My Reports after saving

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Fill the form with the item's current data
        public IActionResult OnGet(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                string sql = "SELECT ItemType, Title, Description, Category, Location, EventDate, PhotoPath " +
                             "FROM Items WHERE Id = @Id AND UserId = @UserId";
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@UserId", UserId);

                using var reader = command.ExecuteReader();
                if (!reader.Read())
                    return NotFound(); // doesn't exist or isn't yours

                ItemType = reader.GetString(0);
                Title = reader.GetString(1);
                Description = reader.IsDBNull(2) ? null : reader.GetString(2);
                Category = reader.GetString(3);
                Location = reader.GetString(4);
                EventDate = reader.GetDateTime(5);
                CurrentPhotoPath = reader.IsDBNull(6) ? null : reader.GetString(6);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            return Page();
        }

        // Save the changes
        public IActionResult OnPost(int id)
        {
            // 1. Check the item belongs to this user, and get its current photo
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var find = new SqlCommand("SELECT PhotoPath FROM Items WHERE Id = @Id AND UserId = @UserId", connection);
                find.Parameters.AddWithValue("@Id", id);
                find.Parameters.AddWithValue("@UserId", UserId);

                object? result = find.ExecuteScalar();
                if (result == null) return NotFound();
                CurrentPhotoPath = result as string;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }

            // 2. Validate (same rules as the Report page)
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
            if (!ReportModel.Categories.Contains(Category))
            {
                ErrorMessage = "Please choose a valid category.";
                return Page();
            }
            if (EventDate > DateTime.Today)
            {
                ErrorMessage = "The date cannot be in the future.";
                return Page();
            }

            // 3. New photo? (optional)
            string? newPhotoPath = null;
            if (Photo != null && Photo.Length > 0)
            {
                string? photoError = PhotoHelper.Validate(Photo);
                if (photoError != null)
                {
                    ErrorMessage = photoError;
                    return Page();
                }
                newPhotoPath = PhotoHelper.Save(Photo, _env.WebRootPath);
            }

            // 4. Update the database
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                string sql = "UPDATE Items SET ItemType = @ItemType, Title = @Title, Description = @Description, " +
                             "Category = @Category, Location = @Location, EventDate = @EventDate, PhotoPath = @PhotoPath " +
                             "WHERE Id = @Id AND UserId = @UserId";
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@ItemType", ItemType);
                command.Parameters.AddWithValue("@Title", Title.Trim());
                command.Parameters.AddWithValue("@Description",
                    string.IsNullOrWhiteSpace(Description) ? DBNull.Value : Description.Trim());
                command.Parameters.AddWithValue("@Category", Category);
                command.Parameters.AddWithValue("@Location", Location.Trim());
                command.Parameters.AddWithValue("@EventDate", EventDate.Value.Date);
                command.Parameters.AddWithValue("@PhotoPath", (object?)(newPhotoPath ?? CurrentPhotoPath) ?? DBNull.Value);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@UserId", UserId);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                PhotoHelper.Delete(newPhotoPath, _env.WebRootPath); // undo the new upload
                ErrorMessage = ex.Message;
                return Page();
            }

            // 5. If the photo was replaced, delete the old file
            if (newPhotoPath != null)
                PhotoHelper.Delete(CurrentPhotoPath, _env.WebRootPath);

            SuccessMessage = "Item updated successfully.";
            return RedirectToPage("/Items/MyReports");
        }
    }
}