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

        [TempData] public string? SuccessMessage { get; set; }   // survives the redirect
        [BindProperty] public string? MessageBody { get; set; }  // the contact form text

        // Show the item
        public IActionResult OnGet(int id)
        {
            if (!LoadItem(id))
                return ErrorMessage.Length > 0 ? Page() : NotFound();

            return Page();
        }

        // "Send message" button posts here
        public IActionResult OnPostSendMessage(int id)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToPage("/Account/Login", new { returnUrl = $"/Items/Details/{id}" });

            if (!LoadItem(id))
                return ErrorMessage.Length > 0 ? Page() : NotFound();

            if (IsOwner)
            {
                ErrorMessage = "You cannot send a message to yourself.";
                return Page();
            }
            if (Item!.Status == "Returned")
            {
                ErrorMessage = "This item has already been returned.";
                return Page();
            }
            if (string.IsNullOrWhiteSpace(MessageBody))
            {
                ErrorMessage = "Please write a message.";
                return Page();
            }
            if (MessageBody.Length > 1000)
            {
                ErrorMessage = "Message must be 1000 characters or less.";
                return Page();
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                string sql = "INSERT INTO Messages (ItemId, SenderId, ReceiverId, Body) " +
                             "VALUES (@ItemId, @SenderId, @ReceiverId, @Body)";
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@ItemId", Item.Id);
                command.Parameters.AddWithValue("@SenderId", CurrentUserId()!.Value);
                command.Parameters.AddWithValue("@ReceiverId", Item.UserId);
                command.Parameters.AddWithValue("@Body", MessageBody.Trim());
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }

            SuccessMessage = $"Message sent to {Item.ReporterName}! Replies will appear in My Messages.";
            return RedirectToPage(new { id }); // reload the page so refresh doesn't resend
        }

        // ---------- helpers ----------

        private int? CurrentUserId()
        {
            string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return value == null ? null : int.Parse(value);
        }

        // Loads the item into Item; returns false if not found or on error
        private bool LoadItem(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var command = new SqlCommand(ItemInfo.SelectSql + " WHERE i.Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                    Item = ItemInfo.FromReader(reader);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }

            if (Item == null) return false;

            IsOwner = CurrentUserId() == Item.UserId;
            return true;
        }
    }
}