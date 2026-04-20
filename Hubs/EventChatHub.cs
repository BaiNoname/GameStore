using Microsoft.AspNetCore.SignalR;

namespace GameStore.Hubs
{
    public class EventChatHub : Hub
    {
        public async Task JoinEventRoom(string eventId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"event-room-{eventId}");
        }

        public async Task LeaveEventRoom(string eventId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"event-room-{eventId}");
        }
    }
}