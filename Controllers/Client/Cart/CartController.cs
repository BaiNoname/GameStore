using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
        private readonly GameStoreContext db;

        public CartController(
            CartService _cartService,
            PaymentService _paymentService,
            IMomoService _momoService,
            GameStoreContext _db)
        {
            cartService = _cartService;
            paymentService = _paymentService;
            momoService = _momoService;
            db = _db;
        }

        private int GetUserId() => int.Parse(User.FindFirst("UserId")!.Value);

        private async Task<NguoiDung?> GetCurrentActiveUserAsync()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return null;

            var claim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrWhiteSpace(claim) || !int.TryParse(claim, out int userId))
                return null;

            var user = db.NguoiDungs.FirstOrDefault(x => x.MaNguoiDung == userId && x.IsActive);
            if (user == null)
            {
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return null;
            }

            return user;
        }

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
            var activeUser = await GetCurrentActiveUserAsync();
            if (activeUser == null)
                return Redirect("/auth/login");

            returnUrl = GetSafeReturnUrl(returnUrl);

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

            var cart = cartService.GetCart(activeUser.MaNguoiDung);
            if (cart == null)
            {
                TempData["ToastMessage"] = "Không thể tải giỏ hàng.";
                TempData["ToastType"] = "error";
                return Redirect("/auth/login");
            }

            ViewBag.HideSubBar = true;
            ViewBag.ReturnUrl = returnUrl;

            TempData["ReturnUrl"] = returnUrl;
            return View(cart);
        }

        [HttpGet("add")]
        public async Task<IActionResult> Add(string gameId, string returnUrl, string mode)
        {
            var activeUser = await GetCurrentActiveUserAsync();
            if (activeUser == null)
                return Redirect("/auth/login");

            returnUrl = GetSafeReturnUrl(returnUrl);

            var cart = cartService.GetCart(activeUser.MaNguoiDung);
            if (cart == null)
            {
                TempData["ToastMessage"] = "Không thể tải giỏ hàng.";
                TempData["ToastType"] = "error";
                return Redirect("/auth/login");
            }

            bool alreadyInCart = cart.ChiTietGioHangs.Any(x => x.MaGame == gameId);

            if (mode == "buy" && alreadyInCart)
            {
                TempData["ReturnUrl"] = returnUrl;
                return RedirectToAction("Index", new { returnUrl });
            }

            var result = cartService.AddToCart(activeUser.MaNguoiDung, gameId);

            if (result)
            {
                TempData["ToastMessage"] = "Đã thêm vào giỏ hàng 🛒";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = "Không thể thêm game vào giỏ hàng. Có thể game đã có trong giỏ, bạn đã sở hữu game này, hoặc tài khoản không còn hợp lệ.";
                TempData["ToastType"] = "error";
            }

            TempData["ReturnUrl"] = returnUrl;

            if (mode == "buy")
                return RedirectToAction("Index", new { returnUrl });

            return Redirect(returnUrl);
        }

        [HttpGet("remove")]
        public async Task<IActionResult> Remove(string gameId, string returnUrl)
        {
            var activeUser = await GetCurrentActiveUserAsync();
            if (activeUser == null)
                return Redirect("/auth/login");

            returnUrl = GetSafeReturnUrl(returnUrl);

            cartService.RemoveFromCart(activeUser.MaNguoiDung, gameId);

            TempData["ToastMessage"] = "Đã xóa game khỏi giỏ hàng";
            TempData["ToastType"] = "success";
            TempData["ReturnUrl"] = returnUrl;

            return RedirectToAction("Index", new { returnUrl });
        }

        [HttpGet("clear")]
        public async Task<IActionResult> Clear(string returnUrl)
        {
            var activeUser = await GetCurrentActiveUserAsync();
            if (activeUser == null)
                return Redirect("/auth/login");

            returnUrl = GetSafeReturnUrl(returnUrl);

            cartService.ClearCart(activeUser.MaNguoiDung);

            TempData["ToastMessage"] = "Đã xóa toàn bộ giỏ hàng 🧹";
            TempData["ToastType"] = "success";
            TempData["ReturnUrl"] = returnUrl;

            return RedirectToAction("Index", new { returnUrl });
        }

        [HttpGet("/checkout")]
        public async Task<IActionResult> Checkout(string method = "balance", string returnUrl = null)
        {
            var activeUser = await GetCurrentActiveUserAsync();
            if (activeUser == null)
                return Redirect("/auth/login");

            returnUrl = GetSafeReturnUrl(returnUrl);

            var userId = activeUser.MaNguoiDung;

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