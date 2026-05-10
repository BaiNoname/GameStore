using GameStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Services
{
    public class CartServiceImpl : CartService
    {
        private readonly GameStoreContext db;

        public CartServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        // Kiểm tra user còn hoạt động hay không
        private bool IsUserActive(int userId)
        {
            return db.NguoiDungs.Any(x => x.MaNguoiDung == userId && x.IsActive);
        }
        
        // Lấy giỏ hàng hiện tại của user hoặc tạo mới nếu chưa có
        private GioHang? GetOrCreateCart(int userId)
        {
            if (!IsUserActive(userId))
                return null;

            // Lấy giỏ hàng của user, sắp xếp theo MaGH giảm dần để lấy giỏ hàng mới nhất
            var cart = db.GioHangs
                .Include(g => g.ChiTietGioHangs)
                .ThenInclude(c => c.Game)
                .Where(g => g.MaNguoiDung == userId)
                .OrderByDescending(g => g.MaGH)
                .FirstOrDefault();

            // Nếu chưa có giỏ hàng nào thì tạo mới
            if (cart == null)
            {
                cart = new GioHang
                {
                    MaGH = Guid.NewGuid().ToString(),
                    MaNguoiDung = userId
                };

                db.GioHangs.Add(cart);
                db.SaveChanges();

                // Lấy lại giỏ hàng mới tạo để có đầy đủ thông tin chi tiết
                cart = db.GioHangs
                    .Include(g => g.ChiTietGioHangs)
                    .ThenInclude(c => c.Game)
                    .Where(g => g.MaNguoiDung == userId)
                    .OrderByDescending(g => g.MaGH)
                    .FirstOrDefault();
            }

            return cart;
        }

        // Lấy giỏ hàng của user
        public GioHang GetCart(int userId)
        {
            return GetOrCreateCart(userId)!;
        }
        
        // Thêm game vào giỏ hàng
        public bool AddToCart(int userId, string gameId)
        {
            if (!IsUserActive(userId))
                return false;

            try
            {
                if (string.IsNullOrWhiteSpace(gameId))
                    return false;

                gameId = gameId.Trim();

                // Lấy giỏ hàng của user
                var cart = GetOrCreateCart(userId);
                if (cart == null)
                    return false;

                // Kiểm tra game có tồn tại hay không
                var game = db.Games.Find(gameId);
                if (game == null)
                    return false;

                // Kiểm tra game đã có trong thư viện của user hay chưa
                bool ownedInLibrary = db.ThuVienGames
                    .Any(x => x.MaNguoiDung == userId && x.MaGame == gameId);

                // Nếu đã có trong thư viện thì không thể thêm vào giỏ hàng
                if (ownedInLibrary)
                    return false;

                // Kiểm tra game đã có trong giỏ hàng hay chưa
                bool exists = cart.ChiTietGioHangs
                    .Any(x => x.MaGame == gameId);

                if (exists)
                    return false;

                // Thêm chi tiết giỏ hàng mới
                db.ChiTietGioHangs.Add(new ChiTietGioHang
                {
                    MaGH = cart.MaGH,
                    MaGame = gameId,
                    DonGiaHienTai = game.Gia
                });

                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ADD TO CART ERROR: " + ex.Message);
                return false;
            }
        }
        
        // Xóa game khỏi giỏ hàng
        public bool RemoveFromCart(int userId, string gameId)
        {
            if (!IsUserActive(userId))
                return false;

            // Lấy giỏ hàng của user
            var cart = GetOrCreateCart(userId);
            if (cart == null)
                return false;

            // Tìm chi tiết giỏ hàng của game đó
            var item = db.ChiTietGioHangs
                .FirstOrDefault(x => x.MaGH == cart.MaGH && x.MaGame == gameId);

            if (item == null)
                return false;

            db.ChiTietGioHangs.Remove(item);
            return db.SaveChanges() > 0;
        }
        
        // Xóa tất cả game khỏi giỏ hàng
        public bool ClearCart(int userId)
        {
            if (!IsUserActive(userId))
                return false;

            // Lấy giỏ hàng của user
            var cart = GetOrCreateCart(userId);
            if (cart == null)
                return false;

            // Nếu giỏ hàng trống thì không cần xóa
            if (!cart.ChiTietGioHangs.Any())
                return false;

            // Xóa tất cả chi tiết giỏ hàng của giỏ hàng đó
            db.ChiTietGioHangs.RemoveRange(cart.ChiTietGioHangs);
            return db.SaveChanges() > 0;
        }
    }
}