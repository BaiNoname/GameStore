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

        // Kiểm tra xem người dùng có còn hoạt động hay không
        private bool IsUserActive(int userId)
        {
            return db.NguoiDungs.Any(x => x.MaNguoiDung == userId && x.IsActive);
        }

        // Lấy tất cả icon effect của người dùng
        public List<UserIconEffect> GetByUser(int userId)
        {
            // Nếu người dùng không còn hoạt động, trả về danh sách rỗng
            if (!IsUserActive(userId))
                return new List<UserIconEffect>();

            // Lấy tất cả icon effect của người dùng, bao gồm thông tin về IconEffect và Event, sắp xếp theo thời gian được cấp (GrantedAt) giảm dần
            return db.UserIconEffects
                .Include(x => x.IconEffect)
                .Include(x => x.Event)
                .Where(x => x.MaNguoiDung == userId)
                .OrderByDescending(x => x.GrantedAt)
                .ToList();
        }

        // Lấy icon effect đang được trang bị của người dùng
        public UserIconEffect? GetEquipped(int userId)
        {
            // Nếu người dùng không còn hoạt động, trả về null
            if (!IsUserActive(userId))
                return null;

            // Lấy icon effect đang được trang bị của người dùng, bao gồm thông tin về IconEffect
            return db.UserIconEffects
                .Include(x => x.IconEffect)
                .FirstOrDefault(x => x.MaNguoiDung == userId && x.IsEquipped);
        }

        // Lấy CSS class của icon effect đang được trang bị của người dùng
        public string? GetEquippedCssClass(int userId)
        {
            // Nếu người dùng không còn hoạt động, trả về null
            if (!IsUserActive(userId))
                return null;
            
            // Lấy CSS class của icon effect đang được trang bị của người dùng
            return db.UserIconEffects
                .Include(x => x.IconEffect)
                .Where(x => x.MaNguoiDung == userId && x.IsEquipped)
                .Select(x => x.IconEffect != null ? x.IconEffect.CssClass : null)
                .FirstOrDefault();
        }

        // Lấy map giữa userId và CSS class của icon effect đang được trang bị của họ cho một danh sách userId
        public Dictionary<int, string> GetEquippedCssClassMap(List<int> userIds)
        {
            // Nếu danh sách userId rỗng hoặc null, trả về dictionary rỗng
            if (userIds == null || !userIds.Any())
                return new Dictionary<int, string>();

            // Lấy map giữa userId và CSS class của icon effect đang được trang bị của họ cho một danh sách userId, chỉ bao gồm những người dùng còn hoạt động
            return db.UserIconEffects
                .Include(x => x.IconEffect)
                .Where(x =>
                    userIds.Contains(x.MaNguoiDung) &&
                    x.IsEquipped &&
                    x.IconEffect != null &&
                    x.NguoiDung != null &&
                    x.NguoiDung.IsActive)
                .ToDictionary(
                    x => x.MaNguoiDung,
                    x => x.IconEffect!.CssClass
                );
        }

        // Trang bị một icon effect cho người dùng, đảm bảo rằng chỉ có một icon effect được trang bị tại một thời điểm
        public bool Equip(int userId, int userIconEffectId)
        {
            // Nếu người dùng không còn hoạt động, trả về false
            if (!IsUserActive(userId))
                return false;
            try
            {
                // Lấy tất cả icon effect của người dùng
                var effects = db.UserIconEffects.Where(x => x.MaNguoiDung == userId).ToList();
                // Tìm icon effect cần trang bị trong số các icon effect của người dùng
                var target = effects.FirstOrDefault(x => x.UserIconEffectId == userIconEffectId);
                // Nếu không tìm thấy icon effect cần trang bị, trả về false
                if (target == null) return false;

                // Bỏ trang bị tất cả icon effect khác của người dùng
                foreach (var item in effects)
                {
                    item.IsEquipped = false;
                }

                // Trang bị icon effect mục tiêu
                target.IsEquipped = true;

                return db.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }

        // Bỏ trang bị tất cả icon effect của người dùng
        public bool Unequip(int userId)
        {
            if (!IsUserActive(userId))
                return false;
            try
            {
                // Lấy tất cả icon effect đang được trang bị của người dùng
                var effects = db.UserIconEffects.Where(x => x.MaNguoiDung == userId && x.IsEquipped).ToList();

                // Nếu không có icon effect nào đang được trang bị, trả về true
                if (!effects.Any())
                    return true;

                // Bỏ trang bị tất cả icon effect đang được trang bị của người dùng
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