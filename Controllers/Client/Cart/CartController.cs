using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Client.Cart
{
    [Authorize]
    [Route("cart")]
    public class CartController : Controller
    {
        private readonly CartService cartService;
        private readonly PaymentService paymentService;
        private readonly IMomoService momoService;

        public CartController(CartService _cartService, PaymentService _paymentService, IMomoService _momoService)
        {
            cartService = _cartService;
            paymentService = _paymentService;
            momoService = _momoService;
        }

        private int GetUserId() => int.Parse(User.FindFirst("UserId").Value);

        [HttpGet("")]
        [HttpGet("index")]
        public IActionResult Index(string returnUrl)
        {
            // 🔥 HANDLE MOMO RESULT
            if (Request.Query["success"] == "momo")
            {
                TempData["ToastMessage"] = "Thanh toán MoMo thành công 🎉";
                TempData["ToastType"] = "success";
            }

            if (Request.Query["error"] == "momo")
            {
                TempData["ToastMessage"] = "Thanh toán MoMo thất bại!";
                TempData["ToastType"] = "error";
            }

            var cart = cartService.GetCart(GetUserId());

            ViewBag.HideSubBar = true;
            ViewBag.ReturnUrl = returnUrl;
            TempData["ReturnUrl"] = returnUrl;

            return View(cart);
        }

        [HttpGet("add")]
        public IActionResult Add(string gameId, string returnUrl, string mode)
        {
            var result = cartService.AddToCart(GetUserId(), gameId);
            TempData["ToastMessage"] = result ? "Đã thêm vào giỏ hàng 🛒" : "Game đã có trong giỏ hàng!";
            TempData["ToastType"] = result ? "success" : "error";
            TempData["ReturnUrl"] = returnUrl;
            if (mode == "buy") return RedirectToAction("Index", new { returnUrl });
            return Redirect(returnUrl ?? "/");
        }

        [HttpGet("remove")]
        public IActionResult Remove(string gameId, string returnUrl)
        {
            cartService.RemoveFromCart(GetUserId(), gameId);
            TempData["ToastMessage"] = "Đã xóa game khỏi giỏ hàng";
            TempData["ToastType"] = "success";
            TempData["ReturnUrl"] = returnUrl;
            return RedirectToAction("Index", new { returnUrl });
        }

        [HttpGet("clear")]
        public IActionResult Clear()
        {
            cartService.ClearCart(GetUserId());
            TempData["ToastMessage"] = "Đã xóa toàn bộ giỏ hàng 🧹";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        [HttpGet("/checkout")]
        public async Task<IActionResult> Checkout(string method = "balance")
        {
            var userId = GetUserId();

            if (method.Equals("momo", StringComparison.OrdinalIgnoreCase))
            {
                var cart = cartService.GetCart(userId);

                if (cart == null || !cart.ChiTietGioHangs.Any())
                {
                    TempData["ToastMessage"] = "Giỏ hàng trống!";
                    TempData["ToastType"] = "error";
                    return Redirect("/cart");
                }

                var total = cart.ChiTietGioHangs.Sum(x => x.DonGiaHienTai);
                var maGD = Guid.NewGuid().ToString();

                paymentService.CreatePendingMomo(userId, maGD, total);

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var paymentUrl = await momoService.CreatePaymentUrlForOrder(userId, maGD, total, baseUrl);

                return Redirect(paymentUrl);
            }

            // balance
            var result = await paymentService.Checkout(userId);
            TempData["ToastMessage"] = result ? "Thanh toán thành công 💳" : "Thanh toán thất bại!";
            TempData["ToastType"] = result ? "success" : "error";
            return Redirect("/cart");
        }
    }
}