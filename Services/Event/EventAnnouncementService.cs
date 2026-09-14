using GameStore.Models;

namespace GameStore.Services
{
    public interface EventAnnouncementService
    {
        // Lấy tất cả thông báo sự kiện theo ID sự kiện
        List<EventAnnouncement> GetByEvent(int eventId);
        // Lấy thông báo sự kiện theo ID
        EventAnnouncement? FindById(int id);
        // Lấy thông báo sự kiện mới nhất theo ID sự kiện
        EventAnnouncement? GetLatestByEvent(int eventId);
        // Tạo thông báo sự kiện mới
        bool Create(EventAnnouncement announcement);
        // Xóa thông báo sự kiện
        bool Delete(int id);
    }
}