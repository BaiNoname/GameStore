using Microsoft.AspNetCore.Http;

namespace GameStore.Services;

public interface IMomoService
{
    // Tạo URL thanh toán cho đơn hàng
    Task<string> CreatePaymentUrlForOrder(int userId, string maGD, decimal amount, string baseUrl);
    // Tạo URL thanh toán cho nạp tiền
    Task<string> CreatePaymentUrlForTopup(int userId, decimal amount, string baseUrl);
    // Xác thực callback từ MoMo
    bool VerifyCallback(IQueryCollection query);
}