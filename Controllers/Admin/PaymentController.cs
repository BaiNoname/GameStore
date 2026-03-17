using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    [Route("admin/payment")]
    public class PaymentController: Controller
    {
        private PaymentService paymentService;

        public PaymentController(PaymentService _paymentService)
        {
            paymentService = _paymentService;
        }

        // 🔹 LIST
        [Route("index")]
        public IActionResult Index(string keyword = "", string status = "", int page = 1)
        {
            int pageSize = 10;
            int totalPages;

            var payments = paymentService.findAll(keyword, status, page, pageSize, out totalPages);

            var vm = new GameStore.ViewModels.PaymentListVM
            {
                Payments = payments,
                CurrentPage = page,
                TotalPages = totalPages,
                Keyword = keyword,
                Status = status
            };

            return View("~/Views/Admin/Payment/Index.cshtml", vm);
        }

        //// 🔹 DETAIL
        //[Route("detail/{id}")]
        //public IActionResult Detail(string id)
        //{
        //    var gd = paymentService.findById(id);

        //    if (gd == null)
        //        return RedirectToAction("Index");

        //    return View("~/Views/Admin/Payment/Detail.cshtml", gd);
        //}

        // 🔹 UPDATE STATUS
        [HttpPost]
        [Route("update-status")]
        public IActionResult UpdateStatus(string id, string status)
        {
            if (paymentService.UpdateStatus(id, status))
            {
                TempData["Msg"] = "Update status OK";
            }
            else
            {
                TempData["Msg"] = "Update failed";
            }

            return RedirectToAction("Index");
        }
    }
}
