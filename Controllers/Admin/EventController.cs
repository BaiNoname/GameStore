using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GameStore.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    [Route("admin")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class EventController : Controller
    {
        private readonly EventService eventService;
        private readonly GameService gameService;
        private readonly IWebHostEnvironment env;
        private const int pageSize = 10;

        public EventController(EventService _eventService, GameService _gameService, IWebHostEnvironment _env)
        {
            eventService = _eventService;
            gameService = _gameService;
            env = _env;
        }

        private void LoadGameSelectList(string? selectedGameId = null)
        {
            ViewBag.Games = new SelectList(
                gameService.GetDb().Games.OrderBy(x => x.TenGame).ToList(),
                "MaGame",
                "TenGame",
                selectedGameId
            );
        }

        private string? SaveEventImage(IFormFile? photo)
        {
            if (photo == null || photo.Length == 0)
                return null;

            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            if (!allowed.Contains(ext)) return null;

            var folder = Path.Combine(env.WebRootPath, "images", "events");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid().ToString("N") + ext;
            var path = Path.Combine(folder, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                photo.CopyTo(stream);
            }

            return fileName;
        }

        private void DeleteEventImage(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;

            var path = Path.Combine(env.WebRootPath, "images", "events", fileName);
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        [Route("event/index")]
        public IActionResult Index(string keyword = "", string eventType = "", string status = "", int page = 1)
        {
            int totalPages;
            var events = eventService.FindAll(keyword, eventType, status, page, pageSize, out totalPages);

            ViewBag.Keyword = keyword;
            ViewBag.EventType = eventType;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View("~/Views/Admin/Event/Index.cshtml", events);
        }

        [Route("event/add")]
        public IActionResult Add(string keyword = "", string eventType = "", string status = "", int page = 1)
        {
            LoadGameSelectList();

            ViewBag.Keyword = keyword;
            ViewBag.EventType = eventType;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;

            return View("~/Views/Admin/Event/Add.cshtml", new Event
            {
                EventType = "Tournament",
                AccessType = "Paid",
                Status = "Upcoming",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(1).AddHours(2)
            });
        }

        [HttpPost]
        [Route("event/add")]
        public IActionResult Add(Event ev, IFormFile? photo, string keyword = "", string eventType = "", string status = "", int page = 1)
        {
            LoadGameSelectList(ev.RelatedGameId);

            ViewBag.Keyword = keyword;
            ViewBag.EventType = eventType;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;

            ev.Title = ev.Title?.Trim() ?? "";
            ev.Slug = ev.Slug?.Trim().ToLower() ?? "";
            ev.EventType = Request.Form["EventType"].ToString();
            ev.AccessType = Request.Form["AccessType"].ToString();
            ev.Status = Request.Form["Status"].ToString();

            if (string.IsNullOrWhiteSpace(ev.Title))
            {
                TempData["Msg"] = "❌ Title không được để trống!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/Event/Add.cshtml", ev);
            }

            if (string.IsNullOrWhiteSpace(ev.Slug))
            {
                TempData["Msg"] = "❌ Slug không được để trống!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/Event/Add.cshtml", ev);
            }

            if (string.IsNullOrWhiteSpace(ev.Content))
            {
                TempData["Msg"] = "❌ Content không được để trống!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/Event/Add.cshtml", ev);
            }

            if (ev.EndAt <= ev.StartAt)
            {
                TempData["Msg"] = "❌ End At phải lớn hơn Start At!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/Event/Add.cshtml", ev);
            }

            var existedSlug = eventService.FindAll("", "", "", 1, int.MaxValue, out int _)
                .FirstOrDefault(x => x.Slug == ev.Slug);

            if (existedSlug != null)
            {
                TempData["Msg"] = "❌ Slug đã tồn tại!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/Event/Add.cshtml", ev);
            }

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                ev.CreatedBy = int.Parse(User.FindFirst("UserId")!.Value);
            }

            if (photo != null)
            {
                var saved = SaveEventImage(photo);
                if (saved == null)
                {
                    TempData["Msg"] = "❌ Ảnh không hợp lệ!";
                    TempData["MsgType"] = "danger";
                    return View("~/Views/Admin/Event/Add.cshtml", ev);
                }

                ev.Banner = saved;
            }

            if (eventService.Create(ev))
            {
                TempData["Msg"] = "✅ Thêm event thành công!";
                TempData["MsgType"] = "success";
                return RedirectToAction("Index", new { keyword, eventType, status, page });
            }

            TempData["Msg"] = "❌ Thêm event thất bại!";
            TempData["MsgType"] = "danger";
            return View("~/Views/Admin/Event/Add.cshtml", ev);
        }

        [Route("event/edit/{id}")]
        public IActionResult Edit(int id, string keyword = "", string eventType = "", string status = "", int page = 1)
        {
            var ev = eventService.FindById(id);
            if (ev == null)
            {
                TempData["Msg"] = "❌ Event không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index", new { keyword, eventType, status, page });
            }

            LoadGameSelectList(ev.RelatedGameId);

            ViewBag.Keyword = keyword;
            ViewBag.EventType = eventType;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;

            return View("~/Views/Admin/Event/Edit.cshtml", ev);
        }

        [HttpPost]
        [Route("event/edit/{id}")]
        public IActionResult Edit(int id, Event ev, IFormFile? photo, string keyword = "", string eventType = "", string status = "", int page = 1)
        {
            var current = eventService.FindById(id);
            if (current == null)
            {
                TempData["Msg"] = "❌ Event không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index", new { keyword, eventType, status, page });
            }

            LoadGameSelectList(current.RelatedGameId);

            ViewBag.Keyword = keyword;
            ViewBag.EventType = eventType;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;

            ev.EventId = id;
            ev.Title = string.IsNullOrWhiteSpace(ev.Title) ? current.Title : ev.Title.Trim();
            ev.Slug = string.IsNullOrWhiteSpace(ev.Slug) ? current.Slug : ev.Slug.Trim().ToLower();
            ev.Summary = ev.Summary?.Trim();
            ev.Content = string.IsNullOrWhiteSpace(ev.Content) ? current.Content : ev.Content;
            ev.EventType = string.IsNullOrWhiteSpace(Request.Form["EventType"]) ? current.EventType : Request.Form["EventType"].ToString().Trim();
            ev.AccessType = string.IsNullOrWhiteSpace(Request.Form["AccessType"]) ? current.AccessType : Request.Form["AccessType"].ToString().Trim();
            ev.Status = string.IsNullOrWhiteSpace(Request.Form["Status"]) ? current.Status : Request.Form["Status"].ToString().Trim();

            if (string.IsNullOrWhiteSpace(ev.Title))
            {
                TempData["Msg"] = "❌ Title không được để trống!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/Event/Edit.cshtml", current);
            }

            if (string.IsNullOrWhiteSpace(ev.Slug))
            {
                TempData["Msg"] = "❌ Slug không được để trống!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/Event/Edit.cshtml", current);
            }

            if (string.IsNullOrWhiteSpace(ev.Content))
            {
                TempData["Msg"] = "❌ Content không được để trống!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/Event/Edit.cshtml", current);
            }

            if (ev.EndAt <= ev.StartAt)
            {
                TempData["Msg"] = "❌ End At phải lớn hơn Start At!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/Event/Edit.cshtml", current);
            }

            var duplicateSlug = eventService.FindAll("", "", "", 1, int.MaxValue, out int _)
                .FirstOrDefault(x => x.Slug == ev.Slug && x.EventId != ev.EventId);

            if (duplicateSlug != null)
            {
                TempData["Msg"] = "❌ Slug đã tồn tại!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/Event/Edit.cshtml", current);
            }

            if (photo != null && photo.Length > 0)
            {
                var saved = SaveEventImage(photo);
                if (saved == null)
                {
                    TempData["Msg"] = "❌ Ảnh không hợp lệ!";
                    TempData["MsgType"] = "danger";
                    return View("~/Views/Admin/Event/Edit.cshtml", current);
                }

                DeleteEventImage(current.Banner);
                ev.Banner = saved;
            }
            else
            {
                ev.Banner = current.Banner;
            }

            ev.CreatedBy = current.CreatedBy;
            ev.CreatedAt = current.CreatedAt;
            ev.CurrentParticipants = current.CurrentParticipants;

            if (eventService.Update(ev))
            {
                TempData["Msg"] = "✅ Cập nhật event thành công!";
                TempData["MsgType"] = "success";
                return RedirectToAction("Index", new { keyword, eventType, status, page });
            }

            TempData["Msg"] = "❌ Cập nhật event thất bại!";
            TempData["MsgType"] = "danger";
            return View("~/Views/Admin/Event/Edit.cshtml", current);
        }

        [Route("event/delete/{id}")]
        public IActionResult Delete(int id, string keyword = "", string eventType = "", string status = "", int page = 1)
        {
            var ev = eventService.FindById(id);
            if (ev != null)
                DeleteEventImage(ev.Banner);

            if (eventService.Delete(id))
            {
                TempData["Msg"] = "✅ Xóa event thành công!";
                TempData["MsgType"] = "success";
            }
            else
            {
                TempData["Msg"] = "❌ Xóa event thất bại!";
                TempData["MsgType"] = "danger";
            }

            return RedirectToAction("Index", new { keyword, eventType, status, page });
        }
    }
}