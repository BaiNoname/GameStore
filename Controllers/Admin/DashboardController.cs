using GameStore.Models;
using GameStore.Pagination.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    [Route("admin")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
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

            var nowUtc = DateTime.UtcNow;
            var todayUtcStart = nowUtc.Date;
            var tomorrowUtcStart = todayUtcStart.AddDays(1);

            var start7DaysUtc = todayUtcStart.AddDays(-6);

            var monthUtcStart = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var nextMonthUtcStart = monthUtcStart.AddMonths(1);

            var successQuery = db.GiaoDiches.Where(x => x.TrangThai == "Success");

            // Tổng quan chính
            vm.TotalRevenue = successQuery.Sum(x => (decimal?)x.ThanhTien) ?? 0;
            vm.TotalOrders = db.GiaoDiches.Count();
            vm.TotalUsers = db.NguoiDungs.Count();
            vm.TotalGames = db.Games.Count();

            // Chỉ số phụ
            vm.SuccessOrders = successQuery.Count();
            vm.PendingOrders = db.GiaoDiches.Count(x => x.TrangThai == "Pending");

            vm.RevenueToday = successQuery
                .Where(x => x.NgayMua >= todayUtcStart && x.NgayMua < tomorrowUtcStart)
                .Sum(x => (decimal?)x.ThanhTien) ?? 0;

            vm.RevenueThisMonth = successQuery
                .Where(x => x.NgayMua >= monthUtcStart && x.NgayMua < nextMonthUtcStart)
                .Sum(x => (decimal?)x.ThanhTien) ?? 0;

            vm.AvgOrderValue = vm.SuccessOrders > 0
                ? successQuery.Average(x => x.ThanhTien)
                : 0;

            // Revenue 7 ngày gần nhất, fill 0 cho ngày không có giao dịch
            var revenueRaw = successQuery
                .Where(x => x.NgayMua >= start7DaysUtc && x.NgayMua < tomorrowUtcStart)
                .AsEnumerable()
                .GroupBy(x => x.NgayMua.ToLocalTime().Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.ThanhTien)
                })
                .ToDictionary(x => x.Date, x => x.Revenue);

            var localToday = DateTime.Now.Date;
            var localStart7Days = localToday.AddDays(-6);

            vm.RevenueChart = Enumerable.Range(0, 7)
                .Select(i =>
                {
                    var date = localStart7Days.AddDays(i);
                    return new RevenueByDay
                    {
                        Date = date.ToString("dd/MM"),
                        Revenue = revenueRaw.ContainsKey(date) ? revenueRaw[date] : 0
                    };
                })
                .ToList();

            // Top game
            vm.TopGames = db.Games
                .OrderByDescending(x => x.SoLuotTai)
                .Take(5)
                .Select(x => new TopGame
                {
                    TenGame = x.TenGame,
                    SoLuotTai = x.SoLuotTai
                })
                .ToList();

            // Recent orders
            vm.RecentOrders = db.GiaoDiches
                .Include(x => x.NguoiDung)
                .OrderByDescending(x => x.NgayMua)
                .Take(8)
                .Select(x => new RecentOrder
                {
                    MaGD = x.MaGD,
                    Email = x.NguoiDung.Email,
                    ThanhTien = x.ThanhTien,
                    TrangThai = x.TrangThai,
                    NgayMua = x.NgayMua
                })
                .ToList();

            // Event stats
            vm.TotalEvents = db.Events.Count();
            vm.UpcomingEvents = db.Events.Count(x => x.Status == "Upcoming");
            vm.LiveEvents = db.Events.Count(x => x.Status == "Live");
            vm.EndedEvents = db.Events.Count(x => x.Status == "Ended");

            vm.RecentEvents = db.Events
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new EventSummaryVM
                {
                    EventId = x.EventId,
                    Title = x.Title,
                    Status = x.Status,
                    EventType = x.EventType,
                    CurrentParticipants = x.CurrentParticipants,
                    MaxParticipants = x.MaxParticipants,
                    StartAt = x.StartAt,
                    EndAt = x.EndAt
                })
                .ToList();

            vm.TopJoinedEvents = db.Events
                .OrderByDescending(x => x.CurrentParticipants)
                .ThenByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new EventSummaryVM
                {
                    EventId = x.EventId,
                    Title = x.Title,
                    Status = x.Status,
                    EventType = x.EventType,
                    CurrentParticipants = x.CurrentParticipants,
                    MaxParticipants = x.MaxParticipants,
                    StartAt = x.StartAt,
                    EndAt = x.EndAt
                })
                .ToList();

            return View("~/Views/Admin/Dashboard.cshtml", vm);
        }
    }
}