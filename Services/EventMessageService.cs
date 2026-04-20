using GameStore.Models;

namespace GameStore.Services
{
    public interface EventMessageService
    {
        List<EventMessage> GetByEvent(int eventId, int take = 100);
        bool Send(int eventId, int userId, string content);
    }
}