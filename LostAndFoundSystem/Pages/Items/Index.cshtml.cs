using System.Text;
using LostAndFoundSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace LostAndFoundSystem.Pages.Items
{
    public class IndexModel : PageModel
    {
        private readonly string _connectionString;

        public IndexModel(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("LostAndFoundDB")!;
        }

        [BindProperty(SupportsGet = true)] public string? Search { get; set; }
        [BindProperty(SupportsGet = true)] public string? Type { get; set; }
        [BindProperty(SupportsGet = true)] public string? Category { get; set; }
        [BindProperty(SupportsGet = true)] public string? Status { get; set; }

        public List<ItemInfo> Items { get; set; } = new();
        public string ErrorMessage { get; set; } = "";

        public void OnGet()
        {
            Status ??= "Open";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var command = new SqlCommand();
                command.Connection = connection;

                var sql = new StringBuilder(ItemInfo.SelectSql + " WHERE 1 = 1");

                if (!string.IsNullOrWhiteSpace(Search))
                {
                    sql.Append(" AND (i.Title LIKE @Search OR i.Description LIKE @Search OR i.Location LIKE @Search)");
                    command.Parameters.AddWithValue("@Search", "%" + Search.Trim() + "%");
                }
                if (Type == "Lost" || Type == "Found")
                {
                    sql.Append(" AND i.ItemType = @Type");
                    command.Parameters.AddWithValue("@Type", Type);
                }
                if (!string.IsNullOrWhiteSpace(Category))
                {
                    sql.Append(" AND i.Category = @Category");
                    command.Parameters.AddWithValue("@Category", Category);
                }
                if (Status == "Open" || Status == "Returned")
                {
                    sql.Append(" AND i.Status = @Status");
                    command.Parameters.AddWithValue("@Status", Status);
                }

                sql.Append(" ORDER BY i.CreatedAt DESC");
                command.CommandText = sql.ToString();

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Items.Add(ItemInfo.FromReader(reader));
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }
}