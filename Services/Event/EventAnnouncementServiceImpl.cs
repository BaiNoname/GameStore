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

        // Lấy thời gian hiện tại theo UTC
        private DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }

        // Lấy tất cả thông báo sự kiện theo ID sự kiện
        public List<EventAnnouncement> GetByEvent(int eventId)
        {
            return db.EventAnnouncements
                .Include(x => x.NguoiDung)
                .Where(x => x.EventId == eventId)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
        }

        // Lấy thông báo sự kiện theo ID thông báo
        public EventAnnouncement? FindById(int id)
        {
            // Sử dụng Include để lấy thông tin người dùng và sự kiện liên quan
            return db.EventAnnouncements
                .Include(x => x.NguoiDung)
                .Include(x => x.Event)
                .FirstOrDefault(x => x.AnnouncementId == id);
        }
        
        // Tạo thông báo sự kiện mới
        public bool Create(EventAnnouncement announcement)
        {
            try
            {
                // Chuẩn hóa dữ liệu đầu vào
                announcement.Title = announcement.Title?.Trim() ?? "";
                announcement.Content = announcement.Content?.Trim() ?? "";
                announcement.CreatedAt = UtcNow();

                // Kiểm tra dữ liệu đầu vào
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
        
        // Xóa thông báo sự kiện
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
        
        // Lấy thông báo sự kiện mới nhất theo ID sự kiện
        public EventAnnouncement? GetLatestByEvent(int eventId)
        {
            return db.EventAnnouncements
                .Include(x => x.NguoiDung)
                .Where(x => x.EventId == eventId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();
        }
    }
}