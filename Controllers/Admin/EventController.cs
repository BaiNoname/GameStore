using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GameStore.Controllers.Admin
{
    // Controller quản lý sự kiện, chỉ admin mới có quyền truy cập
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

        // Tải danh sách game để hiển thị trong dropdown chọn game liên quan
        private void LoadGameSelectList(string? selectedGameId = null)
        {
            ViewBag.Games = new SelectList(
                gameService.GetDb().Games.OrderBy(x => x.TenGame).ToList(),
                "MaGame",
                "TenGame",
                selectedGameId
            );
        }

        // Tải danh sách effect để hiển thị trong dropdown chọn effect làm phần thưởng
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

        // Tải dữ liệu cần thiết cho form thêm/sửa event, bao gồm danh sách game và effect, đồng thời chọn sẵn giá trị đã chọn nếu có
        private void LoadFormData(string? selectedGameId = null, string? selectedEffectCode = null)
        {
            LoadGameSelectList(selectedGameId);
            LoadEffectSelectList(selectedEffectCode);
        }

        // Lưu ảnh sự kiện lên server và trả về tên file đã lưu, nếu ảnh không hợp lệ thì trả về null
        private string? SaveEventImage(IFormFile? photo)
        {
            if (photo == null || photo.Length == 0)
                return null;

            // chỉ cho phép các định dạng ảnh phổ biến
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

        // Xóa ảnh sự kiện khỏi server nếu tồn tại
        private void DeleteEventImage(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;

            var path = Path.Combine(env.WebRootPath, "images", "events", fileName);
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        // Validate thông tin phần thưởng của event, trả về true nếu hợp lệ, ngược lại trả về false và thông báo lỗi
        private bool ValidatePrize(Event ev, out string errorMessage)
        {
            errorMessage = "";

            // nếu không chọn loại phần thưởng thì coi như không có phần thưởng
            ev.PrizeType = string.IsNullOrWhiteSpace(ev.PrizeType) ? null : ev.PrizeType.Trim();
            // nếu có chọn loại phần thưởng thì giá trị phần thưởng không được để trống
            ev.PrizeValue = string.IsNullOrWhiteSpace(ev.PrizeValue) ? null : ev.PrizeValue.Trim();
            // nếu có chọn loại phần thưởng thì điều kiện nhận thưởng mặc định là "CheckIn"
            ev.PrizeCondition = string.IsNullOrWhiteSpace(ev.PrizeCondition) ? null : ev.PrizeCondition.Trim();

            // nếu không chọn loại phần thưởng thì bỏ qua các trường liên quan đến phần thưởng
            if (string.IsNullOrWhiteSpace(ev.PrizeType))
            {
                ev.PrizeType = null;
                ev.PrizeValue = null;
                ev.PrizeCondition = null;
                return true;
            }

            // chỉ cho phép 2 loại phần thưởng là Balance (tiền trong game) hoặc Effect (hiệu ứng icon)
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

        // Danh sách event với phân trang và bộ lọc
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

        // Form thêm event mới
        [Route("event/add")]
        public IActionResult Add(string filterKeyword = "", string filterEventType = "", string filterStatus = "", int currentPage = 1)
        {
            LoadFormData();

            ViewBag.Keyword = filterKeyword;
            ViewBag.EventType = filterEventType;
            ViewBag.Status = filterStatus;
            ViewBag.CurrentPage = currentPage;

            var nowUtc = DateTime.UtcNow;

            // mặc định thời gian bắt đầu là ngày mai và kết thúc sau đó 2 tiếng để tránh lỗi thời gian ở quá khứ khi admin tạo event mới
            return View("~/Views/Admin/Event/Add.cshtml", new Event
            {
                EventType = "Tournament",
                AccessType = "Paid",
                PrizeCondition = "CheckIn",
                StartAt = nowUtc.AddDays(1),
                EndAt = nowUtc.AddDays(1).AddHours(2)
            });
        }

        // Xử lý form thêm event mới
        [HttpPost]
        [Route("event/add")]
        public IActionResult Add(

            // bind các trường của event từ form, đồng thời lấy file ảnh nếu có và các giá trị filter để giữ nguyên khi quay lại trang index
            Event ev,
            IFormFile? photo,
            string filterKeyword = "",
            string filterEventType = "",
            string filterStatus = "",
            int currentPage = 1)
        {
            // trim các trường chuỗi và chuẩn hóa slug về chữ thường để tránh lỗi khi lưu vào database
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

            // nếu có chọn loại phần thưởng là Effect thì truyền giá trị đã chọn để load lại form, nếu không thì truyền null
            LoadFormData(ev.RelatedGameId, ev.PrizeType == "Effect" ? ev.PrizeValue : null);

            ViewBag.Keyword = filterKeyword;
            ViewBag.EventType = filterEventType;
            ViewBag.Status = filterStatus;
            ViewBag.CurrentPage = currentPage;

            // validate các trường bắt buộc và hợp lệ, nếu có lỗi thì trả về form với thông báo lỗi
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

            // nếu có ảnh được tải lên thì lưu ảnh và gán tên file vào event, nếu ảnh không hợp lệ thì trả về form với thông báo lỗi
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

            // khi tạo event mới thì số lượng người tham gia hiện tại là 0
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

        // Form sửa event
        [Route("event/edit/{id}")]
        public IActionResult Edit(int id, string filterKeyword = "", string filterEventType = "", string filterStatus = "", int currentPage = 1)
        {
            // tìm event theo ID, nếu không tồn tại thì trả về trang index với thông báo lỗi
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

            // load dữ liệu cho form, nếu event có phần thưởng là Effect thì truyền giá trị effect đã chọn để load lại form, nếu không thì truyền null
            LoadFormData(ev.RelatedGameId, ev.PrizeType == "Effect" ? ev.PrizeValue : null);

            ViewBag.Keyword = filterKeyword;
            ViewBag.EventType = filterEventType;
            ViewBag.Status = filterStatus;
            ViewBag.CurrentPage = currentPage;

            return View("~/Views/Admin/Event/Edit.cshtml", ev);
        }

        // Xử lý form sửa event
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
            // tìm event theo ID, nếu không tồn tại thì trả về trang index với thông báo lỗi
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

            // trim các trường chuỗi và chuẩn hóa slug về chữ thường để tránh lỗi khi lưu vào database
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

            // nếu có chọn loại phần thưởng là Effect thì truyền giá trị đã chọn để load lại form, nếu không thì truyền null
            LoadFormData(ev.RelatedGameId ?? current.RelatedGameId, ev.PrizeType == "Effect" ? ev.PrizeValue : null);

            ViewBag.Keyword = filterKeyword;
            ViewBag.EventType = filterEventType;
            ViewBag.Status = filterStatus;
            ViewBag.CurrentPage = currentPage;

            // validate các trường bắt buộc và hợp lệ, nếu có lỗi thì trả về form với thông báo lỗi
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

            // kiểm tra slug có bị trùng với event khác hay không, nếu có thì trả về form với thông báo lỗi
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

        // Xóa event, nếu event đã có giao dịch hoặc dữ liệu liên quan thì không cho xóa và trả về thông báo lỗi
        [Route("event/delete/{id}")]
        public IActionResult Delete(int id, string keyword = "", string eventType = "", string status = "", int page = 1)
        {
            var ev = eventService.FindById(id);

            // nếu event không tồn tại thì trả về trang index với thông báo lỗi
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