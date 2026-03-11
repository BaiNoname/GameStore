using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Client.Cart
{
    [Route("cart")]
    public class CartController : Controller
    {
        [Route("")]
        public IActionResult Index()
        {
            ViewBag.HideSubBar = true;
            return View();
        }
    }
}
