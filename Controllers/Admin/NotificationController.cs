using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    // Quản lý thông báo chạy ngang (admin tự tạo)
    [Authorize(Roles = "admin")]
    [Route("admin/notification")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class NotificationController : Controller
    {
        private readonly NotificationService notificationService;

        public NotificationController(NotificationService _notificationService)
        {
            notificationService = _notificationService;
        }

        [Route("index")]
        public IActionResult Index(string keyword = "", int page = 1)
        {
            int pageSize = 10;
            int totalPages;
            var data = notificationService.FindAll(keyword, page, pageSize, out totalPages);
            ViewBag.Keyword = keyword;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View("~/Views/Admin/Notification/Index.cshtml", data);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ThongBao tb)
        {
            var (ok, message) = notificationService.Create(tb);
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = ok ? "success" : "error";
            return Redirect("/admin/notification/index");
        }

        [HttpPost("edit")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ThongBao tb)
        {
            var (ok, message) = notificationService.Update(tb);
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = ok ? "success" : "error";
            return Redirect("/admin/notification/index");
        }

        [HttpPost("toggle")]
        [ValidateAntiForgeryToken]
        public IActionResult Toggle(int maTB)
        {
            var (ok, message) = notificationService.ToggleActive(maTB);
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = ok ? "success" : "error";
            return Redirect("/admin/notification/index");
        }

        [HttpPost("delete")]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int maTB)
        {
            var (ok, message) = notificationService.Delete(maTB);
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = ok ? "success" : "error";
            return Redirect("/admin/notification/index");
        }
    }
}
