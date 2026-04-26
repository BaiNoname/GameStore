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

        private bool IsUserActive(int userId)
        {
            return db.NguoiDungs.Any(x => x.MaNguoiDung == userId && x.IsActive);
        }

        private GioHang? GetOrCreateCart(int userId)
        {
            if (!IsUserActive(userId))
                return null;

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

                cart = db.GioHangs
                    .Include(g => g.ChiTietGioHangs)
                    .ThenInclude(c => c.Game)
                    .Where(g => g.MaNguoiDung == userId)
                    .OrderByDescending(g => g.MaGH)
                    .FirstOrDefault();
            }

            return cart;
        }

        public GioHang GetCart(int userId)
        {
            return GetOrCreateCart(userId)!;
        }

        public bool AddToCart(int userId, string gameId)
        {
            if (!IsUserActive(userId))
                return false;

            try
            {
                if (string.IsNullOrWhiteSpace(gameId))
                    return false;

                gameId = gameId.Trim();

                var cart = GetOrCreateCart(userId);
                if (cart == null)
                    return false;

                var game = db.Games.Find(gameId);
                if (game == null)
                    return false;

                bool ownedInLibrary = db.ThuVienGames
                    .Any(x => x.MaNguoiDung == userId && x.MaGame == gameId);

                if (ownedInLibrary)
                    return false;

                bool exists = cart.ChiTietGioHangs
                    .Any(x => x.MaGame == gameId);

                if (exists)
                    return false;

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

        public bool RemoveFromCart(int userId, string gameId)
        {
            if (!IsUserActive(userId))
                return false;

            var cart = GetOrCreateCart(userId);
            if (cart == null)
                return false;

            var item = db.ChiTietGioHangs
                .FirstOrDefault(x => x.MaGH == cart.MaGH && x.MaGame == gameId);

            if (item == null)
                return false;

            db.ChiTietGioHangs.Remove(item);
            return db.SaveChanges() > 0;
        }

        public bool ClearCart(int userId)
        {
            if (!IsUserActive(userId))
                return false;

            var cart = GetOrCreateCart(userId);
            if (cart == null)
                return false;

            if (!cart.ChiTietGioHangs.Any())
                return false;

            db.ChiTietGioHangs.RemoveRange(cart.ChiTietGioHangs);
            return db.SaveChanges() > 0;
        }
    }
}