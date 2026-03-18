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

        public CartController(CartService _cartService, PaymentService _paymentService)
        {
            cartService = _cartService;
            paymentService = _paymentService;
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

        [HttpGet("/checkout")]
        public IActionResult Checkout()
        {
            var result = paymentService.Checkout(GetUserId());

            TempData["Msg"] = result
                ? "Thanh toán thành công!"
                : "Thanh toán thất bại!";

            return Redirect("/cart");
        }
    }
}