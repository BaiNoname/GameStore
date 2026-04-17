using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Client
{

    /// Xử lý tất cả callback và redirect từ VNPay
    /// Route: /api/vnpay/...

    [Route("api/vnpay")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class VnpayController : Controller
    {
        private readonly VnpayService _vnpayService;
        private readonly ILogger<VnpayController> _logger;

        public VnpayController(VnpayService vnpayService, ILogger<VnpayController> logger)
        {
            _vnpayService = vnpayService;
            _logger = logger;
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback()
        {
            var (isSuccess, maGD, loaiGD, message) = await _vnpayService.HandleCallbackAsync(Request);

            _logger.LogInformation("VNPay callback: isSuccess={isSuccess}, loaiGD={loaiGD}, maGD={maGD}, msg={msg}",
                isSuccess, loaiGD, maGD, message);

            if (isSuccess)
            {
                TempData["ToastMessage"] = loaiGD == "Topup"
                    ? "Nạp tiền thành công 💰"
                    : "Thanh toán VNPay thành công 🎮";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = loaiGD == "Topup"
                    ? $"Nạp tiền thất bại: {message}"
                    : $"Thanh toán thất bại: {message}";
                TempData["ToastType"] = "error";
            }

            return Redirect("/cart");
        }

        // ═══════════════════════════════════════════════════════════════
        // TOP-UP — User bấm "Nạp VNPay" từ form trong giỏ hàng
        // URL: /api/vnpay/topup?amount=50000
        // ═══════════════════════════════════════════════════════════════
        [Authorize]
        [HttpGet("topup")]
        public IActionResult Topup(decimal amount)
        {
            if (amount < 1000)
            {
                TempData["ToastMessage"] = "Số tiền nạp tối thiểu là 1.000 VND";
                TempData["ToastType"] = "error";
                return Redirect("/cart");
            }

            var userId = int.Parse(User.FindFirst("UserId")!.Value);

            var paymentUrl = _vnpayService.CreatePaymentUrlForTopup(
                userId,
                amount,
                $"{Request.Scheme}://{Request.Host}"
            );

            return Redirect(paymentUrl);
        }
    }
}