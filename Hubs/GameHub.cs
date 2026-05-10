using Microsoft.AspNetCore.SignalR;

namespace GameStore.Hubs
{
    public class GameHub : Hub
    {
        // Phương thức để người chơi tham gia vào một game cụ thể, sử dụng Groups của SignalR để quản lý các nhóm kết nối theo gameId
        public async Task JoinGame(string gameId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        }
    }
}