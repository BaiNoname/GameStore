using GameStore.Models;

namespace GameStore.Services
{
    public interface UserService
    {
        List<NguoiDung> findAll(string keyword, string status, int page, int pageSize, out int totalPages);
        NguoiDung findById(int id);
        bool Create(NguoiDung user);
        bool Update(NguoiDung user);
        bool Delete(int id, int currentUserId);
        bool IsEmailExists(string email);
        bool IsEmailExistsForOtherUser(string email, int userId);
        bool Activate(int id);
    }
}