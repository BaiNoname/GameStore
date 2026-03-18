using GameStore.Models;

namespace GameStore.Services
{
    public interface CartService
    {
        GioHang GetCart(int userId);

        bool AddToCart(int userId, string gameId);

        bool RemoveFromCart(int userId, string gameId);

        bool ClearCart(int userId);
    }
}