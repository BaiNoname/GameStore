using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Client.Cart
{
    [Authorize]
    [Route("cart")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
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

        private string GetSafeReturnUrl(string? returnUrl = null)
        {
            var url = returnUrl
                      ?? TempData["ReturnUrl"]?.ToString()
                      ?? "/home/index#game-list-section";

            TempData["ReturnUrl"] = url;
            return url;
        }

        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index(string returnUrl)
        {
            returnUrl = GetSafeReturnUrl(returnUrl);

            // MoMo redirect về /cart với params
            if (Request.Query.ContainsKey("resultCode") && Request.Query.ContainsKey("orderId"))
            {
                var resultCode = Request.Query["resultCode"].ToString();
                var orderId = Request.Query["orderId"].ToString();

                if (resultCode == "0")
                {
                    if (orderId.StartsWith("TOPUP_"))
                    {
                        var parts = orderId.Split('_');
                        var amount = decimal.Parse(Request.Query["amount"].ToString());

                        if (parts.Length >= 2 && int.TryParse(parts[1], out int topupUserId))
                        {
                            await paymentService.CompleteTopup(topupUserId, amount);
                            TempData["ToastMessage"] = $"Nạp tiền thành công! +{amount:N0} VND 💰";
                            TempData["ToastType"] = "success";
                        }
                    }
                    else if (orderId.StartsWith("ORDER_"))
                    {
                        var maGD = orderId["ORDER_".Length..];
                        try { await paymentService.CompleteMomo(maGD); } catch { }
                        TempData["ToastMessage"] = "Thanh toán MoMo thành công 🎉";
                        TempData["ToastType"] = "success";
                    }
                }
                else
                {
                    if (orderId.StartsWith("ORDER_"))
                    {
                        var maGD = orderId["ORDER_".Length..];
                        try { await paymentService.FailMomo(maGD); } catch { }
                    }

                    TempData["ToastMessage"] = "Thanh toán thất bại!";
                    TempData["ToastType"] = "error";
                }

                return RedirectToAction("Index", new { returnUrl });
            }

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
            returnUrl = GetSafeReturnUrl(returnUrl);

            var userId = GetUserId();
            var cart = cartService.GetCart(userId);

            bool alreadyInCart = cart.ChiTietGioHangs.Any(x => x.MaGame == gameId);

            // BUY NOW: nếu đã có trong cart thì vào thẳng giỏ hàng, không báo lỗi
            if (mode == "buy" && alreadyInCart)
            {
                TempData["ReturnUrl"] = returnUrl;
                return RedirectToAction("Index", new { returnUrl });
            }

            var result = cartService.AddToCart(userId, gameId);

            if (result)
            {
                TempData["ToastMessage"] = "Đã thêm vào giỏ hàng 🛒";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = "Không thể thêm game vào giỏ hàng. Có thể game đã có trong giỏ hoặc bạn đã sở hữu game này.";
                TempData["ToastType"] = "error";
            }

            TempData["ReturnUrl"] = returnUrl;

            if (mode == "buy")
                return RedirectToAction("Index", new { returnUrl });

            return Redirect(returnUrl);
        }

        [HttpGet("remove")]
        public IActionResult Remove(string gameId, string returnUrl)
        {
            returnUrl = GetSafeReturnUrl(returnUrl);

            cartService.RemoveFromCart(GetUserId(), gameId);

            TempData["ToastMessage"] = "Đã xóa game khỏi giỏ hàng";
            TempData["ToastType"] = "success";
            TempData["ReturnUrl"] = returnUrl;

            return RedirectToAction("Index", new { returnUrl });
        }

        [HttpGet("clear")]
        public IActionResult Clear(string returnUrl)
        {
            returnUrl = GetSafeReturnUrl(returnUrl);

            cartService.ClearCart(GetUserId());

            TempData["ToastMessage"] = "Đã xóa toàn bộ giỏ hàng 🧹";
            TempData["ToastType"] = "success";
            TempData["ReturnUrl"] = returnUrl;

            return RedirectToAction("Index", new { returnUrl });
        }

        [HttpGet("/checkout")]
        public async Task<IActionResult> Checkout(string method = "balance", string returnUrl = null)
        {
            returnUrl = GetSafeReturnUrl(returnUrl);

            var userId = GetUserId();

            if (method.Equals("momo", StringComparison.OrdinalIgnoreCase))
            {
                var cart = cartService.GetCart(userId);

                if (cart == null || !cart.ChiTietGioHangs.Any())
                {
                    TempData["ToastMessage"] = "Giỏ hàng trống!";
                    TempData["ToastType"] = "error";
                    return RedirectToAction("Index", "Cart", new { returnUrl });
                }

                var total = cart.ChiTietGioHangs.Sum(x => x.DonGiaHienTai);
                var maGD = Guid.NewGuid().ToString();

                paymentService.CreatePendingMomo(userId, maGD, total);

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var paymentUrl = await momoService.CreatePaymentUrlForOrder(userId, maGD, total, baseUrl);

                return Redirect(paymentUrl);
            }

            var result = await paymentService.Checkout(userId);

            TempData["ToastMessage"] = result ? "Thanh toán thành công 💳" : "Thanh toán thất bại!";
            TempData["ToastType"] = result ? "success" : "error";

            return RedirectToAction("Index", "Cart", new { returnUrl });
        }
    }
}