using GameStore.Models;
using GameStore.Pagination.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    [Route("admin")]
    public class DashboardController : Controller
    {
        private readonly GameStoreContext db;

        public DashboardController(GameStoreContext _db)
        {
            db = _db;
        }

        [Route("dashboard")]
        [Route("")]
        public IActionResult Index()
        {
            var vm = new DashboardVM();

            // 🔥 tổng doanh thu (chỉ lấy success)
            vm.TotalRevenue = db.GiaoDiches
                .Where(x => x.TrangThai == "Success")
                .Sum(x => (decimal?)x.ThanhTien) ?? 0;

            // 🔥 tổng đơn
            vm.TotalOrders = db.GiaoDiches.Count();

            // 🔥 users
            vm.TotalUsers = db.NguoiDungs.Count();

            // 🔥 games
            vm.TotalGames = db.Games.Count();

            // 📈 doanh thu 7 ngày
            vm.RevenueChart = db.GiaoDiches
                .Where(x => x.TrangThai == "Success")
                .GroupBy(x => x.NgayMua.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.ThanhTien)
                })
                .OrderBy(x => x.Date)
                .Take(7)
                .ToList() 
                .Select(x => new RevenueByDay
                {
                    Date = x.Date.ToString("dd/MM"),
                    Revenue = x.Revenue
                })
                .ToList();

            // 🔥 top game
            vm.TopGames = db.Games
                .OrderByDescending(x => x.SoLuotTai)
                .Take(5)
                .Select(x => new TopGame
                {
                    TenGame = x.TenGame,
                    SoLuotTai = x.SoLuotTai
                })
                .ToList();

            // 🧾 recent orders
            vm.RecentOrders = db.GiaoDiches
                .Include(x => x.NguoiDung)
                .OrderByDescending(x => x.NgayMua)
                .Take(5)
                .Select(x => new RecentOrder
                {
                    MaGD = x.MaGD,
                    Email = x.NguoiDung.Email,
                    ThanhTien = x.ThanhTien,
                    TrangThai = x.TrangThai
                })
                .ToList();

            return View("~/Views/Admin/Dashboard.cshtml", vm);
        }
    }
}