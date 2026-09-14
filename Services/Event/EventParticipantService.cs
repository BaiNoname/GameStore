using GameStore.Models;

namespace GameStore.Services
{
    public interface EventParticipantService
    {
        // Kiểm tra xem người dùng đã tham gia sự kiện chưa
        bool IsJoined(int eventId, int userId);
        // Tham gia sự kiện miễn phí
        bool JoinFree(int eventId, int userId);
        // Tham gia sự kiện có trả phí
        bool JoinPaid(int eventId, int userId, decimal paidAmount);
        // Lấy danh sách người tham gia theo ID sự kiện
        List<EventParticipant> GetParticipantsByEvent(int eventId);
        // Đếm số lượng người tham gia theo ID sự kiện
        int CountJoined(int eventId);

        // Tìm người tham gia theo ID sự kiện và ID người dùng
        EventParticipant? FindParticipant(int eventId, int userId);
        // Check-in người tham gia
        bool CheckIn(int eventId, int userId);
        
        // Tìm người tham gia theo ID
        EventParticipant? FindById(int participantId);

        // Xóa người tham gia khỏi sự kiện
        bool RemoveParticipant(int participantId);

        // Lấy danh sách sự kiện mà người dùng đã tham gia
        List<EventParticipant> GetMyEvents(int userId);
    }
}