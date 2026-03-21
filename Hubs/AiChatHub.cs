using Microsoft.AspNetCore.SignalR;

namespace GameStore.Hubs
{
    public class AiChatHub : Hub
    {
        public async Task SendMessage(string message)
        {
            await Clients.Caller.SendAsync("ReceiveMessage", "user", message);

            var reply = $"AI Admin: {message}";

            await Clients.Caller.SendAsync("ReceiveMessage", "ai", reply);
        }
    }
}