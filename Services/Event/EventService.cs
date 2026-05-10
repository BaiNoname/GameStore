using GameStore.Models;

namespace GameStore.Services
{
    public interface EventService
    {
        // Tìm kiếm sự kiện với các tiêu chí: từ khóa, loại sự kiện, trạng thái, phân trang
        List<Event> FindAll(string keyword, string eventType, string status, int page, int pageSize, out int totalPages);
        // Tìm kiếm sự kiện công khai với các tiêu chí: loại sự kiện, trạng thái, phân trang
        List<Event> FindPublic(string eventType, string status, int page, int pageSize, out int totalPages);
        // Lấy danh sách sự kiện nổi bật
        List<Event> GetFeatured(int take = 1);
        // Lấy danh sách sự kiện sắp diễn ra
        List<Event> GetUpcoming(int take = 6);
        // Lấy danh sách sự kiện đang diễn ra
        List<Event> GetLive(int take = 6);
        // Tìm kiếm sự kiện theo ID hoặc slug
        Event? FindById(int id);
        // Tìm kiếm sự kiện theo slug
        Event? FindBySlug(string slug);
        // Tạo sự kiện mới
        bool Create(Event ev);
        // Cập nhật sự kiện
        bool Update(Event ev);
        // Xóa sự kiện
        bool Delete(int id);
        // Làm mới trạng thái của các sự kiện
        void RefreshEventStatuses();
        // Kiểm tra xem người dùng đã tham gia sự kiện hay chưa
        bool IsUserJoined(int eventId, int userId);
    }
}