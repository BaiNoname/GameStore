using GameStore.Services;
using Microsoft.AspNetCore.SignalR;

namespace GameStore.Hubs
{
    public class ChatHub : Hub
    {
        private readonly LocalAiService _ai;

        public ChatHub(LocalAiService ai)
        {
            _ai = ai;
        }

        public async Task SendMessage(string message)
        {
            var userId = Context.ConnectionId;

            // 1. HIỂN THỊ USER NGAY
            await Clients.Caller.SendAsync("ReceiveMessage", "You", message);

            // 2. AI trả lời
            var answer = await _ai.AskAsync(userId, message);

            // 3. STREAM AI (đúng thứ tự sau user)
            await StreamText(userId, answer);
        }

        // ================= STREAM =================
        private async Task StreamText(string userId, string text)
        {
            string current = "";

            foreach (var c in text)
            {
                current += c;

                await Clients.Caller.SendAsync(
                    "ReceiveMessageStream",
                    "AI",
                    current
                );

                await Task.Delay(15);
            }
        }
    }
}