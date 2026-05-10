using GameStore.Models;

namespace GameStore.Services
{
    public interface CartService
    {
        // Lấy giỏ hàng hiện tại của user
        GioHang GetCart(int userId);

        // Thêm game vào giỏ hàng
        bool AddToCart(int userId, string gameId);

        // Xóa game khỏi giỏ hàng
        bool RemoveFromCart(int userId, string gameId);

        // Xóa tất cả game khỏi giỏ hàng
        bool ClearCart(int userId);
    }
}