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

            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private DateTime? EnsureUtc(DateTime? value)
        {
            if (!value.HasValue) return null;
            return EnsureUtc(value.Value);
        }

        public void RefreshEventStatuses()
        {
            try
            {
                var now = DateTime.UtcNow;

                var events = db.Events.ToList();

                foreach (var ev in events)
                {
                    if (ev.StartAt > now)
                    {
                        ev.Status = "Upcoming";
                    }
                    else if (ev.StartAt <= now && ev.EndAt >= now)
                    {
                        ev.Status = "Live";
                    }
                    else if (ev.EndAt < now)
                    {
                        ev.Status = "Ended";
                    }

                    ev.UpdatedAt = now;
                }

                db.SaveChanges();
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
                var now = UtcNow();

                ev.Title = ev.Title?.Trim() ?? "";
                ev.Slug = ev.Slug?.Trim().ToLower() ?? "";
                ev.Summary = ev.Summary?.Trim();
                ev.EventType = string.IsNullOrWhiteSpace(ev.EventType) ? "Tournament" : ev.EventType.Trim();
                ev.AccessType = string.IsNullOrWhiteSpace(ev.AccessType) ? "Paid" : ev.AccessType.Trim();
                ev.Status = string.IsNullOrWhiteSpace(ev.Status) ? "Upcoming" : ev.Status.Trim();
                ev.CreatedAt = now;
                ev.UpdatedAt = null;
                ev.StartAt = EnsureUtc(ev.StartAt);
                ev.EndAt = EnsureUtc(ev.EndAt);

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
                var now = UtcNow();

                var current = db.Events.FirstOrDefault(x => x.EventId == ev.EventId);
                if (current == null) return false;

                current.Title = string.IsNullOrWhiteSpace(ev.Title) ? current.Title : ev.Title.Trim();
                current.Slug = string.IsNullOrWhiteSpace(ev.Slug) ? current.Slug : ev.Slug.Trim().ToLower();
                current.Summary = ev.Summary?.Trim();
                current.Content = ev.Content;
                current.Banner = ev.Banner;
                current.RelatedGameId = ev.RelatedGameId;
                current.EventType = string.IsNullOrWhiteSpace(ev.EventType) ? current.EventType : ev.EventType.Trim();
                current.AccessType = string.IsNullOrWhiteSpace(ev.AccessType) ? current.AccessType : ev.AccessType.Trim();
                current.Price = ev.Price;
                current.MaxParticipants = ev.MaxParticipants;
                current.PrizeInfo = ev.PrizeInfo?.Trim();
                current.Status = string.IsNullOrWhiteSpace(ev.Status) ? current.Status : ev.Status.Trim();
                current.StartAt = EnsureUtc(ev.StartAt);
                current.EndAt = EnsureUtc(ev.EndAt);
                current.UpdatedAt = now;

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