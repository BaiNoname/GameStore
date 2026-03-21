using Microsoft.AspNetCore.SignalR;

namespace GameStore.Hubs
{
    public class GameHub : Hub
    {
        public async Task JoinGame(string gameId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        }
    }
}