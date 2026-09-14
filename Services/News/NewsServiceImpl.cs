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

        // Lấy thời gian hiện tại theo UTC
        private DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }

        // Đảm bảo giá trị DateTime là UTC
        private DateTime EnsureUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
                return value;

            if (value.Kind == DateTimeKind.Local)
                return value.ToUniversalTime();

            return DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();
        }

        // Phiên bản EnsureUtc cho DateTime? (nullable)
        private DateTime? EnsureUtc(DateTime? value)
        {
            if (!value.HasValue) return null;
            return EnsureUtc(value.Value);
        }

        // Tự động chuyển trạng thái các tin tức đã hết hạn sang "Expired"
        public void ExpireOldNews()
        {
            try
            {
                var now = UtcNow();

                // Tìm tất cả tin tức có trạng thái "Published" và đã hết hạn
                var expiredItems = db.News
                    .Where(x => x.Status != null
                             && x.Status.Trim().ToLower() == "published"
                             && x.ExpiredAt != null
                             && x.ExpiredAt <= now)
                    .ToList();

                // Nếu có tin tức nào cần cập nhật, thực hiện cập nhật trạng thái và thời gian cập nhật
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

        // Tìm kiếm tin tức với các điều kiện lọc và phân trang
        public List<News> FindAll(string keyword, string newsType, string status, int page, int pageSize, out int totalPages)
        {
            // Trước khi thực hiện tìm kiếm, tự động chuyển trạng thái các tin tức đã hết hạn sang "Expired"
            ExpireOldNews();

            // Bắt đầu truy vấn từ bảng News, bao gồm thông tin liên quan từ bảng Game và NguoiDung
            var query = db.News
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .AsQueryable();

            // Áp dụng bộ lọc tìm kiếm theo từ khóa trên tiêu đề và tóm tắt
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(x =>
                    x.Title.ToLower().Contains(keyword) ||
                    x.Summary != null && x.Summary.ToLower().Contains(keyword));
            }

            // Áp dụng bộ lọc theo loại tin tức nếu được chỉ định và không phải "all"
            if (!string.IsNullOrWhiteSpace(newsType) && newsType.Trim().ToLower() != "all")
            {
                var normalizedNewsType = newsType.Trim().ToLower();
                query = query.Where(x => x.NewsType != null && x.NewsType.Trim().ToLower() == normalizedNewsType);
            }

            // Áp dụng bộ lọc theo trạng thái nếu được chỉ định
            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.Trim().ToLower();
                query = query.Where(x => x.Status != null && x.Status.Trim().ToLower() == normalizedStatus);
            }

            int totalItems = query.Count();
            totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Sắp xếp kết quả: ưu tiên tin nổi bật, sau đó mới đến thời gian đăng và ID
            return query
                .OrderByDescending(x => x.IsFeatured)
                .ThenByDescending(x => x.PublishedAt)
                .ThenByDescending(x => x.NewsId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        // Tìm kiếm tin tức đã được xuất bản với các điều kiện lọc và phân trang
        public List<News> FindPublished(string newsType, int page, int pageSize, out int totalPages)
        {
            // Trước khi thực hiện tìm kiếm, tự động chuyển trạng thái các tin tức đã hết hạn sang "Expired"
            ExpireOldNews();

            var now = UtcNow();

            // Bắt đầu truy vấn từ bảng News, bao gồm thông tin liên quan từ bảng Game và NguoiDung, chỉ lấy những tin tức có trạng thái "Published" và chưa hết hạn
            var query = db.News
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .Where(x => x.Status != null
                         && x.Status.Trim().ToLower() == "published"
                         && (x.ExpiredAt == null || x.ExpiredAt > now))
                .AsQueryable();

            // Áp dụng bộ lọc theo loại tin tức nếu được chỉ định và không phải "all"
            if (!string.IsNullOrWhiteSpace(newsType) && newsType.Trim().ToLower() != "all")
            {
                var normalizedNewsType = newsType.Trim().ToLower();
                query = query.Where(x => x.NewsType != null && x.NewsType.Trim().ToLower() == normalizedNewsType);
            }

            int totalItems = query.Count();
            totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Sắp xếp kết quả: ưu tiên tin nổi bật, sau đó mới đến thời gian đăng và ID
            return query
                .OrderByDescending(x => x.IsFeatured)
                .ThenByDescending(x => x.PublishedAt)
                .ThenByDescending(x => x.NewsId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        // Lấy danh sách tin tức nổi bật đã được xuất bản và chưa hết hạn, sắp xếp theo thời gian đăng và ID
        public List<News> GetFeatured(int take = 1)
        {
            ExpireOldNews();

            var now = UtcNow();

            // Lấy những tin tức có trạng thái "Published", chưa hết hạn và được đánh dấu là nổi bật, sắp xếp theo thời gian đăng và ID
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

        // Lấy danh sách tin tức nổi bật đã được xuất bản và chưa hết hạn, sắp xếp theo số lượt xem, thời gian đăng và ID
        public List<News> GetTrending(int take = 4)
        {
            ExpireOldNews();

            var now = UtcNow();

            // Lấy những tin tức có trạng thái "Published", chưa hết hạn, sắp xếp theo số lượt xem, thời gian đăng và ID
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

        // Lấy danh sách tin tức mới nhất đã được xuất bản và chưa hết hạn, sắp xếp theo thời gian đăng và ID
        public List<News> GetLatest(int take = 6)
        {
            ExpireOldNews();

            var now = UtcNow();

            // Lấy những tin tức có trạng thái "Published", chưa hết hạn, sắp xếp theo thời gian đăng và ID
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

        // Tìm kiếm tin tức theo ID, bao gồm thông tin liên quan từ bảng Game và NguoiDung
        public News? FindById(int id)
        {
            ExpireOldNews();

            // Tìm kiếm tin tức theo ID, bao gồm thông tin liên quan từ bảng Game và NguoiDung
            return db.News
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .FirstOrDefault(x => x.NewsId == id);
        }

        // Tìm kiếm tin tức theo Slug, chỉ lấy những tin tức có trạng thái "Published" và chưa hết hạn, bao gồm thông tin liên quan từ bảng Game và NguoiDung
        public News? FindBySlug(string slug)
        {
            ExpireOldNews();

            var now = UtcNow();

            if (string.IsNullOrWhiteSpace(slug))
                return null;

            slug = slug.Trim().ToLower();

            // Tìm kiếm tin tức theo Slug, chỉ lấy những tin tức có trạng thái "Published" và chưa hết hạn, bao gồm thông tin liên quan từ bảng Game và NguoiDung
            return db.News
                .Include(x => x.Game)
                .Include(x => x.NguoiDung)
                .FirstOrDefault(x => x.Slug != null
                                  && x.Slug.Trim().ToLower() == slug
                                  && x.Status != null
                                  && x.Status.Trim().ToLower() == "published"
                                  && (x.ExpiredAt == null || x.ExpiredAt > now));
        }

        // Tạo mới một tin tức, tự động xử lý các trường dữ liệu như thời gian đăng, thời gian hết hạn, trạng thái và loại tin tức
        public bool Create(News news)
        {
            try
            {
                var now = UtcNow();

                // Xử lý các trường dữ liệu: loại bỏ khoảng trắng thừa, chuẩn hóa chữ thường, và thiết lập giá trị mặc định nếu cần
                news.Title = news.Title?.Trim() ?? "";
                news.Slug = news.Slug?.Trim().ToLower() ?? "";
                news.Summary = news.Summary?.Trim();
                news.NewsType = string.IsNullOrWhiteSpace(news.NewsType) ? "General" : news.NewsType.Trim();
                news.Status = string.IsNullOrWhiteSpace(news.Status) ? "Published" : news.Status.Trim();
                news.CreatedAt = now;
                news.UpdatedAt = null;

                // Nếu PublishedAt không được cung cấp, mặc định là thời gian hiện tại; nếu đã cung cấp, đảm bảo là UTC
                if (news.PublishedAt == default)
                    news.PublishedAt = now;
                else
                    news.PublishedAt = EnsureUtc(news.PublishedAt);

                // Nếu ExpiredAt đã được cung cấp, đảm bảo là UTC; nếu không, mặc định là 1 tháng sau thời gian đăng
                if (news.ExpiredAt.HasValue)
                    news.ExpiredAt = EnsureUtc(news.ExpiredAt);
                else
                    news.ExpiredAt = news.PublishedAt.AddMonths(1);

                // Thêm tin tức mới vào cơ sở dữ liệu
                db.News.Add(news);
                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("NEWS CREATE ERROR: " + ex.Message);
                return false;
            }
        }

        // Cập nhật một tin tức đã tồn tại, chỉ cập nhật những trường dữ liệu được cung cấp và khác với giá trị cũ, tự động xử lý các trường dữ liệu như thời gian đăng, thời gian hết hạn, trạng thái và loại tin tức
        public bool Update(News news)
        {
            try
            {
                var now = UtcNow();

                // Tìm tin tức đã tồn tại theo ID
                var existing = db.News.FirstOrDefault(x => x.NewsId == news.NewsId);
                if (existing == null) return false;

                // Cập nhật các trường dữ liệu nếu có giá trị mới và khác với giá trị cũ, đồng thời xử lý các trường dữ liệu như loại bỏ khoảng trắng thừa, chuẩn hóa chữ thường, và thiết lập giá trị mặc định nếu cần
                existing.Title = string.IsNullOrWhiteSpace(news.Title) ? existing.Title : news.Title.Trim();
                existing.Slug = string.IsNullOrWhiteSpace(news.Slug) ? existing.Slug : news.Slug.Trim().ToLower();
                existing.Summary = news.Summary?.Trim();
                existing.Content = news.Content;
                existing.Thumbnail = string.IsNullOrWhiteSpace(news.Thumbnail) ? existing.Thumbnail : news.Thumbnail;
                existing.RelatedGameId = news.RelatedGameId;

                // Nếu AuthorUserId được cung cấp và khác với giá trị cũ, cập nhật AuthorUserId
                existing.NewsType = string.IsNullOrWhiteSpace(news.NewsType)
                    ? string.IsNullOrWhiteSpace(existing.NewsType) ? "General" : existing.NewsType
                    : news.NewsType.Trim();

                // Nếu Status được cung cấp và khác với giá trị cũ, cập nhật Status
                existing.Status = string.IsNullOrWhiteSpace(news.Status)
                    ? string.IsNullOrWhiteSpace(existing.Status) ? "Published" : existing.Status
                    : news.Status.Trim();

                // IsFeatured: chỉ update nếu khác giá trị cũ
                existing.IsFeatured = news.IsFeatured;

                // PublishedAt: chỉ update nếu khác giá trị cũ
                if (news.PublishedAt != default)
                {
                    var currentPublishedUtc = EnsureUtc(existing.PublishedAt);
                    var postedPublishedUtc = EnsureUtc(news.PublishedAt);

                    if (currentPublishedUtc != postedPublishedUtc)
                    {
                        existing.PublishedAt = postedPublishedUtc;
                    }
                }

                // ExpiredAt: chỉ update nếu khác giá trị cũ
                var currentExpiredUtc = EnsureUtc(existing.ExpiredAt);
                var postedExpiredUtc = EnsureUtc(news.ExpiredAt);

                // Xác định xem ExpiredAt có thay đổi hay không, bao gồm cả trường hợp từ null sang có giá trị, từ có giá trị sang null, hoặc từ một giá trị này sang một giá trị khác
                bool expiredChanged =
                    currentExpiredUtc == null && postedExpiredUtc != null ||
                    currentExpiredUtc != null && postedExpiredUtc == null ||
                    currentExpiredUtc != null && postedExpiredUtc != null && currentExpiredUtc.Value != postedExpiredUtc.Value;

                // Nếu ExpiredAt có thay đổi, cập nhật ExpiredAt
                if (expiredChanged)
                {
                    existing.ExpiredAt = postedExpiredUtc;
                }

                existing.UpdatedAt = now;

                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("NEWS UPDATE ERROR: " + ex.Message);
                return false;
            }
        }

        // Xóa một tin tức theo ID
        public bool Delete(int id)
        {
            // Tìm tin tức theo ID và xóa nếu tồn tại
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
        
        // Tăng số lượt xem của một tin tức
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