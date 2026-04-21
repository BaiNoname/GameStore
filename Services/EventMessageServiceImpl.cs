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

        private DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }

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

        public bool Send(int eventId, int userId, string content)
        {
            try
            {
                content = content?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(content))
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