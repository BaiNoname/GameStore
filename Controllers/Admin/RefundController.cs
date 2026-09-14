using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    // Controller quản lý yêu cầu hoàn trả game, chỉ admin mới có quyền truy cập
    [Authorize(Roles = "admin")]
    [Route("admin/refund")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class RefundController : Controller
    {
        private readonly RefundService refundService;

        public RefundController(RefundService _refundService)
        {
            refundService = _refundService;
        }

        // Danh sách yêu cầu hoàn trả (lọc theo trạng thái, phân trang)
        [Route("index")]
        public IActionResult Index(string status = "", int page = 1)
        {
            int pageSize = 10;
            int totalPages;

            var data = refundService.FindAll(status, page, pageSize, out totalPages);

            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View("~/Views/Admin/Refund/Index.cshtml", data);
        }

        // Duyệt yêu cầu hoàn trả -> hoàn tiền + trừ game khỏi thư viện
        [HttpPost]
        [Route("approve")]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(int id, string status = "", int page = 1)
        {
            var (ok, message) = refundService.Approve(id);
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = ok ? "success" : "error";

            return RedirectToAction("Index", new { status, page });
        }

        // Từ chối yêu cầu hoàn trả (kèm ghi chú)
        [HttpPost]
        [Route("reject")]
        [ValidateAntiForgeryToken]
        public IActionResult Reject(int id, string ghiChu = "", string status = "", int page = 1)
        {
            var (ok, message) = refundService.Reject(id, ghiChu);
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = ok ? "success" : "error";

            return RedirectToAction("Index", new { status, page });
        }
    }
}
