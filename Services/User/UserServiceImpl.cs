using GameStore.Models;

namespace GameStore.Services
{
    public class UserServiceImpl : UserService
    {
        private readonly GameStoreContext db;

        public UserServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        // Lấy thời gian hiện tại theo UTC
        private DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }

        // Tạo người dùng mới
        public bool Create(NguoiDung user)
        {
            try
            {
                if (user == null)
                    return false;

                // Chuẩn hóa dữ liệu đầu vào
                user.Email = user.Email?.Trim().ToLower();
                user.TenNguoiDung = user.TenNguoiDung?.Trim();
                user.Quyen = (user.Quyen ?? "").Trim().ToLower();

                // Kiểm tra các trường bắt buộc
                if (string.IsNullOrEmpty(user.Email) ||
                    string.IsNullOrEmpty(user.MatKhau) ||
                    string.IsNullOrEmpty(user.TenNguoiDung))
                    return false;
                
                // Kiểm tra định dạng email
                if (!System.Text.RegularExpressions.Regex.IsMatch(user.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    return false;
                
                // Kiểm tra độ dài mật khẩu
                if (user.MatKhau.Length < 5)
                    return false;
                
                // Kiểm tra email đã tồn tại
                if (db.NguoiDungs.Any(u => u.Email == user.Email))
                    return false;

                // Đảm bảo quyền hợp lệ
                if (user.Quyen != "admin" && user.Quyen != "user")
                    user.Quyen = "user";

                user.SoDu = 0;
                user.NgayDangKy = DateOnly.FromDateTime(UtcNow());
                user.GioHang = null;
                user.IsActive = true;

                // Mã hóa mật khẩu trước khi lưu
                user.MatKhau = BCrypt.Net.BCrypt.HashPassword(user.MatKhau);

                db.NguoiDungs.Add(user);
                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR CREATE USER: " + ex.Message);
                return false;
            }
        }

        // Chuyển trạng thái người dùng (đánh dấu không hoạt động)
        public bool Delete(int id, int currentUserId)
        {
            try
            {
                var user = db.NguoiDungs.Find(id);
                if (user == null)
                    return false;

                // Không cho phép người dùng tự vô hiệu hóa tài khoản của mình
                if (user.MaNguoiDung == currentUserId)
                    return false;

                // Không cho phép vô hiệu hóa tài khoản admin
                if ((user.Quyen ?? "").Trim().ToLower() == "admin")
                    return false;

                // Nếu đã vô hiệu hóa rồi thì không cần làm gì
                if (!user.IsActive)
                    return false;

                user.IsActive = false;
                return db.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }

        // Kích hoạt lại người dùng đã bị vô hiệu hóa
        public bool Activate(int id)
        {
            try
            {
                // Không cho phép kích hoạt lại tài khoản admin đã bị vô hiệu hóa
                var user = db.NguoiDungs.Find(id);
                if (user == null)
                    return false;

                if (user.IsActive)
                    return true;

                user.IsActive = true;
                return db.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }

        // Tìm kiếm người dùng với phân trang và lọc theo trạng thái
        public List<NguoiDung> findAll(string keyword, string status, int page, int pageSize, out int totalPages)
        {
            // Chuẩn hóa tham số đầu vào
            var query = db.NguoiDungs.AsQueryable();

            // Lọc theo từ khóa nếu có
            if (!string.IsNullOrEmpty(keyword))
            {
                // Chuẩn hóa từ khóa để so sánh không phân biệt hoa thường
                keyword = keyword.Trim().ToLower();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(keyword) ||
                    u.TenNguoiDung.ToLower().Contains(keyword));
            }

            // Lọc theo trạng thái nếu có
            if (!string.IsNullOrWhiteSpace(status))
            {
                // Chuẩn hóa trạng thái để so sánh không phân biệt hoa thường
                var normalized = status.Trim().ToLower();

                // Chỉ chấp nhận "active" hoặc "inactive" làm giá trị hợp lệ
                if (normalized == "active")
                    query = query.Where(u => u.IsActive);
                else if (normalized == "inactive")
                    query = query.Where(u => !u.IsActive);
            }

            // Sắp xếp kết quả: ưu tiên người dùng đang hoạt động và mới đăng ký hơn
            query = query
                .OrderByDescending(u => u.IsActive)
                .ThenByDescending(u => u.NgayDangKy);

            int totalItems = query.Count();
            totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        // Tìm người dùng theo ID
        public NguoiDung findById(int id)
        {
            return db.NguoiDungs.FirstOrDefault(user => user.MaNguoiDung == id);
        }

        // Cập nhật thông tin người dùng
        public bool Update(NguoiDung user)
        {
            // Chuẩn hóa dữ liệu đầu vào
            try
            {
                var existingUser = db.NguoiDungs.Find(user.MaNguoiDung);
                if (existingUser == null)
                    return false;

                var email = user.Email?.Trim().ToLower();
                var name = user.TenNguoiDung?.Trim();
                var role = (user.Quyen ?? "").Trim().ToLower();

                // Kiểm tra các trường bắt buộc
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(name))
                    return false;

                // Kiểm tra định dạng email
                bool duplicateEmail = db.NguoiDungs.Any(u =>
                    u.MaNguoiDung != user.MaNguoiDung &&
                    u.Email.ToLower() == email);

                // Kiểm tra email đã tồn tại cho người dùng khác
                if (duplicateEmail)
                    return false;
                
                // Đảm bảo quyền hợp lệ
                if (role != "admin" && role != "user")
                    role = "user";

                existingUser.TenNguoiDung = name;
                existingUser.Email = email;
                existingUser.Quyen = role;
                existingUser.SoDu = user.SoDu;
                existingUser.IsActive = user.IsActive;

                // Nếu có mật khẩu mới thì cập nhật, nếu không thì giữ nguyên
                if (!string.IsNullOrWhiteSpace(user.MatKhau))
                {
                    existingUser.MatKhau = BCrypt.Net.BCrypt.HashPassword(user.MatKhau);
                }

                return db.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }

        // Kiểm tra xem email đã tồn tại trong hệ thống chưa (dùng cho đăng ký)
        public bool IsEmailExists(string email)
        {
            email = email?.Trim().ToLower();
            return db.NguoiDungs.Any(u => u.Email.ToLower() == email);
        }

        // Kiểm tra xem email đã tồn tại trong hệ thống chưa, ngoại trừ người dùng hiện tại (dùng cho cập nhật thông tin)
        public bool IsEmailExistsForOtherUser(string email, int userId)
        {
            email = email?.Trim().ToLower();
            return db.NguoiDungs.Any(u =>
                u.MaNguoiDung != userId &&
                u.Email.ToLower() == email);
        }
    }
}