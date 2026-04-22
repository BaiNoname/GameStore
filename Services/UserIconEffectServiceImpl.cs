using GameStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Services
{
    public class UserIconEffectServiceImpl : UserIconEffectService
    {
        private readonly GameStoreContext db;

        public UserIconEffectServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        public List<UserIconEffect> GetByUser(int userId)
        {
            return db.UserIconEffects
                .Include(x => x.IconEffect)
                .Include(x => x.Event)
                .Where(x => x.MaNguoiDung == userId)
                .OrderByDescending(x => x.GrantedAt)
                .ToList();
        }

        public UserIconEffect? GetEquipped(int userId)
        {
            return db.UserIconEffects
                .Include(x => x.IconEffect)
                .FirstOrDefault(x => x.MaNguoiDung == userId && x.IsEquipped);
        }

        public string? GetEquippedCssClass(int userId)
        {
            return db.UserIconEffects
                .Include(x => x.IconEffect)
                .Where(x => x.MaNguoiDung == userId && x.IsEquipped)
                .Select(x => x.IconEffect != null ? x.IconEffect.CssClass : null)
                .FirstOrDefault();
        }

        public Dictionary<int, string> GetEquippedCssClassMap(List<int> userIds)
        {
            if (userIds == null || !userIds.Any())
                return new Dictionary<int, string>();

            return db.UserIconEffects
                .Include(x => x.IconEffect)
                .Where(x => userIds.Contains(x.MaNguoiDung) && x.IsEquipped && x.IconEffect != null)
                .ToDictionary(
                    x => x.MaNguoiDung,
                    x => x.IconEffect!.CssClass
                );
        }

        public bool Equip(int userId, int userIconEffectId)
        {
            try
            {
                var effects = db.UserIconEffects.Where(x => x.MaNguoiDung == userId).ToList();
                var target = effects.FirstOrDefault(x => x.UserIconEffectId == userIconEffectId);
                if (target == null) return false;

                foreach (var item in effects)
                {
                    item.IsEquipped = false;
                }

                target.IsEquipped = true;

                return db.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }

        public bool Unequip(int userId)
        {
            try
            {
                var effects = db.UserIconEffects.Where(x => x.MaNguoiDung == userId && x.IsEquipped).ToList();

                if (!effects.Any())
                    return true;

                foreach (var item in effects)
                {
                    item.IsEquipped = false;
                }

                return db.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}