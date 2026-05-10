using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Client.Cart
{
    // Controller xử lý các hành động liên quan đến giỏ hàng của người dùng
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

        // Lấy ID người dùng từ claim trong token
        private int GetUserId() => int.Parse(User.FindFirst("UserId")!.Value);

        // Lấy thông tin người dùng hiện tại và kiểm tra xem tài khoản còn hoạt động hay không
        private async Task<NguoiDung?> GetCurrentActiveUserAsync()
        {
            // Nếu người dùng chưa đăng nhập, trả về null
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return null;

            // Lấy claim chứa UserId và kiểm tra tính hợp lệ
            var claim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrWhiteSpace(claim) || !int.TryParse(claim, out int userId))
                return null;

            // Truy vấn cơ sở dữ liệu để lấy thông tin người dùng và kiểm tra xem tài khoản còn hoạt động hay không
            var user = db.NguoiDungs.FirstOrDefault(x => x.MaNguoiDung == userId && x.IsActive);

            // Nếu không tìm thấy người dùng hoặc tài khoản đã bị vô hiệu hóa, đăng xuất và trả về null
            if (user == null)
            {
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return null;
            }

            return user;
        }

        // Đảm bảo returnUrl an toàn 
        private string GetSafeReturnUrl(string? returnUrl = null)
        {
            var url = returnUrl
                      ?? TempData["ReturnUrl"]?.ToString()
                      ?? "/home/index#game-list-section";

            TempData["ReturnUrl"] = url;
            return url;
        }

        // Hiển thị trang giỏ hàng, xử lý kết quả thanh toán từ MoMo và hiển thị thông báo tương ứng
        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index(string returnUrl)
        {
            // Lấy thông tin người dùng hiện tại và kiểm tra xem tài khoản còn hoạt động hay không
            var activeUser = await GetCurrentActiveUserAsync();
            if (activeUser == null)
                return Redirect("/auth/login");

            // Đảm bảo returnUrl an toàn trước khi sử dụng
            returnUrl = GetSafeReturnUrl(returnUrl);

            // Xử lý kết quả thanh toán từ MoMo nếu có
            if (Request.Query.ContainsKey("resultCode") && Request.Query.ContainsKey("orderId"))
            {
                var resultCode = Request.Query["resultCode"].ToString();
                var orderId = Request.Query["orderId"].ToString();

                // Nếu thanh toán thành công
                if (resultCode == "0")
                {
                    // Xử lý hoàn tất giao dịch dựa trên orderId để cập nhật trạng thái đơn hàng hoặc nạp tiền vào tài khoản
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
                    // Nếu orderId bắt đầu bằng "ORDER_", xử lý hoàn tất đơn hàng và cập nhật trạng thái giao dịch
                    else if (orderId.StartsWith("ORDER_"))
                    {
                        var maGD = orderId["ORDER_".Length..];
                        try { await paymentService.CompleteMomo(maGD); } catch { }
                        TempData["ToastMessage"] = "Thanh toán MoMo thành công 🎉";
                        TempData["ToastType"] = "success";
                    }
                }
                // Nếu thanh toán thất bại
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

            // Lấy thông tin giỏ hàng của người dùng hiện tại
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

        // Thêm game vào giỏ hàng, kiểm tra nếu đã có trong giỏ hoặc đã sở hữu, và hiển thị thông báo tương ứng
        [HttpGet("add")]
        public async Task<IActionResult> Add(string gameId, string returnUrl, string mode)
        {
            // Lấy thông tin người dùng hiện tại và kiểm tra xem tài khoản còn hoạt động hay không
            var activeUser = await GetCurrentActiveUserAsync();
            if (activeUser == null)
                return Redirect("/auth/login");

            returnUrl = GetSafeReturnUrl(returnUrl);

            // Lấy thông tin giỏ hàng của người dùng hiện tại
            var cart = cartService.GetCart(activeUser.MaNguoiDung);
            if (cart == null)
            {
                TempData["ToastMessage"] = "Không thể tải giỏ hàng.";
                TempData["ToastType"] = "error";
                return Redirect("/auth/login");
            }

            // Kiểm tra nếu game đã có trong giỏ hàng
            bool alreadyInCart = cart.ChiTietGioHangs.Any(x => x.MaGame == gameId);

            // Nếu đang ở chế độ "mua ngay" và game đã có trong giỏ hàng, chuyển hướng về trang giỏ hàng thay vì thêm lại
            if (mode == "buy" && alreadyInCart)
            {
                TempData["ReturnUrl"] = returnUrl;
                return RedirectToAction("Index", new { returnUrl });
            }

            // Thêm game vào giỏ hàng, nếu không thể thêm (do đã có trong giỏ, đã sở hữu, hoặc tài khoản không hợp lệ) thì hiển thị thông báo lỗi
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

        // Xóa game khỏi giỏ hàng và hiển thị thông báo tương ứng
        [HttpGet("remove")]
        public async Task<IActionResult> Remove(string gameId, string returnUrl)
        {
            // Lấy thông tin người dùng hiện tại và kiểm tra xem tài khoản còn hoạt động hay không
            var activeUser = await GetCurrentActiveUserAsync();
            // Nếu người dùng chưa đăng nhập hoặc tài khoản không còn hợp lệ, chuyển hướng về trang đăng nhập
            if (activeUser == null)
                return Redirect("/auth/login");

            // Đảm bảo returnUrl an toàn trước khi sử dụng
            returnUrl = GetSafeReturnUrl(returnUrl);

            // Xóa game khỏi giỏ hàng
            cartService.RemoveFromCart(activeUser.MaNguoiDung, gameId);

            TempData["ToastMessage"] = "Đã xóa game khỏi giỏ hàng";
            TempData["ToastType"] = "success";
            TempData["ReturnUrl"] = returnUrl;

            return RedirectToAction("Index", new { returnUrl });
        }

        // Xóa toàn bộ giỏ hàng và hiển thị thông báo tương ứng
        [HttpGet("clear")]
        public async Task<IActionResult> Clear(string returnUrl)
        {
            // Lấy thông tin người dùng hiện tại và kiểm tra xem tài khoản còn hoạt động hay không
            var activeUser = await GetCurrentActiveUserAsync();
            if (activeUser == null)
                return Redirect("/auth/login");

            // Đảm bảo returnUrl an toàn trước khi sử dụng
            returnUrl = GetSafeReturnUrl(returnUrl);

            // Xóa toàn bộ giỏ hàng
            cartService.ClearCart(activeUser.MaNguoiDung);

            TempData["ToastMessage"] = "Đã xóa toàn bộ giỏ hàng 🧹";
            TempData["ToastType"] = "success";
            TempData["ReturnUrl"] = returnUrl;

            return RedirectToAction("Index", new { returnUrl });
        }

        // Xử lý thanh toán, hỗ trợ cả phương thức thanh toán bằng số dư tài khoản và MoMo, và hiển thị thông báo tương ứng
        [HttpGet("/checkout")]
        public async Task<IActionResult> Checkout(string method = "balance", string returnUrl = null)
        {
            // Lấy thông tin người dùng hiện tại và kiểm tra xem tài khoản còn hoạt động hay không
            var activeUser = await GetCurrentActiveUserAsync();
            if (activeUser == null)
                return Redirect("/auth/login");

            // Đảm bảo returnUrl an toàn trước khi sử dụng
            returnUrl = GetSafeReturnUrl(returnUrl);

            // Xử lý thanh toán dựa trên phương thức được chọn (mặc định là "balance")
            var userId = activeUser.MaNguoiDung;

            // Nếu phương thức thanh toán là "momo", tạo giao dịch pending và chuyển hướng người dùng đến trang thanh toán của MoMo
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

            // Nếu phương thức thanh toán là "balance", thực hiện thanh toán trực tiếp bằng số dư tài khoản và hiển thị thông báo tương ứng
            var result = await paymentService.Checkout(userId);

            TempData["ToastMessage"] = result ? "Thanh toán thành công 💳" : "Thanh toán thất bại!";
            TempData["ToastType"] = result ? "success" : "error";

            return RedirectToAction("Index", "Cart", new { returnUrl });
        }
    }
}