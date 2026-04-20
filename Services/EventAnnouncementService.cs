using GameStore.Models;

namespace GameStore.Services
{
    public interface EventAnnouncementService
    {
        List<EventAnnouncement> GetByEvent(int eventId);
        EventAnnouncement? FindById(int id);
        bool Create(EventAnnouncement announcement);
        bool Delete(int id);
    }
}