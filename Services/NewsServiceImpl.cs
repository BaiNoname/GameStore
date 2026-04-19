using GameStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Services
{
    public class NewsServiceImpl : NewsService
    {
        private readonly GameStoreContext db;

        public NewsServiceImpl(GameStoreContext _db)
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

        public void ExpireOldNews()
        {
            try
            {
                var now = UtcNow();

                var expiredItems = db.News
                    .Where(x => x.Status != null
                             && x.Status.Trim().ToLower() == "published"
                             && x.ExpiredAt != null
                             && x.ExpiredAt <= now)
                    .ToList();

                if (expiredItems.Any())
                {
                    foreach (var item in expiredItems)
                    {
                        item.Status = "Expired";
                        item.UpdatedAt = now;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("NEWS EXPIRE ERROR: " + ex.Message);
            }
        }

        public List<News> FindAll(string keyword, string newsType, string status, int page, int pageSize, out int totalPages)
        {
            ExpireOldNews();

            var query = db.News
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

            if (!string.IsNullOrWhiteSpace(newsType) && newsType.Trim().ToLower() != "all")
            {
                var normalizedNewsType = newsType.Trim().ToLower();
                query = query.Where(x => x.NewsType != null && x.NewsType.Trim().ToLower() == normalizedNewsType);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.Trim().ToLower();
                query = query.Where(x => x.Status != null && x.Status.Trim().ToLower() == normalizedStatus);
            }

            int totalItems = query.Count();
            totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return query
                .OrderByDescending(x => x.IsFeatured)
                .ThenByDescending(x => x.PublishedAt)
                .ThenByDescending(x => x.NewsId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public List<News> FindPublished(string newsType, int page, int pageSize, out int totalPages)
        {
            ExpireOldNews();

            var now = UtcNow();

            var query = db.News
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .Where(x => x.Status != null
                         && x.Status.Trim().ToLower() == "published"
                         && (x.ExpiredAt == null || x.ExpiredAt > now))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(newsType) && newsType.Trim().ToLower() != "all")
            {
                var normalizedNewsType = newsType.Trim().ToLower();
                query = query.Where(x => x.NewsType != null && x.NewsType.Trim().ToLower() == normalizedNewsType);
            }

            int totalItems = query.Count();
            totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return query
                .OrderByDescending(x => x.IsFeatured)
                .ThenByDescending(x => x.PublishedAt)
                .ThenByDescending(x => x.NewsId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public List<News> GetFeatured(int take = 1)
        {
            ExpireOldNews();

            var now = UtcNow();

            return db.News
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .Where(x => x.Status != null
                         && x.Status.Trim().ToLower() == "published"
                         && (x.ExpiredAt == null || x.ExpiredAt > now)
                         && x.IsFeatured)
                .OrderByDescending(x => x.PublishedAt)
                .ThenByDescending(x => x.NewsId)
                .Take(take)
                .ToList();
        }

        public List<News> GetTrending(int take = 4)
        {
            ExpireOldNews();

            var now = UtcNow();

            return db.News
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .Where(x => x.Status != null
                         && x.Status.Trim().ToLower() == "published"
                         && (x.ExpiredAt == null || x.ExpiredAt > now))
                .OrderByDescending(x => x.ViewCount)
                .ThenByDescending(x => x.PublishedAt)
                .Take(take)
                .ToList();
        }

        public List<News> GetLatest(int take = 6)
        {
            ExpireOldNews();

            var now = UtcNow();

            return db.News
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .Where(x => x.Status != null
                         && x.Status.Trim().ToLower() == "published"
                         && (x.ExpiredAt == null || x.ExpiredAt > now))
                .OrderByDescending(x => x.PublishedAt)
                .ThenByDescending(x => x.NewsId)
                .Take(take)
                .ToList();
        }

        public News? FindById(int id)
        {
            ExpireOldNews();

            return db.News
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .FirstOrDefault(x => x.NewsId == id);
        }

        public News? FindBySlug(string slug)
        {
            ExpireOldNews();

            var now = UtcNow();

            if (string.IsNullOrWhiteSpace(slug))
                return null;

            slug = slug.Trim().ToLower();

            return db.News
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .FirstOrDefault(x => x.Slug != null
                                  && x.Slug.Trim().ToLower() == slug
                                  && x.Status != null
                                  && x.Status.Trim().ToLower() == "published"
                                  && (x.ExpiredAt == null || x.ExpiredAt > now));
        }

        public bool Create(News news)
        {
            try
            {
                var now = UtcNow();

                news.Title = news.Title?.Trim() ?? "";
                news.Slug = news.Slug?.Trim().ToLower() ?? "";
                news.Summary = news.Summary?.Trim();
                news.NewsType = string.IsNullOrWhiteSpace(news.NewsType) ? "General" : news.NewsType.Trim();
                news.Status = string.IsNullOrWhiteSpace(news.Status) ? "Published" : news.Status.Trim();
                news.CreatedAt = now;
                news.UpdatedAt = null;

                if (news.PublishedAt == default)
                    news.PublishedAt = now;
                else
                    news.PublishedAt = EnsureUtc(news.PublishedAt);

                if (news.ExpiredAt.HasValue)
                    news.ExpiredAt = EnsureUtc(news.ExpiredAt);
                else
                    news.ExpiredAt = news.PublishedAt.AddMonths(1);

                db.News.Add(news);
                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("NEWS CREATE ERROR: " + ex.Message);
                return false;
            }
        }

        public bool Update(News news)
        {
            try
            {
                var now = UtcNow();

                var existing = db.News.FirstOrDefault(x => x.NewsId == news.NewsId);
                if (existing == null) return false;

                existing.Title = string.IsNullOrWhiteSpace(news.Title) ? existing.Title : news.Title.Trim();
                existing.Slug = string.IsNullOrWhiteSpace(news.Slug) ? existing.Slug : news.Slug.Trim().ToLower();
                existing.Summary = news.Summary?.Trim();
                existing.Content = news.Content;
                existing.Thumbnail = string.IsNullOrWhiteSpace(news.Thumbnail) ? existing.Thumbnail : news.Thumbnail;
                existing.RelatedGameId = news.RelatedGameId;

                existing.NewsType = string.IsNullOrWhiteSpace(news.NewsType)
                    ? (string.IsNullOrWhiteSpace(existing.NewsType) ? "General" : existing.NewsType)
                    : news.NewsType.Trim();

                existing.Status = string.IsNullOrWhiteSpace(news.Status)
                    ? (string.IsNullOrWhiteSpace(existing.Status) ? "Published" : existing.Status)
                    : news.Status.Trim();

                existing.IsFeatured = news.IsFeatured;

                if (news.PublishedAt != default)
                    existing.PublishedAt = EnsureUtc(news.PublishedAt);

                existing.ExpiredAt = EnsureUtc(news.ExpiredAt);
                existing.UpdatedAt = now;

                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("NEWS UPDATE ERROR: " + ex.Message);
                return false;
            }
        }

        public bool Delete(int id)
        {
            try
            {
                var news = db.News.Find(id);
                if (news == null) return false;

                db.News.Remove(news);
                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("NEWS DELETE ERROR: " + ex.Message);
                return false;
            }
        }

        public bool IncreaseView(int id)
        {
            try
            {
                var news = db.News.Find(id);
                if (news == null) return false;

                news.ViewCount += 1;
                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("NEWS VIEW ERROR: " + ex.Message);
                return false;
            }
        }
    }
}