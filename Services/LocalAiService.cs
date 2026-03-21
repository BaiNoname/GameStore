using GameStore.Models;

namespace GameStore.Services
{
    public class LocalAiService
    {
        private readonly GameStoreContext _db;

        // 🧠 lưu memory tạm theo user
        private static Dictionary<string, List<string>> chatMemory = new();

        public LocalAiService(GameStoreContext db)
        {
            _db = db;
        }

        public Task<string> AskAsync(string userId, string message)
        {
            message = message.ToLower();

            // lưu history
            if (!chatMemory.ContainsKey(userId))
                chatMemory[userId] = new List<string>();

            chatMemory[userId].Add(message);

            // =========================
            // 🤖 INTENT: GAME HOT
            // =========================
            if (message.Contains("game hot") ||
                message.Contains("hot game") ||
                message.Contains("hay") ||
                message.Contains("đề xuất"))
            {
                var games = _db.Games
                    .OrderByDescending(x => x.SoLuotTai)
                    .Take(5)
                    .Select(x => $"🔥 {x.TenGame}")
                    .ToList();

                return Task.FromResult("🎮 Game hot nhất:\n" + string.Join("\n", games));
            }

            // =========================
            // 💸 GAME RẺ
            // =========================
            if (message.Contains("rẻ") || message.Contains("giá"))
            {
                var games = _db.Games
                    .OrderBy(x => x.Gia)
                    .Take(5)
                    .Select(x => $"💰 {x.TenGame} - {x.Gia}đ")
                    .ToList();

                return Task.FromResult("💸 Game rẻ nhất:\n" + string.Join("\n", games));
            }

            // =========================
            // 🎯 GAME THEO TÊN
            // =========================
            var found = _db.Games
                .Where(x => x.TenGame.ToLower().Contains(message))
                .Take(5)
                .Select(x => $"🎮 {x.TenGame}")
                .ToList();

            if (found.Any())
                return Task.FromResult("🔍 Mình tìm thấy:\n" + string.Join("\n", found));

            // =========================
            // ❓ DEFAULT SMART RESPONSE
            // =========================
            return Task.FromResult(
                "🤖 Mình chưa hiểu rõ, bạn thử:\n" +
                "- game hot\n- game rẻ\n- tên game"
            );
        }
    }
}