using GameStore.Models;

namespace GameStore.Services
{
    public interface UserService
    {
        // Lấy tất cả người dùng
        List<NguoiDung> findAll(string keyword, string status, int page, int pageSize, out int totalPages);
        // Lấy người dùng theo ID
        NguoiDung findById(int id);
        // Tạo người dùng mới
        bool Create(NguoiDung user);
        // Cập nhật thông tin người dùng
        bool Update(NguoiDung user);
        // Xóa người dùng
        bool Delete(int id, int currentUserId);
        // Kiểm tra email đã tồn tại
        bool IsEmailExists(string email);
        // Kiểm tra email đã tồn tại cho người dùng khác
        bool IsEmailExistsForOtherUser(string email, int userId);
        // Kích hoạt người dùng
        bool Activate(int id);
    }
}