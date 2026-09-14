using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    // Quản lý mã khuyến mãi (admin)
    [Authorize(Roles = "admin")]
    [Route("admin/coupon")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class CouponController : Controller
    {
        private readonly CouponService couponService;

        public CouponController(CouponService _couponService)
        {
            couponService = _couponService;
        }

        [Route("index")]
        public IActionResult Index(string keyword = "", int page = 1)
        {
            int pageSize = 10;
            int totalPages;
            var data = couponService.FindAll(keyword, page, pageSize, out totalPages);

            ViewBag.Keyword = keyword;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View("~/Views/Admin/Coupon/Index.cshtml", data);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(KhuyenMai km)
        {
            var (ok, message) = couponService.Create(km);
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = ok ? "success" : "error";
            return RedirectToAction("Index");
        }

        [HttpPost("edit")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(KhuyenMai km)
        {
            var (ok, message) = couponService.Update(km);
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = ok ? "success" : "error";
            return RedirectToAction("Index");
        }

        [HttpPost("toggle")]
        [ValidateAntiForgeryToken]
        public IActionResult Toggle(int maKM)
        {
            var (ok, message) = couponService.ToggleActive(maKM);
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = ok ? "success" : "error";
            return RedirectToAction("Index");
        }

        [HttpPost("delete")]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int maKM)
        {
            var (ok, message) = couponService.Delete(maKM);
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = ok ? "success" : "error";
            return RedirectToAction("Index");
        }
    }
}
