using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    [Authorize(Roles = "admin")]
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
