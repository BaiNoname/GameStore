using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Client.Checkout
{
    [Route("checkout")]
    public class CheckoutController : Controller
    {
        [Route("")]
        public IActionResult Index()
        {
            ViewBag.HideSubBar = true;
            return View();
        }
    }
}
