using System.Security.Claims;
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

        public MyReportsModel(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("LostAndFoundDB")!;
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
            public string? PhotoPath { get; set; }   // filled in by the Photo Upload module
        }

        public List<ReportRow> Reports { get; set; } = new();
        public string ErrorMessage { get; set; } = "";

        [TempData] public string? SuccessMessage { get; set; }

        public void OnGet()
        {
            var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdText, out int userId))
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