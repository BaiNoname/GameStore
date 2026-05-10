using GameStore.Models;

namespace GameStore.Services
{
    public interface EventMessageService
    {
        // Lấy tất cả tin nhắn sự kiện theo ID sự kiện, có thể giới hạn số lượng trả về
        List<EventMessage> GetByEvent(int eventId, int take = 100);
        // Gửi tin nhắn sự kiện
        bool Send(int eventId, int userId, string content);
        // Lấy tin nhắn sự kiện mới nhất theo ID sự kiện và ID người dùng
        EventMessage? GetLatestMessage(int eventId, int userId, string content);
        // Lấy tin nhắn sự kiện mới nhất theo ID sự kiện
        EventMessage? GetLatestByEvent(int eventId);
    }
}