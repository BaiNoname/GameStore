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
        private readonly GameStoreContext db;
        private const int pageSize = 10;

        public EventController(
            EventService _eventService,
            GameService _gameService,
            IWebHostEnvironment _env,
            GameStoreContext _db)
        {
            eventService = _eventService;
            gameService = _gameService;
            env = _env;
            db = _db;
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

        private void LoadEffectSelectList(string? selectedEffectCode = null)
        {
            ViewBag.Effects = db.IconEffects
                .Where(x => x.IsActive)
                .OrderBy(x => x.EffectName)
                .Select(x => new SelectListItem
                {
                    Value = x.EffectCode,
                    Text = x.EffectName + " (" + x.Rarity + ")",
                    Selected = x.EffectCode == selectedEffectCode
                })
                .ToList();
        }

        private void LoadFormData(string? selectedGameId = null, string? selectedEffectCode = null)
        {
            LoadGameSelectList(selectedGameId);
            LoadEffectSelectList(selectedEffectCode);
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

        private bool ValidatePrize(Event ev, out string errorMessage)
        {
            errorMessage = "";

            ev.PrizeType = string.IsNullOrWhiteSpace(ev.PrizeType) ? null : ev.PrizeType.Trim();
            ev.PrizeValue = string.IsNullOrWhiteSpace(ev.PrizeValue) ? null : ev.PrizeValue.Trim();
            ev.PrizeCondition = string.IsNullOrWhiteSpace(ev.PrizeCondition) ? null : ev.PrizeCondition.Trim();

            if (string.IsNullOrWhiteSpace(ev.PrizeType))
            {
                ev.PrizeType = null;
                ev.PrizeValue = null;
                ev.PrizeCondition = null;
                return true;
            }

            if (ev.PrizeType != "Balance" && ev.PrizeType != "Effect")
            {
                errorMessage = "❌ Prize Type không hợp lệ!";
                return false;
            }

            ev.PrizeCondition = "CheckIn";

            if (ev.PrizeType == "Balance")
            {
                if (!decimal.TryParse(ev.PrizeValue, out decimal amount) || amount <= 0)
                {
                    errorMessage = "❌ Prize Value của Balance phải là số lớn hơn 0!";
                    return false;
                }
            }
            else if (ev.PrizeType == "Effect")
            {
                if (string.IsNullOrWhiteSpace(ev.PrizeValue))
                {
                    errorMessage = "❌ Vui lòng chọn effect!";
                    return false;
                }

                var effectExists = db.IconEffects.Any(x => x.EffectCode == ev.PrizeValue && x.IsActive);
                if (!effectExists)
                {
                    errorMessage = "❌ Effect không tồn tại hoặc đã bị vô hiệu hóa!";
                    return false;
                }
            }

            return true;
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
        public IActionResult Add(string filterKeyword = "", string filterEventType = "", string filterStatus = "", int currentPage = 1)
        {
            LoadFormData();

            ViewBag.Keyword = filterKeyword;
            ViewBag.EventType = filterEventType;
            ViewBag.Status = filterStatus;
            ViewBag.CurrentPage = currentPage;

            var nowUtc = DateTime.UtcNow;

            return View("~/Views/Admin/Event/Add.cshtml", new Event
            {
                EventType = "Tournament",
                AccessType = "Paid",
                PrizeCondition = "CheckIn",
                StartAt = nowUtc.AddDays(1),
                EndAt = nowUtc.AddDays(1).AddHours(2)
            });
        }

        [HttpPost]
        [Route("event/add")]
        public IActionResult Add(
            Event ev,
            IFormFile? photo,
            string filterKeyword = "",
            string filterEventType = "",
            string filterStatus = "",
            int currentPage = 1)
        {
            ev.Title = ev.Title?.Trim() ?? "";
            ev.Slug = ev.Slug?.Trim().ToLower() ?? "";
            ev.Summary = ev.Summary?.Trim();
            ev.Content = ev.Content?.Trim() ?? "";
            ev.EventType = Request.Form["EventType"].ToString().Trim();
            ev.AccessType = Request.Form["AccessType"].ToString().Trim();
            ev.PrizeInfo = ev.PrizeInfo?.Trim();
            ev.PrizeType = Request.Form["PrizeType"].ToString().Trim();
            ev.PrizeValue = Request.Form["PrizeValue"].ToString().Trim();
            ev.PrizeCondition = "CheckIn";

            LoadFormData(ev.RelatedGameId, ev.PrizeType == "Effect" ? ev.PrizeValue : null);

            ViewBag.Keyword = filterKeyword;
            ViewBag.EventType = filterEventType;
            ViewBag.Status = filterStatus;
            ViewBag.CurrentPage = currentPage;

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

            if (!ValidatePrize(ev, out string prizeError))
            {
                TempData["Msg"] = prizeError;
                TempData["MsgType"] = "danger";
                LoadFormData(ev.RelatedGameId, ev.PrizeType == "Effect" ? ev.PrizeValue : null);
                return View("~/Views/Admin/Event/Add.cshtml", ev);
            }

            if (ev.StartAt.ToUniversalTime() < DateTime.UtcNow)
            {
                TempData["Msg"] = "❌ Start At không được ở quá khứ!";
                TempData["MsgType"] = "danger";
                return View("~/Views/Admin/Event/Add.cshtml", ev);
            }

            if (ev.EndAt.ToUniversalTime() < DateTime.UtcNow)
            {
                TempData["Msg"] = "❌ End At không được ở quá khứ!";
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

            if (photo != null && photo.Length > 0)
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
                return RedirectToAction("Index", new
                {
                    keyword = filterKeyword,
                    eventType = filterEventType,
                    status = filterStatus,
                    page = currentPage
                });
            }

            TempData["Msg"] = "❌ Thêm event thất bại! Thời gian không hợp lệ hoặc dữ liệu chưa đúng.";
            TempData["MsgType"] = "danger";
            return View("~/Views/Admin/Event/Add.cshtml", ev);
        }

        [Route("event/edit/{id}")]
        public IActionResult Edit(int id, string filterKeyword = "", string filterEventType = "", string filterStatus = "", int currentPage = 1)
        {
            var ev = eventService.FindById(id);
            if (ev == null)
            {
                TempData["Msg"] = "❌ Event không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index", new
                {
                    keyword = filterKeyword,
                    eventType = filterEventType,
                    status = filterStatus,
                    page = currentPage
                });
            }

            LoadFormData(ev.RelatedGameId, ev.PrizeType == "Effect" ? ev.PrizeValue : null);

            ViewBag.Keyword = filterKeyword;
            ViewBag.EventType = filterEventType;
            ViewBag.Status = filterStatus;
            ViewBag.CurrentPage = currentPage;

            return View("~/Views/Admin/Event/Edit.cshtml", ev);
        }

        [HttpPost]
        [Route("event/edit/{id}")]
        public IActionResult Edit(
    int id,
    Event ev,
    IFormFile? photo,
    string filterKeyword = "",
    string filterEventType = "",
    string filterStatus = "",
    int currentPage = 1)
        {
            var current = eventService.FindById(id);
            if (current == null)
            {
                TempData["Msg"] = "❌ Event không tồn tại!";
                TempData["MsgType"] = "danger";
                return RedirectToAction("Index", new
                {
                    keyword = filterKeyword,
                    eventType = filterEventType,
                    status = filterStatus,
                    page = currentPage
                });
            }

            ev.EventId = id;
            ev.Title = string.IsNullOrWhiteSpace(ev.Title) ? current.Title : ev.Title.Trim();
            ev.Slug = string.IsNullOrWhiteSpace(ev.Slug) ? current.Slug : ev.Slug.Trim().ToLower();
            ev.Summary = ev.Summary?.Trim();
            ev.Content = string.IsNullOrWhiteSpace(ev.Content) ? current.Content : ev.Content.Trim();
            ev.EventType = string.IsNullOrWhiteSpace(Request.Form["EventType"]) ? current.EventType : Request.Form["EventType"].ToString().Trim();
            ev.AccessType = string.IsNullOrWhiteSpace(Request.Form["AccessType"]) ? current.AccessType : Request.Form["AccessType"].ToString().Trim();
            ev.PrizeInfo = ev.PrizeInfo?.Trim();
            ev.PrizeType = Request.Form["PrizeType"].ToString().Trim();
            ev.PrizeValue = Request.Form["PrizeValue"].ToString().Trim();
            ev.PrizeCondition = "CheckIn";

            LoadFormData(ev.RelatedGameId ?? current.RelatedGameId, ev.PrizeType == "Effect" ? ev.PrizeValue : null);

            ViewBag.Keyword = filterKeyword;
            ViewBag.EventType = filterEventType;
            ViewBag.Status = filterStatus;
            ViewBag.CurrentPage = currentPage;

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

            if (!ValidatePrize(ev, out string prizeError))
            {
                TempData["Msg"] = prizeError;
                TempData["MsgType"] = "danger";
                LoadFormData(ev.RelatedGameId ?? current.RelatedGameId, ev.PrizeType == "Effect" ? ev.PrizeValue : null);
                return View("~/Views/Admin/Event/Edit.cshtml", current);
            }

            // giữ StartAt cũ
            ev.StartAt = current.StartAt;

            // nếu EndAt không đổi thì giữ nguyên, không validate lại
            var postedEndRaw = Request.Form["EndAt"].ToString();
            var currentEndRaw = current.EndAt.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss");

            if (string.IsNullOrWhiteSpace(postedEndRaw) || postedEndRaw == currentEndRaw)
            {
                ev.EndAt = current.EndAt;
            }
            else
            {
                if (!DateTime.TryParse(postedEndRaw, out DateTime parsedEndAt))
                {
                    TempData["Msg"] = "❌ Không được sửa End At về quá khứ!";
                    TempData["MsgType"] = "danger";
                    return View("~/Views/Admin/Event/Edit.cshtml", current);
                }

                ev.EndAt = parsedEndAt;

                if (ev.EndAt.ToUniversalTime() < DateTime.UtcNow)
                {
                    TempData["Msg"] = "❌ Không được sửa End At về quá khứ!";
                    TempData["MsgType"] = "danger";
                    return View("~/Views/Admin/Event/Edit.cshtml", current);
                }

                if (ev.EndAt <= ev.StartAt)
                {
                    TempData["Msg"] = "❌ End At phải lớn hơn Start At!";
                    TempData["MsgType"] = "danger";
                    return View("~/Views/Admin/Event/Edit.cshtml", current);
                }
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
                return RedirectToAction("Index", new
                {
                    keyword = filterKeyword,
                    eventType = filterEventType,
                    status = filterStatus,
                    page = currentPage
                });
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
                TempData["Msg"] = "❌ Không thể xóa event vì event đã có giao dịch hoặc dữ liệu liên quan.";
                TempData["MsgType"] = "danger";
            }

            return RedirectToAction("Index", new { keyword, eventType, status, page });
        }
    }
}