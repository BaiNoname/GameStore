using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Client.Checkout
{
    [Route("checkout")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
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
