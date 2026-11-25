using System.Security.Claims;
using LostAndFoundSystem.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace LostAndFoundSystem.Pages.Items
{
    [Authorize]
    public class MyReportsModel : PageModel
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _env;

        public MyReportsModel(IConfiguration config, IWebHostEnvironment env)
        {
            _connectionString = config.GetConnectionString("LostAndFoundDB")!;
            _env = env;
        }

        // One row of the table on the page
        public class ReportRow
        {
            public int Id { get; set; }
            public string ItemType { get; set; } = "";
            public string Title { get; set; } = "";
            public string Category { get; set; } = "";
            public string Location { get; set; } = "";
            public DateTime EventDate { get; set; }
            public string Status { get; set; } = "";
            public string? PhotoPath { get; set; }
        }

        public List<ReportRow> Reports { get; set; } = new();
        public string ErrorMessage { get; set; } = "";

        [TempData] public string? SuccessMessage { get; set; }

        public void OnGet()
        {
            LoadReports();
        }

        // "Mark returned" button
        public IActionResult OnPostMarkReturned(int id)
        {
            return SetStatus(id, "Returned", "Item marked as returned. 🎉");
        }

        // "Reopen" button (in case it was marked returned by mistake)
        public IActionResult OnPostReopen(int id)
        {
            return SetStatus(id, "Open", "Item reopened.");
        }

        // "Delete" button
        public IActionResult OnPostDelete(int id)
        {
            int? userId = GetUserId();
            if (userId == null) return RedirectToPage("/Account/Login");

            string? photoPath;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // 1. Make sure the item exists AND belongs to this user; get its photo
                using (var find = new SqlCommand("SELECT PhotoPath FROM Items WHERE Id = @Id AND UserId = @UserId", connection))
                {
                    find.Parameters.AddWithValue("@Id", id);
                    find.Parameters.AddWithValue("@UserId", userId);
                    object? result = find.ExecuteScalar();
                    if (result == null)
                    {
                        ErrorMessage = "Item not found.";
                        LoadReports();
                        return Page();
                    }
                    photoPath = result as string; // null if the item had no photo
                }

                // 2. Delete its messages first (they point to the item), then the item
                using var transaction = connection.BeginTransaction();

                using (var deleteMessages = new SqlCommand("DELETE FROM Messages WHERE ItemId = @Id", connection, transaction))
                {
                    deleteMessages.Parameters.AddWithValue("@Id", id);
                    deleteMessages.ExecuteNonQuery();
                }
                using (var deleteItem = new SqlCommand("DELETE FROM Items WHERE Id = @Id AND UserId = @UserId", connection, transaction))
                {
                    deleteItem.Parameters.AddWithValue("@Id", id);
                    deleteItem.Parameters.AddWithValue("@UserId", userId);
                    deleteItem.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                LoadReports();
                return Page();
            }

            // 3. Remove the photo file from the uploads folder
            PhotoHelper.Delete(photoPath, _env.WebRootPath);

            SuccessMessage = "Report deleted.";
            return RedirectToPage();
        }

        // ---------- helpers ----------

        private int? GetUserId()
        {
            var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdText, out int userId) ? userId : null;
        }

        private IActionResult SetStatus(int id, string status, string successText)
        {
            int? userId = GetUserId();
            if (userId == null) return RedirectToPage("/Account/Login");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // "AND UserId" makes sure people can only change their own items
                string sql = "UPDATE Items SET Status = @Status WHERE Id = @Id AND UserId = @UserId";
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@UserId", userId);

                if (command.ExecuteNonQuery() == 0)
                {
                    ErrorMessage = "Item not found.";
                    LoadReports();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                LoadReports();
                return Page();
            }

            SuccessMessage = successText;
            return RedirectToPage();
        }

        private void LoadReports()
        {
            int? userId = GetUserId();
            if (userId == null)
            {
                ErrorMessage = "Could not identify your account. Please log in again.";
                return;
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                string sql = "SELECT Id, ItemType, Title, Category, Location, EventDate, Status, PhotoPath " +
                             "FROM Items WHERE UserId = @UserId ORDER BY CreatedAt DESC";
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Reports.Add(new ReportRow
                    {
                        Id = reader.GetInt32(0),
                        ItemType = reader.GetString(1),
                        Title = reader.GetString(2),
                        Category = reader.GetString(3),
                        Location = reader.GetString(4),
                        EventDate = reader.GetDateTime(5),
                        Status = reader.GetString(6),
                        PhotoPath = reader.IsDBNull(7) ? null : reader.GetString(7)
                    });
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }
}