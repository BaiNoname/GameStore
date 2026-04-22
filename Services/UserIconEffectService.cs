using GameStore.Models;

namespace GameStore.Services
{
    public interface UserIconEffectService
    {
        List<UserIconEffect> GetByUser(int userId);
        UserIconEffect? GetEquipped(int userId);
        string? GetEquippedCssClass(int userId);
        Dictionary<int, string> GetEquippedCssClassMap(List<int> userIds);
        bool Equip(int userId, int userIconEffectId);
        bool Unequip(int userId);
    }
}