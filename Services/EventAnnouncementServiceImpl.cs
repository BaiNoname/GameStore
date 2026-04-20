using GameStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Services
{
    public class EventAnnouncementServiceImpl : EventAnnouncementService
    {
        private readonly GameStoreContext db;

        public EventAnnouncementServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        private DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }

        public List<EventAnnouncement> GetByEvent(int eventId)
        {
            return db.EventAnnouncements
                .Include(x => x.NguoiDung)
                .Where(x => x.EventId == eventId)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
        }

        public EventAnnouncement? FindById(int id)
        {
            return db.EventAnnouncements
                .Include(x => x.NguoiDung)
                .Include(x => x.Event)
                .FirstOrDefault(x => x.AnnouncementId == id);
        }

        public bool Create(EventAnnouncement announcement)
        {
            try
            {
                announcement.Title = announcement.Title?.Trim() ?? "";
                announcement.Content = announcement.Content?.Trim() ?? "";
                announcement.CreatedAt = UtcNow();

                if (string.IsNullOrWhiteSpace(announcement.Title) || string.IsNullOrWhiteSpace(announcement.Content))
                    return false;

                db.EventAnnouncements.Add(announcement);
                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EVENT ANNOUNCEMENT CREATE ERROR: " + ex.Message);
                return false;
            }
        }

        public bool Delete(int id)
        {
            try
            {
                var item = db.EventAnnouncements.FirstOrDefault(x => x.AnnouncementId == id);
                if (item == null) return false;

                db.EventAnnouncements.Remove(item);
                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EVENT ANNOUNCEMENT DELETE ERROR: " + ex.Message);
                return false;
            }
        }
    }
}