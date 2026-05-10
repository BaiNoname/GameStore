using GameStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Services
{
    public class EventMessageServiceImpl : EventMessageService
    {
        private readonly GameStoreContext db;

        public EventMessageServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        // Lấy thời gian hiện tại theo UTC
        private DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }
        
        // Lấy tất cả tin nhắn sự kiện theo ID sự kiện, có thể giới hạn số lượng trả về
        public List<EventMessage> GetByEvent(int eventId, int take = 100)
        {
            return db.EventMessages
                .Include(x => x.NguoiDung)
                .Where(x => x.EventId == eventId && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .Take(take)
                .OrderBy(x => x.CreatedAt)
                .ToList();
        }
        
        // Gửi tin nhắn sự kiện
        public bool Send(int eventId, int userId, string content)
        {
            try
            {
                content = content?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(content))
                    return false;

                var user = db.NguoiDungs.FirstOrDefault(x => x.MaNguoiDung == userId && x.IsActive);
                if (user == null)
                    return false;

                var message = new EventMessage
                {
                    EventId = eventId,
                    UserId = userId,
                    Content = content,
                    CreatedAt = UtcNow(),
                    IsDeleted = false
                };

                db.EventMessages.Add(message);
                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EVENT MESSAGE SEND ERROR: " + ex.Message);
                return false;
            }
        }
        
        // Lấy tin nhắn sự kiện mới nhất theo ID sự kiện và ID người dùng
        public EventMessage? GetLatestMessage(int eventId, int userId, string content)
        {
            content = content?.Trim() ?? "";

            return db.EventMessages
                .Include(x => x.NguoiDung)
                .Where(x => x.EventId == eventId
                         && x.UserId == userId
                         && x.Content == content
                         && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();
        }
        
        // Lấy tin nhắn sự kiện mới nhất theo ID sự kiện
        public EventMessage? GetLatestByEvent(int eventId)
        {
            return db.EventMessages
                .Include(x => x.NguoiDung)
                .Where(x => x.EventId == eventId && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();
        }
    }
}