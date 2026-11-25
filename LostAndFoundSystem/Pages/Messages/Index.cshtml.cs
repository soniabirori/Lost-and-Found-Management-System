using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace LostAndFoundSystem.Pages.Messages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly string _connectionString;

        public IndexModel(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("LostAndFoundDB")!;
        }

        public class MessageRow
        {
            public int ItemId { get; set; }
            public string ItemTitle { get; set; } = "";
            public string ItemType { get; set; } = "";
            public bool IsIncoming { get; set; }          // true = received, false = sent
            public int OtherUserId { get; set; }
            public string OtherName { get; set; } = "";
            public string OtherEmail { get; set; } = "";
            public string? OtherPhone { get; set; }
            public string Body { get; set; } = "";
            public DateTime SentAt { get; set; }
        }

        public List<MessageRow> MessageList { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
        [TempData] public string? SuccessMessage { get; set; }

        // Reply form fields
        [BindProperty] public int ReplyItemId { get; set; }
        [BindProperty] public int ReplyToUserId { get; set; }
        [BindProperty] public string? ReplyBody { get; set; }

        private int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public void OnGet()
        {
            LoadMessages();
        }

        public IActionResult OnPostReply()
        {
            if (string.IsNullOrWhiteSpace(ReplyBody) || ReplyBody.Length > 1000)
            {
                ErrorMessage = "Reply must be between 1 and 1000 characters.";
                LoadMessages();
                return Page();
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // Security: you can only reply in a conversation you are already part of
                string checkSql = "SELECT COUNT(*) FROM Messages WHERE ItemId = @ItemId AND " +
                                  "((SenderId = @Me AND ReceiverId = @Other) OR (SenderId = @Other AND ReceiverId = @Me))";
                using (var check = new SqlCommand(checkSql, connection))
                {
                    check.Parameters.AddWithValue("@ItemId", ReplyItemId);
                    check.Parameters.AddWithValue("@Me", Me);
                    check.Parameters.AddWithValue("@Other", ReplyToUserId);
                    if ((int)check.ExecuteScalar() == 0)
                    {
                        ErrorMessage = "You can only reply to your own conversations.";
                        LoadMessages();
                        return Page();
                    }
                }

                string sql = "INSERT INTO Messages (ItemId, SenderId, ReceiverId, Body) " +
                             "VALUES (@ItemId, @SenderId, @ReceiverId, @Body)";
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@ItemId", ReplyItemId);
                command.Parameters.AddWithValue("@SenderId", Me);
                command.Parameters.AddWithValue("@ReceiverId", ReplyToUserId);
                command.Parameters.AddWithValue("@Body", ReplyBody.Trim());
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                LoadMessages();
                return Page();
            }

            SuccessMessage = "Reply sent!";
            return RedirectToPage();
        }

        private void LoadMessages()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                string sql =
                    "SELECT m.ItemId, i.Title, i.ItemType, m.SenderId, m.Body, m.SentAt, " +
                    "s.FullName AS SenderName, s.Email AS SenderEmail, s.Phone AS SenderPhone, " +
                    "m.ReceiverId, r.FullName AS ReceiverName, r.Email AS ReceiverEmail, r.Phone AS ReceiverPhone " +
                    "FROM Messages m " +
                    "JOIN Items i ON m.ItemId = i.Id " +
                    "JOIN Users s ON m.SenderId = s.Id " +
                    "JOIN Users r ON m.ReceiverId = r.Id " +
                    "WHERE m.SenderId = @Me OR m.ReceiverId = @Me " +
                    "ORDER BY m.SentAt DESC";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Me", Me);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    bool incoming = (int)reader["ReceiverId"] == Me;

                    MessageList.Add(new MessageRow
                    {
                        ItemId = (int)reader["ItemId"],
                        ItemTitle = (string)reader["Title"],
                        ItemType = (string)reader["ItemType"],
                        IsIncoming = incoming,
                        OtherUserId = incoming ? (int)reader["SenderId"] : (int)reader["ReceiverId"],
                        OtherName = (string)reader[incoming ? "SenderName" : "ReceiverName"],
                        OtherEmail = (string)reader[incoming ? "SenderEmail" : "ReceiverEmail"],
                        OtherPhone = reader[incoming ? "SenderPhone" : "ReceiverPhone"] as string,
                        Body = (string)reader["Body"],
                        SentAt = (DateTime)reader["SentAt"]
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