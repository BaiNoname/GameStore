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
        public IActionResult Index()
        {
            var cart = cartService.GetCart(GetUserId());
            ViewBag.HideSubBar = true;
            return View(cart);
        }

        [HttpGet("add")]
        public IActionResult Add(string gameId, string returnUrl)
        {
            var result = cartService.AddToCart(GetUserId(), gameId);

            if (!result)
                TempData["Msg"] = "Game đã có trong giỏ hàng!";

            return Redirect(returnUrl ?? "/");
        }

        [HttpGet("remove")]
        public IActionResult Remove(string gameId)
        {
            cartService.RemoveFromCart(GetUserId(), gameId);
            return RedirectToAction("Index");
        }

        [HttpGet("clear")]
        public IActionResult Clear()
        {
            cartService.ClearCart(GetUserId());
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
                    TempData["Msg"] = "Giỏ hàng trống!";
                    return Redirect("/cart");
                }

                var total = cart.ChiTietGioHangs.Sum(x => x.DonGiaHienTai);

                // Pass null so VnpayService uses configured CallbackUrl from appsettings
                var paymentUrl = vnpayService.CreatePaymentUrlForOrder(GetUserId(), total, null);
                return Redirect(paymentUrl);
            }
            else
            {
                var result = await paymentService.Checkout(GetUserId());

                TempData["Msg"] = result
                    ? "Thanh toán bằng số dư thành công!"
                    : "Thanh toán thất bại!";

                return Redirect("/cart");
            }
        }
    }
}