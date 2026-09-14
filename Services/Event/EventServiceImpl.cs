using GameStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Services
{
    public class EventServiceImpl : EventService
    {
        private readonly GameStoreContext db;

        public EventServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        // Lấy thời gian hiện tại theo UTC
        private DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }

        // Đảm bảo giá trị DateTime là UTC, nếu không sẽ chuyển đổi
        private DateTime EnsureUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
                return value;

            if (value.Kind == DateTimeKind.Local)
                return value.ToUniversalTime();

            return DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();
        }
        
        // Tính toán trạng thái của sự kiện dựa trên thời gian bắt đầu và kết thúc
        private string CalculateStatus(DateTime startAt, DateTime endAt)
        {
            var now = UtcNow();
            var startUtc = EnsureUtc(startAt);
            var endUtc = EnsureUtc(endAt);

            if (now < startUtc)
                return "Upcoming";

            if (now > endUtc)
                return "Ended";

            return "Live";
        }

        // Cập nhật trạng thái của tất cả sự kiện dựa trên thời gian hiện tại
        public void RefreshEventStatuses()
        {
            try
            {
                var events = db.Events.ToList();
                bool hasChanges = false;

                foreach (var ev in events)
                {
                    var newStatus = CalculateStatus(ev.StartAt, ev.EndAt);

                    // Chỉ cập nhật nếu trạng thái mới khác với trạng thái hiện tại để tránh ghi đè không cần thiết
                    if (!string.Equals(ev.Status, newStatus, StringComparison.OrdinalIgnoreCase))
                    {
                        ev.Status = newStatus;
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("EVENT REFRESH ERROR: " + ex.Message);
            }
        }

        // Tìm kiếm sự kiện với các bộ lọc và phân trang
        public List<Event> FindAll(string keyword, string eventType, string status, int page, int pageSize, out int totalPages)
        {
            RefreshEventStatuses();

            var query = db.Events
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .AsQueryable();

            // Bộ lọc tìm kiếm theo từ khóa trong Title hoặc Summary
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(x =>
                    x.Title.ToLower().Contains(keyword) ||
                    x.Summary != null && x.Summary.ToLower().Contains(keyword));
            }

            // Bộ lọc theo loại sự kiện, nếu không phải "all" thì sẽ so sánh sau khi đã chuẩn hóa
            if (!string.IsNullOrWhiteSpace(eventType) && eventType.Trim().ToLower() != "all")
            {
                var normalizedType = eventType.Trim().ToLower();
                query = query.Where(x => x.EventType != null && x.EventType.Trim().ToLower() == normalizedType);
            }

            // Bộ lọc theo trạng thái sự kiện, nếu không phải "all" thì sẽ so sánh sau khi đã chuẩn hóa
            if (!string.IsNullOrWhiteSpace(status) && status.Trim().ToLower() != "all")
            {
                var normalizedStatus = status.Trim().ToLower();
                query = query.Where(x => x.Status != null && x.Status.Trim().ToLower() == normalizedStatus);
            }

            int totalItems = query.Count();
            totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Sắp xếp sự kiện theo thứ tự ưu tiên: Live > Upcoming > Ended, sau đó theo thời gian bắt đầu và ID để đảm bảo thứ tự nhất quán
            return query
                .OrderByDescending(x => x.Status == "Live")
                .ThenByDescending(x => x.Status == "Upcoming")
                .ThenBy(x => x.StartAt)
                .ThenByDescending(x => x.EventId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        // Tìm kiếm sự kiện công khai với các bộ lọc và phân trang (chỉ trả về sự kiện có trạng thái Live hoặc Upcoming)
        public List<Event> FindPublic(string eventType, string status, int page, int pageSize, out int totalPages)
        {
            RefreshEventStatuses();

            var query = db.Events
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .AsQueryable();

            // Chỉ lấy sự kiện có trạng thái Live hoặc Upcoming
            if (!string.IsNullOrWhiteSpace(eventType) && eventType.Trim().ToLower() != "all")
            {
                var normalizedType = eventType.Trim().ToLower();
                query = query.Where(x => x.EventType != null && x.EventType.Trim().ToLower() == normalizedType);
            }

            // Bộ lọc theo trạng thái sự kiện, nếu không phải "all" thì sẽ so sánh sau khi đã chuẩn hóa
            if (!string.IsNullOrWhiteSpace(status) && status.Trim().ToLower() != "all")
            {
                var normalizedStatus = status.Trim().ToLower();
                query = query.Where(x => x.Status != null && x.Status.Trim().ToLower() == normalizedStatus);
            }

            int totalItems = query.Count();
            totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Sắp xếp sự kiện theo thứ tự ưu tiên: Live > Upcoming > Ended, sau đó theo thời gian bắt đầu và ID để đảm bảo thứ tự nhất quán
            return query
                .OrderByDescending(x => x.Status == "Live")
                .ThenByDescending(x => x.Status == "Upcoming")
                .ThenBy(x => x.StartAt)
                .ThenByDescending(x => x.EventId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        // Lấy sự kiện nổi bật nhất (Live > Upcoming > Ended) và có nhiều người tham gia nhất
        public List<Event> GetFeatured(int take = 1)
        {
            RefreshEventStatuses();

            return db.Events
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .OrderByDescending(x => x.Status == "Live")
                .ThenByDescending(x => x.CurrentParticipants)
                .ThenBy(x => x.StartAt)
                .Take(take)
                .ToList();
        }

        // Lấy các sự kiện sắp diễn ra (Upcoming) và có thời gian bắt đầu gần nhất
        public List<Event> GetUpcoming(int take = 6)
        {
            RefreshEventStatuses();

            return db.Events
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .Where(x => x.Status != null && x.Status.Trim().ToLower() == "upcoming")
                .OrderBy(x => x.StartAt)
                .Take(take)
                .ToList();
        }

        // Lấy các sự kiện đang diễn ra (Live) và có nhiều người tham gia nhất
        public List<Event> GetLive(int take = 6)
        {
            RefreshEventStatuses();

            return db.Events
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .Where(x => x.Status != null && x.Status.Trim().ToLower() == "live")
                .OrderByDescending(x => x.CurrentParticipants)
                .ThenBy(x => x.StartAt)
                .Take(take)
                .ToList();
        }

        // Lấy sự kiện theo ID, bao gồm thông tin liên quan như Game, NguoiDung, EventAnnouncements và EventParticipants
        public Event? FindById(int id)
        {
            RefreshEventStatuses();

            return db.Events
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .Include(x => x.EventAnnouncements.OrderByDescending(a => a.CreatedAt))
                .Include(x => x.EventParticipants)
                .FirstOrDefault(x => x.EventId == id);
        }

        // Lấy sự kiện theo Slug, bao gồm thông tin liên quan như Game, NguoiDung, EventAnnouncements và EventParticipants
        public Event? FindBySlug(string slug)
        {
            RefreshEventStatuses();

            if (string.IsNullOrWhiteSpace(slug))
                return null;

            slug = slug.Trim().ToLower();

            return db.Events
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .Include(x => x.EventAnnouncements.OrderByDescending(a => a.CreatedAt))
                .Include(x => x.EventParticipants)
                .FirstOrDefault(x => x.Slug != null && x.Slug.Trim().ToLower() == slug);
        }

        // Tạo sự kiện mới với các điều kiện kiểm tra hợp lệ về thời gian và dữ liệu đầu vào
        public bool Create(Event ev)
        {
            // Kiểm tra các trường bắt buộc và hợp lệ
            try
            {
                var nowUtc = UtcNow();

                ev.Title = ev.Title?.Trim() ?? "";
                ev.Slug = ev.Slug?.Trim().ToLower() ?? "";
                ev.Summary = ev.Summary?.Trim();
                ev.Content = ev.Content?.Trim() ?? "";
                ev.EventType = string.IsNullOrWhiteSpace(ev.EventType) ? "Tournament" : ev.EventType.Trim();
                ev.AccessType = string.IsNullOrWhiteSpace(ev.AccessType) ? "Paid" : ev.AccessType.Trim();
                ev.PrizeInfo = ev.PrizeInfo?.Trim();
                ev.PrizeType = string.IsNullOrWhiteSpace(ev.PrizeType) ? null : ev.PrizeType.Trim();
                ev.PrizeValue = string.IsNullOrWhiteSpace(ev.PrizeValue) ? null : ev.PrizeValue.Trim();
                ev.PrizeCondition = string.IsNullOrWhiteSpace(ev.PrizeCondition) ? null : ev.PrizeCondition.Trim();

                var startUtc = EnsureUtc(ev.StartAt);
                var endUtc = EnsureUtc(ev.EndAt);

                if (startUtc < nowUtc)
                    return false;

                if (endUtc < nowUtc)
                    return false;

                if (endUtc <= startUtc)
                    return false;

                ev.StartAt = startUtc;
                ev.EndAt = endUtc;
                ev.Status = CalculateStatus(ev.StartAt, ev.EndAt);
                ev.CreatedAt = nowUtc;
                ev.UpdatedAt = null;

                db.Events.Add(ev);
                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EVENT CREATE ERROR: " + ex.Message);
                return false;
            }
        }
        // Cập nhật sự kiện với các điều kiện kiểm tra hợp lệ về thời gian và dữ liệu đầu vào, chỉ cập nhật những trường được cung cấp

        public bool Update(Event ev)
        {
            // Kiểm tra các trường hợp ngoại lệ và chỉ cập nhật những trường được cung cấp, đồng thời đảm bảo rằng thời gian kết thúc mới hợp lệ nếu có thay đổi
            try
            {
                var nowUtc = UtcNow();

                var current = db.Events.FirstOrDefault(x => x.EventId == ev.EventId);
                if (current == null) return false;

                current.Title = string.IsNullOrWhiteSpace(ev.Title) ? current.Title : ev.Title.Trim();
                current.Slug = string.IsNullOrWhiteSpace(ev.Slug) ? current.Slug : ev.Slug.Trim().ToLower();
                current.Summary = ev.Summary?.Trim();
                current.Content = string.IsNullOrWhiteSpace(ev.Content) ? current.Content : ev.Content.Trim();
                current.Banner = ev.Banner;
                current.RelatedGameId = ev.RelatedGameId;
                current.EventType = string.IsNullOrWhiteSpace(ev.EventType) ? current.EventType : ev.EventType.Trim();
                current.AccessType = string.IsNullOrWhiteSpace(ev.AccessType) ? current.AccessType : ev.AccessType.Trim();
                current.Price = ev.Price;
                current.MaxParticipants = ev.MaxParticipants;
                current.PrizeInfo = ev.PrizeInfo?.Trim();
                current.PrizeType = string.IsNullOrWhiteSpace(ev.PrizeType) ? null : ev.PrizeType.Trim();
                current.PrizeValue = string.IsNullOrWhiteSpace(ev.PrizeValue) ? null : ev.PrizeValue.Trim();
                current.PrizeCondition = string.IsNullOrWhiteSpace(ev.PrizeCondition) ? null : ev.PrizeCondition.Trim();

                var currentEndUtc = EnsureUtc(current.EndAt);
                var postedEndUtc = EnsureUtc(ev.EndAt);

                bool endChanged = currentEndUtc != postedEndUtc;

                // chỉ validate và update EndAt khi admin thực sự sửa
                if (endChanged)
                {
                    var startUtc = EnsureUtc(current.StartAt);

                    if (postedEndUtc < nowUtc)
                        return false;

                    if (postedEndUtc <= startUtc)
                        return false;

                    current.EndAt = postedEndUtc;
                }

                current.Status = CalculateStatus(current.StartAt, current.EndAt);
                current.UpdatedAt = nowUtc;

                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EVENT UPDATE ERROR: " + ex.Message);
                return false;
            }
        }
        
        // Xóa sự kiện với các điều kiện kiểm tra hợp lệ về dữ liệu liên quan
        public bool Delete(int id)
        {
            // Kiểm tra xem sự kiện có tồn tại hay không, và nếu có, kiểm tra xem có giao dịch nào liên quan đến sự kiện đó hay không trước khi xóa để tránh mất dữ liệu quan trọng
            try
            {
                var ev = db.Events.FirstOrDefault(x => x.EventId == id);
                if (ev == null) return false;

                bool hasTransactions = db.GiaoDiches.Any(x => x.EventId == id);
                if (hasTransactions)
                {
                    Console.WriteLine("EVENT DELETE ERROR: Event has related transactions.");
                    return false;
                }

                db.Events.Remove(ev);
                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EVENT DELETE ERROR: " + ex.Message);
                return false;
            }
        }
        
        // Kiểm tra xem người dùng đã tham gia sự kiện hay chưa
        public bool IsUserJoined(int eventId, int userId)
        {
            return db.EventParticipants.Any(x =>
                x.EventId == eventId &&
                x.UserId == userId &&
                x.JoinStatus != null &&
                x.JoinStatus.Trim().ToLower() == "joined");
        }
    }
}