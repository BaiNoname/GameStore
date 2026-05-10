using GameStore.Models;

namespace GameStore.Pagination.User
{
    // ViewModel cho thẻ sự kiện của người dùng, chứa thông tin về người tham gia, sự kiện, thông báo và tin nhắn mới nhất
    public class MyEventCardVM
    {
        // Thông tin về người tham gia sự kiện, không thể null
        public EventParticipant Participant { get; set; } = null!;
        // Thông tin về sự kiện, có thể null nếu sự kiện đã kết thúc hoặc bị xóa
        public Event? Event { get; set; }
        // Thông tin về thông báo mới nhất của sự kiện, có thể null nếu không có thông báo
        public EventAnnouncement? LatestAnnouncement { get; set; }
        // Thông tin về tin nhắn mới nhất của sự kiện, có thể null nếu không có tin nhắn
        public EventMessage? LatestMessage { get; set; }
    }
}