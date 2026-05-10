using GameStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Services
{
    public class EventParticipantServiceImpl : EventParticipantService
    {
        private readonly GameStoreContext db;

        public EventParticipantServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        // Kiểm tra user còn hoạt động hay không
        private bool IsUserActive(int userId)
        {
            return db.NguoiDungs.Any(x => x.MaNguoiDung == userId && x.IsActive);
        }

        // Lấy thời gian hiện tại theo UTC
        private DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }

        // Kiểm tra xem user đã tham gia sự kiện chưa
        public bool IsJoined(int eventId, int userId)
        {
            return db.EventParticipants.Any(x =>
                x.EventId == eventId &&
                x.UserId == userId &&
                x.JoinStatus != null &&
                x.JoinStatus.Trim().ToLower() == "joined");
        }

        // Đếm số lượng người đã tham gia sự kiện
        public int CountJoined(int eventId)
        {
            return db.EventParticipants.Count(x =>
                x.EventId == eventId &&
                x.JoinStatus != null &&
                x.JoinStatus.Trim().ToLower() == "joined");
        }
        
        // Lấy danh sách người tham gia theo ID sự kiện
        public List<EventParticipant> GetParticipantsByEvent(int eventId)
        {
            return db.EventParticipants
                .Include(x => x.NguoiDung)
                .Where(x => x.EventId == eventId)
                .OrderByDescending(x => x.JoinedAt)
                .ToList();
        }
        
        // Tìm người tham gia theo ID sự kiện và ID người dùng
        public EventParticipant? FindParticipant(int eventId, int userId)
        {
            return db.EventParticipants
                .Include(x => x.NguoiDung)
                .FirstOrDefault(x => x.EventId == eventId && x.UserId == userId);
        }
        
        // Tham gia sự kiện miễn phí
        public bool JoinFree(int eventId, int userId)
        {
            if (!IsUserActive(userId))
                return false;

            try
            {
                if (IsJoined(eventId, userId))
                    return true;

                var ev = db.Events.FirstOrDefault(x => x.EventId == eventId);
                if (ev == null) return false;

                // Kiểm tra nếu sự kiện đã đạt giới hạn người tham gia
                if (ev.MaxParticipants.HasValue && ev.CurrentParticipants >= ev.MaxParticipants.Value)
                    return false;

                // Tạo đối tượng EventParticipant mới
                var participant = new EventParticipant
                {
                    EventId = eventId,
                    UserId = userId,
                    JoinStatus = "Joined",
                    PaidAmount = 0,
                    JoinedAt = UtcNow(),
                    IsCheckedIn = false
                };

                // Thêm người tham gia vào cơ sở dữ liệu
                db.EventParticipants.Add(participant);
                ev.CurrentParticipants += 1;
                ev.UpdatedAt = UtcNow();

                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EVENT JOIN FREE ERROR: " + ex.Message);
                return false;
            }
        }
        
        // Tham gia sự kiện có trả phí
        public bool JoinPaid(int eventId, int userId, decimal paidAmount)
        {
            if (!IsUserActive(userId))
                return false;

            try
            {
                if (IsJoined(eventId, userId))
                    return true;

                // Kiểm tra nếu sự kiện đã đạt giới hạn người tham gia
                var ev = db.Events.FirstOrDefault(x => x.EventId == eventId);
                if (ev == null) return false;

                // Kiểm tra nếu sự kiện đã đạt giới hạn người tham gia
                if (ev.MaxParticipants.HasValue && ev.CurrentParticipants >= ev.MaxParticipants.Value)
                    return false;

                // Tạo đối tượng EventParticipant mới
                var participant = new EventParticipant
                {
                    EventId = eventId,
                    UserId = userId,
                    JoinStatus = "Joined",
                    PaidAmount = paidAmount,
                    JoinedAt = UtcNow(),
                    IsCheckedIn = false
                };

                // Thêm người tham gia vào cơ sở dữ liệu
                db.EventParticipants.Add(participant);
                ev.CurrentParticipants += 1;
                ev.UpdatedAt = UtcNow();

                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EVENT JOIN PAID ERROR: " + ex.Message);
                return false;
            }
        }
        
        // Check-in người tham gia
        public bool CheckIn(int eventId, int userId)
        {
            if (!IsUserActive(userId))
                return false;

            try
            {
                var participant = db.EventParticipants
                    .FirstOrDefault(x => x.EventId == eventId && x.UserId == userId);

                if (participant == null)
                    return false;

                if (participant.IsCheckedIn)
                    return true;

                participant.IsCheckedIn = true;
                participant.CheckedInAt = UtcNow();

                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EVENT CHECKIN ERROR: " + ex.Message);
                return false;
            }
        }
        
        // Tìm người tham gia theo ID
        public EventParticipant? FindById(int participantId)
        {
            return db.EventParticipants
                .Include(x => x.NguoiDung)
                .Include(x => x.Event)
                .FirstOrDefault(x => x.ParticipantId == participantId);
        }
        
        // Xóa người tham gia khỏi sự kiện
        public bool RemoveParticipant(int participantId)
        {
            // Lấy người tham gia để kiểm tra và cập nhật số lượng người tham gia của sự kiện
            try
            {
                var participant = db.EventParticipants
                    .Include(x => x.Event)
                    .FirstOrDefault(x => x.ParticipantId == participantId);

                if (participant == null)
                    return false;

                var ev = participant.Event;
                if (ev != null && ev.CurrentParticipants > 0)
                {
                    ev.CurrentParticipants -= 1;
                    ev.UpdatedAt = DateTime.UtcNow;
                }

                db.EventParticipants.Remove(participant);
                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EVENT REMOVE PARTICIPANT ERROR: " + ex.Message);
                return false;
            }
        }
        
        // Lấy danh sách sự kiện mà người dùng đã tham gia
        public List<EventParticipant> GetMyEvents(int userId)
        {
            if (!IsUserActive(userId))
                return new List<EventParticipant>();

            // Lấy danh sách sự kiện mà người dùng đã tham gia, bao gồm thông tin về sự kiện và trò chơi
            return db.EventParticipants
                .Include(x => x.Event)
                    .ThenInclude(e => e.Game)
                .Include(x => x.NguoiDung)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.JoinedAt)
                .ToList();
        }

    }
}