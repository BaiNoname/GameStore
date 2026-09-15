using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GameStore.Models;

namespace GameStore.Services
{
    public class EventAiServiceImpl : EventAiService
    {
        private readonly GameStoreContext db;
        private readonly IHttpClientFactory httpFactory;
        private readonly IConfiguration config;

        public EventAiServiceImpl(GameStoreContext _db, IHttpClientFactory _httpFactory, IConfiguration _config)
        {
            db = _db;
            httpFactory = _httpFactory;
            config = _config;
        }

        public async Task<(bool ok, string reply)> AskAsync(int eventId, int userId, string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return (false, "Bạn hãy nhập câu hỏi về sự kiện nhé.");

            var ev = db.Events.FirstOrDefault(e => e.EventId == eventId);
            if (ev == null)
                return (false, "Không tìm thấy sự kiện.");

            var apiKey = config["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return (false, "Trợ lý AI chưa được cấu hình (thiếu Gemini API key).");

            var model = config["Gemini:Model"];
            if (string.IsNullOrWhiteSpace(model)) model = "gemini-3.6-flash";

            // ===== Ngữ cảnh sự kiện =====
            var joined = db.EventParticipants.Any(p => p.EventId == eventId && p.UserId == userId);
            var ctx = new StringBuilder();
            ctx.AppendLine("THÔNG TIN SỰ KIỆN:");
            ctx.AppendLine($"- Tên: {ev.Title}");
            ctx.AppendLine($"- Loại sự kiện: {ev.EventType}");
            ctx.AppendLine($"- Hình thức: {ev.AccessType}" + (ev.AccessType == "Paid" ? $" (phí tham gia: {ev.Price:#,0}đ)" : " (miễn phí)"));
            ctx.AppendLine($"- Trạng thái: {ev.Status}");
            ctx.AppendLine($"- Thời gian diễn ra: {ev.StartAt.ToLocalTime():dd/MM/yyyy HH:mm} đến {ev.EndAt.ToLocalTime():dd/MM/yyyy HH:mm}");
            ctx.AppendLine($"- Số người tham gia: {ev.CurrentParticipants}" + (ev.MaxParticipants.HasValue ? $"/{ev.MaxParticipants}" : ""));
            if (!string.IsNullOrWhiteSpace(ev.PrizeInfo)) ctx.AppendLine($"- Phần thưởng: {ev.PrizeInfo}");
            if (!string.IsNullOrWhiteSpace(ev.PrizeType)) ctx.AppendLine($"- Loại thưởng: {ev.PrizeType} {ev.PrizeValue} (điều kiện: {ev.PrizeCondition})");
            if (!string.IsNullOrWhiteSpace(ev.Content))
                ctx.AppendLine($"- Thể lệ/mô tả: {StripHtml(ev.Content)}");
            ctx.AppendLine($"- Người dùng hiện tại {(joined ? "ĐÃ tham gia" : "CHƯA tham gia")} sự kiện này.");

            var systemPrompt =
                "Bạn là trợ lý AI của GameStore, CHỈ tư vấn về SỰ KIỆN (event) đang xem. " +
                "Chỉ trả lời dựa trên thông tin sự kiện được cung cấp bên dưới và các câu hỏi liên quan tới sự kiện đó " +
                "(thể lệ, cách tham gia, phí, thời gian, phần thưởng, điểm danh, điều kiện nhận thưởng...). " +
                "Nếu câu hỏi KHÔNG liên quan tới sự kiện (ví dụ hỏi về game khác, chính trị, code, đời sống...), " +
                "hãy lịch sự từ chối và nhắc người dùng chỉ hỏi về sự kiện này. " +
                "Trả lời NGẮN GỌN, đủ ý trong 1-3 câu, bằng tiếng Việt, thân thiện, không bịa thông tin ngoài dữ liệu được cấp. " +
                "Hạn chế dùng ký hiệu markdown; nếu cần nhấn mạnh chỉ dùng **chữ đậm** đơn giản, không dùng bảng hay tiêu đề.\n\n" +
                ctx.ToString();

            // ===== Gọi Gemini =====
            // Gộp system prompt + câu hỏi vào 1 content để tương thích mọi model/version (không dùng system_instruction)
            var fullPrompt = systemPrompt + "\n\nCÂU HỎI CỦA NGƯỜI DÙNG: " + question;
            var payload = new
            {
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = fullPrompt } } }
                },
                generationConfig = new { temperature = 0.4, maxOutputTokens = 1024 }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            try
            {
                var client = httpFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                var reqBody = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var resp = await client.PostAsync(url, reqBody);
                var raw = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    // Lấy message lỗi thật từ Gemini để dễ chẩn đoán
                    string detail = "";
                    try
                    {
                        using var ed = JsonDocument.Parse(raw);
                        if (ed.RootElement.TryGetProperty("error", out var er) &&
                            er.TryGetProperty("message", out var em))
                            detail = em.GetString() ?? "";
                    }
                    catch { }
                    if (string.IsNullOrWhiteSpace(detail))
                        detail = raw.Length > 300 ? raw.Substring(0, 300) : raw;

                    return (false, $"AI lỗi ({(int)resp.StatusCode}): {detail}");
                }

                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                if (root.TryGetProperty("candidates", out var cands) && cands.GetArrayLength() > 0)
                {
                    var parts = cands[0].GetProperty("content").GetProperty("parts");
                    var sb = new StringBuilder();
                    foreach (var p in parts.EnumerateArray())
                        if (p.TryGetProperty("text", out var t)) sb.Append(t.GetString());

                    var text = sb.ToString().Trim();
                    return string.IsNullOrWhiteSpace(text)
                        ? (false, "Xin lỗi, mình chưa có câu trả lời phù hợp.")
                        : (true, text);
                }

                return (false, "Xin lỗi, mình chưa trả lời được câu này.");
            }
            catch
            {
                return (false, "Không kết nối được trợ lý AI, vui lòng thử lại sau.");
            }
        }

        private static string StripHtml(string html)
        {
            var text = Regex.Replace(html ?? "", "<.*?>", " ");
            text = System.Net.WebUtility.HtmlDecode(text);
            text = Regex.Replace(text, "\\s+", " ").Trim();
            if (text.Length > 1500) text = text.Substring(0, 1500) + "...";
            return text;
        }
    }
}