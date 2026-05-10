using GameStore.Models;

namespace GameStore.Services
{
    public interface UserIconEffectService
    {
        // Lấy danh sách hiệu ứng biểu tượng người dùng theo ID người dùng
        List<UserIconEffect> GetByUser(int userId);
        //  Lấy hiệu ứng biểu tượng người dùng đang được trang bị theo ID người dùng
        UserIconEffect? GetEquipped(int userId);
        // Lấy lớp CSS của hiệu ứng biểu tượng người dùng đang được trang bị
        string? GetEquippedCssClass(int userId);
        // Lấy bản đồ lớp CSS của hiệu ứng biểu tượng người dùng đang được trang bị cho nhiều người dùng
        Dictionary<int, string> GetEquippedCssClassMap(List<int> userIds);
        // Trang bị hiệu ứng biểu tượng người dùng
        bool Equip(int userId, int userIconEffectId);
        // Gỡ bỏ hiệu ứng biểu tượng người dùng
        bool Unequip(int userId);
    }
}