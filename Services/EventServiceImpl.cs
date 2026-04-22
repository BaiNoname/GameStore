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

        private DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }

        private DateTime EnsureUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
                return value;

            if (value.Kind == DateTimeKind.Local)
                return value.ToUniversalTime();

            return DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();
        }

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

        public void RefreshEventStatuses()
        {
            try
            {
                var events = db.Events.ToList();
                bool hasChanges = false;

                foreach (var ev in events)
                {
                    var newStatus = CalculateStatus(ev.StartAt, ev.EndAt);

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

        public List<Event> FindAll(string keyword, string eventType, string status, int page, int pageSize, out int totalPages)
        {
            RefreshEventStatuses();

            var query = db.Events
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(x =>
                    x.Title.ToLower().Contains(keyword) ||
                    (x.Summary != null && x.Summary.ToLower().Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(eventType) && eventType.Trim().ToLower() != "all")
            {
                var normalizedType = eventType.Trim().ToLower();
                query = query.Where(x => x.EventType != null && x.EventType.Trim().ToLower() == normalizedType);
            }

            if (!string.IsNullOrWhiteSpace(status) && status.Trim().ToLower() != "all")
            {
                var normalizedStatus = status.Trim().ToLower();
                query = query.Where(x => x.Status != null && x.Status.Trim().ToLower() == normalizedStatus);
            }

            int totalItems = query.Count();
            totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return query
                .OrderByDescending(x => x.Status == "Live")
                .ThenByDescending(x => x.Status == "Upcoming")
                .ThenBy(x => x.StartAt)
                .ThenByDescending(x => x.EventId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public List<Event> FindPublic(string eventType, string status, int page, int pageSize, out int totalPages)
        {
            RefreshEventStatuses();

            var query = db.Events
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(eventType) && eventType.Trim().ToLower() != "all")
            {
                var normalizedType = eventType.Trim().ToLower();
                query = query.Where(x => x.EventType != null && x.EventType.Trim().ToLower() == normalizedType);
            }

            if (!string.IsNullOrWhiteSpace(status) && status.Trim().ToLower() != "all")
            {
                var normalizedStatus = status.Trim().ToLower();
                query = query.Where(x => x.Status != null && x.Status.Trim().ToLower() == normalizedStatus);
            }

            int totalItems = query.Count();
            totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return query
                .OrderByDescending(x => x.Status == "Live")
                .ThenByDescending(x => x.Status == "Upcoming")
                .ThenBy(x => x.StartAt)
                .ThenByDescending(x => x.EventId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

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

        public bool Create(Event ev)
        {
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

        public bool Update(Event ev)
        {
            try
            {
                var nowUtc = UtcNow();

                var current = db.Events.FirstOrDefault(x => x.EventId == ev.EventId);
                if (current == null) return false;

                var startUtc = current.StartAt;
                var endUtc = EnsureUtc(ev.EndAt);

                if (endUtc < nowUtc)
                    return false;

                if (endUtc <= startUtc)
                    return false;

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

                current.EndAt = endUtc;
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

        public bool Delete(int id)
        {
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