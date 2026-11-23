using System.Security.Claims;
using LostAndFoundSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace LostAndFoundSystem.Pages.Items
{
    public class DetailsModel : PageModel
    {
        private readonly string _connectionString;

        public DetailsModel(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("LostAndFoundDB")!;
        }

        public ItemInfo? Item { get; set; }
        public bool IsOwner { get; set; }
        public string ErrorMessage { get; set; } = "";

        public IActionResult OnGet(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var command = new SqlCommand(ItemInfo.SelectSql + " WHERE i.Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    Item = ItemInfo.FromReader(reader);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }

            if (Item == null)
                return NotFound(); // no item with that Id

            // Is the logged-in user the one who reported this item?
            string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            IsOwner = currentUserId != null && int.Parse(currentUserId) == Item.UserId;

            return Page();
        }
    }
}