using Microsoft.AspNetCore.Http;

namespace GameStore.Services;

public interface IMomoService
{
    Task<string> CreatePaymentUrlForOrder(int userId, string maGD, decimal amount, string baseUrl);
    Task<string> CreatePaymentUrlForTopup(int userId, decimal amount, string baseUrl);
    bool VerifyCallback(IQueryCollection query);
}