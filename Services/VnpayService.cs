namespace GameStore.Services
{
    public interface VnpayService
    {
        string CreatePaymentUrlForOrder(int userId, decimal amount, string returnUrl);
        /// Tạo URL nạp tiền vào balance (phương thức = "Topup").
        string CreateTopupUrl(int userId, decimal amount, string returnUrl);
        
        
    }
}
