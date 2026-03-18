using GameStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Services
{
    public class CartServiceImpl : CartService
    {
        private GameStoreContext db;

        public CartServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        // 🔥 LẤY CART CHUẨN (luôn đúng 1 cart)
        private GioHang GetOrCreateCart(int userId)
        {
            var cart = db.GioHangs
                .Include(g => g.ChiTietGioHangs)
                .ThenInclude(c => c.Game)
                .Where(g => g.MaNguoiDung == userId)
                .OrderByDescending(g => g.MaGH)
                .FirstOrDefault();

            if (cart == null)
            {
                cart = new GioHang
                {
                    MaGH = Guid.NewGuid().ToString(),
                    MaNguoiDung = userId
                };

                db.GioHangs.Add(cart);
                db.SaveChanges();
            }

            return cart;
        }

        // 🛒 GET CART
        public GioHang GetCart(int userId)
        {
            return GetOrCreateCart(userId);
        }

        // ➕ ADD TO CART
        public bool AddToCart(int userId, string gameId)
        {
            var cart = GetOrCreateCart(userId);

            // ❌ đã mua rồi thì không cho add
            bool bought = db.ChiTietGiaoDiches
                .Include(x => x.GiaoDich)
                .Any(x => x.GiaoDich.MaNguoiDung == userId && x.MaGame == gameId);

            if (bought) return false;

            // ❌ đã có trong cart
            bool exists = cart.ChiTietGioHangs
                .Any(x => x.MaGame == gameId);

            if (exists) return false;

            var game = db.Games.Find(gameId);
            if (game == null) return false;

            db.ChiTietGioHangs.Add(new ChiTietGioHang
            {
                MaGH = cart.MaGH,
                MaGame = gameId,
                DonGiaHienTai = game.Gia
            });

            return db.SaveChanges() > 0;
        }

        // ❌ REMOVE
        public bool RemoveFromCart(int userId, string gameId)
        {
            var cart = GetOrCreateCart(userId);

            var item = db.ChiTietGioHangs
                .FirstOrDefault(x => x.MaGH == cart.MaGH && x.MaGame == gameId);

            if (item == null) return false;

            db.ChiTietGioHangs.Remove(item);
            return db.SaveChanges() > 0;
        }

        // 🧹 CLEAR
        public bool ClearCart(int userId)
        {
            var cart = GetOrCreateCart(userId);

            if (!cart.ChiTietGioHangs.Any())
                return false;

            db.ChiTietGioHangs.RemoveRange(cart.ChiTietGioHangs);
            return db.SaveChanges() > 0;
        }
    }
}