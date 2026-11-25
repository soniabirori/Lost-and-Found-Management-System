using LostAndFoundSystem.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace LostAndFoundSystem.Pages
{
    public class IndexModel : PageModel
    {
        private readonly string _connectionString;

        public IndexModel(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("LostAndFoundDB")!;
        }

        public int OpenLost { get; set; }
        public int OpenFound { get; set; }
        public int Returned { get; set; }
        public int Members { get; set; }
        public List<ItemInfo> RecentItems { get; set; } = new();

        public void OnGet()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // 1. Live statistics for the hero section
                string statsSql =
                    "SELECT " +
                    "ISNULL(SUM(CASE WHEN ItemType = 'Lost'  AND Status = 'Open' THEN 1 ELSE 0 END), 0), " +
                    "ISNULL(SUM(CASE WHEN ItemType = 'Found' AND Status = 'Open' THEN 1 ELSE 0 END), 0), " +
                    "ISNULL(SUM(CASE WHEN Status = 'Returned' THEN 1 ELSE 0 END), 0), " +
                    "(SELECT COUNT(*) FROM Users) " +
                    "FROM Items";
                using (var stats = new SqlCommand(statsSql, connection))
                using (var reader = stats.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        OpenLost = reader.GetInt32(0);
                        OpenFound = reader.GetInt32(1);
                        Returned = reader.GetInt32(2);
                        Members = reader.GetInt32(3);
                    }
                }

                // 2. The 6 newest open items
                string recentSql = ItemInfo.SelectSql +
                    " WHERE i.Status = 'Open' ORDER BY i.CreatedAt DESC OFFSET 0 ROWS FETCH NEXT 6 ROWS ONLY";
                using (var recent = new SqlCommand(recentSql, connection))
                using (var reader = recent.ExecuteReader())
                {
                    while (reader.Read())
                        RecentItems.Add(ItemInfo.FromReader(reader));
                }
            }
            catch
            {
                // If the database is unavailable the home page still shows, just without numbers
            }
        }
    }
}