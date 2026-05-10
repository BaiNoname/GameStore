using Microsoft.AspNetCore.SignalR;

namespace GameStore.Hubs
{
    public class AiChatHub : Hub
    {
        // Phương thức để gửi tin nhắn từ client đến server và nhận phản hồi từ AI Admin
        public async Task SendMessage(string message)
        {
            await Clients.Caller.SendAsync("ReceiveMessage", "user", message);

            var reply = $"AI Admin: {message}";

            await Clients.Caller.SendAsync("ReceiveMessage", "ai", reply);
        }
    }
}