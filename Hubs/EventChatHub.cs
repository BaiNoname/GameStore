using Microsoft.AspNetCore.SignalR;

namespace GameStore.Hubs
{
    public class EventChatHub : Hub
    {
        // Phương thức để người dùng tham gia phòng chat của sự kiện, sử dụng Groups của SignalR để quản lý các phòng chat
        public async Task JoinEventRoom(string eventId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"event-room-{eventId}");
        }

        // Phương thức để người dùng rời khỏi phòng chat của sự kiện
        public async Task LeaveEventRoom(string eventId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"event-room-{eventId}");
        }
    }
}