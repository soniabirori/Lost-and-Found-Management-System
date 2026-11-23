using Microsoft.Data.SqlClient;

namespace LostAndFoundSystem.Models
{
    public class ItemInfo
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ItemType { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string Category { get; set; } = "";
        public string Location { get; set; } = "";
        public DateTime EventDate { get; set; }
        public string? PhotoPath { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string ReporterName { get; set; } = "";

        // The SELECT both pages use (joins Users so we get the reporter's name)
        public const string SelectSql =
            "SELECT i.Id, i.UserId, i.ItemType, i.Title, i.Description, i.Category, i.Location, " +
            "i.EventDate, i.PhotoPath, i.Status, i.CreatedAt, u.FullName AS ReporterName " +
            "FROM Items i JOIN Users u ON i.UserId = u.Id";

        // Turns one database row into an ItemInfo object
        public static ItemInfo FromReader(SqlDataReader reader)
        {
            return new ItemInfo
            {
                Id = (int)reader["Id"],
                UserId = (int)reader["UserId"],
                ItemType = (string)reader["ItemType"],
                Title = (string)reader["Title"],
                Description = reader["Description"] as string,
                Category = (string)reader["Category"],
                Location = (string)reader["Location"],
                EventDate = (DateTime)reader["EventDate"],
                PhotoPath = reader["PhotoPath"] as string,
                Status = (string)reader["Status"],
                CreatedAt = (DateTime)reader["CreatedAt"],
                ReporterName = (string)reader["ReporterName"]
            };
        }
    }
}