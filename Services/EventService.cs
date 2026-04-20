using GameStore.Models;

namespace GameStore.Services
{
    public interface EventService
    {
        List<Event> FindAll(string keyword, string eventType, string status, int page, int pageSize, out int totalPages);
        List<Event> FindPublic(string eventType, string status, int page, int pageSize, out int totalPages);
        List<Event> GetFeatured(int take = 1);
        List<Event> GetUpcoming(int take = 6);
        List<Event> GetLive(int take = 6);
        Event? FindById(int id);
        Event? FindBySlug(string slug);
        bool Create(Event ev);
        bool Update(Event ev);
        bool Delete(int id);
        void RefreshEventStatuses();
        bool IsUserJoined(int eventId, int userId);
    }
}