using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    // Controller quản lý thanh toán, chỉ admin mới có quyền truy cập
    [Authorize(Roles = "admin")]
    [Route("admin/payment")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class PaymentController : Controller
    {
        private PaymentService paymentService;

        public PaymentController(PaymentService _paymentService)
        {
            paymentService = _paymentService;
        }

        // Hiển thị danh sách thanh toán với phân trang và lọc theo từ khóa và trạng thái
        [Route("index")]
        public IActionResult Index(string keyword = "", string status = "", int page = 1)
        {
            int pageSize = 10;
            int totalPages;

            var payments = paymentService.findAll(keyword, status, page, pageSize, out totalPages);

            var vm = new GameStore.Pagination.Admin.PaymentListVM
            {
                Payments = payments,
                CurrentPage = page,
                TotalPages = totalPages,
                Keyword = keyword,
                Status = status
            };

            return View("~/Views/Admin/Payment/Index.cshtml", vm);
        }

        // Cập nhật trạng thái thanh toán, sau đó chuyển hướng về trang danh sách với các tham số hiện tại
        [HttpPost]
        [Route("update-status")]
        public IActionResult UpdateStatus(string id, string status, string keyword = "", string currentStatus = "", int page = 1)
        {
            if (paymentService.UpdateStatus(id, status))
            {
                TempData["Msg"] = "Update status OK";
            }
            else
            {
                TempData["Msg"] = "Update failed";
            }

            return RedirectToAction("Index", new
            {
                keyword,
                status = currentStatus,
                page
            });
        }
    }
}