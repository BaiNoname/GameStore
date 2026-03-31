// Thêm dòng này vào đầu file interface
using Microsoft.AspNetCore.Http;

namespace GameStore.Services
{
    public interface VnpayService
    {
        string CreatePaymentUrlForOrder(int userId, decimal amount, string baseUrl);
        string CreatePaymentUrlForTopup(int userId, decimal amount, string baseUrl);
        Task<(bool isSuccess, string maGD, string loaiGD, string message)> HandleCallbackAsync(HttpRequest request);
    }
}