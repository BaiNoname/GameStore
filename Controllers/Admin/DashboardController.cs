using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    [Route("admin")]
    public class DashboardController: Controller
    {

        [Route("dashboard")]
        [Route("")]
        public IActionResult Index()
        {
            return View("~/Views/Admin/Dashboard.cshtml");
        }
    }
}
