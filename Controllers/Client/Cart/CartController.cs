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
        private readonly VnpayService vnpayService;

        public CartController(CartService _cartService, PaymentService _paymentService, VnpayService _vnpayService)
        {
            cartService = _cartService;
            paymentService = _paymentService;
            vnpayService = _vnpayService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst("UserId").Value);
        }

        [HttpGet("")]
        [HttpGet("index")]
        public IActionResult Index(string returnUrl)
        {
            var cart = cartService.GetCart(GetUserId());
            ViewBag.HideSubBar = true;

            // 🔥 giữ lại để View dùng
            ViewBag.ReturnUrl = returnUrl;

            // 🔥 backup cho các request tiếp theo (remove, clear…)
            TempData["ReturnUrl"] = returnUrl;

            return View(cart);
        }

        [HttpGet("add")]
        public IActionResult Add(string gameId, string returnUrl, string mode)
        {
            var result = cartService.AddToCart(GetUserId(), gameId);

            if (result)
            {
                TempData["ToastMessage"] = "Đã thêm vào giỏ hàng 🛒";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = "Game đã có trong giỏ hàng!";
                TempData["ToastType"] = "error";
            }

            TempData["ReturnUrl"] = returnUrl;

            if (mode == "buy")
            {
                return RedirectToAction("Index", new { returnUrl = returnUrl });
            }

            return Redirect(returnUrl ?? "/");
        }

        [HttpGet("remove")]
        public IActionResult Remove(string gameId, string returnUrl)
        {
            cartService.RemoveFromCart(GetUserId(), gameId);

            TempData["ToastMessage"] = "Đã xóa game khỏi giỏ hàng";
            TempData["ToastType"] = "success";

            // 🔥 giữ lại returnUrl
            TempData["ReturnUrl"] = returnUrl;

            return RedirectToAction("Index", new { returnUrl = returnUrl });
        }

        [HttpGet("clear")]
        public IActionResult Clear()
        {
            cartService.ClearCart(GetUserId());

            TempData["ToastMessage"] = "Đã xóa toàn bộ giỏ hàng 🧹";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index");
        }

        // method = "balance" -> use existing balance checkout
        // method = "vnpay"   -> redirect to VNPAY payment url for the order
        [HttpGet("/checkout")]
        public async Task<IActionResult> Checkout(string method = "balance")
        {
            if (method.Equals("vnpay", StringComparison.OrdinalIgnoreCase))
            {
                var cart = cartService.GetCart(GetUserId());
                if (cart == null || !cart.ChiTietGioHangs.Any())
                {
                    TempData["ToastMessage"] = "Giỏ hàng trống!";
                    TempData["ToastType"] = "error";

                    return Redirect("/cart");
                }

                var total = cart.ChiTietGioHangs.Sum(x => x.DonGiaHienTai);

                var paymentUrl = vnpayService.CreatePaymentUrlForOrder(GetUserId(), total, null);
                return Redirect(paymentUrl);
            }
            else
            {
                var result = await paymentService.Checkout(GetUserId());

                TempData["ToastMessage"] = result
                    ? "Thanh toán thành công 💳"
                    : "Thanh toán thất bại!";

                TempData["ToastType"] = result ? "success" : "error";

                return Redirect("/cart");
            }
        }
    }
}