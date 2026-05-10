using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    // Controller quản lý thông báo sự kiện, chỉ admin mới có quyền truy cập
    [Authorize(Roles = "admin")]
    [Route("admin/event-announcement")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class EventAnnouncementController : Controller
    {
        private readonly EventService eventService;
        private readonly EventAnnouncementService announcementService;

        public EventAnnouncementController(
            EventService _eventService,
            EventAnnouncementService _announcementService)
        {
            eventService = _eventService;
            announcementService = _announcementService;
        }

        // Hiển thị danh sách thông báo của một sự kiện cụ thể
        [HttpGet("index/{eventId}")]
        public IActionResult Index(int eventId)
        {
            var ev = eventService.FindById(eventId);
            if (ev == null)
            {
                TempData["Msg"] = "❌ Event không tồn tại!";
                TempData["MsgType"] = "danger";
                return Redirect("/admin/event/index");
            }

            // Truyền thông tin sự kiện và danh sách thông báo vào ViewBag để hiển thị trên view
            ViewBag.Event = ev;
            ViewBag.Announcements = announcementService.GetByEvent(eventId);

            return View("~/Views/Admin/EventAnnouncement/Index.cshtml", new EventAnnouncement
            {
                EventId = eventId
            });
        }

        // Xử lý thêm mới thông báo cho sự kiện
        [HttpPost("add")]
        public IActionResult Add(EventAnnouncement model)
        {
            // Kiểm tra xem sự kiện có tồn tại hay không trước khi thêm thông báo
            var ev = eventService.FindById(model.EventId);

            // Nếu sự kiện không tồn tại, hiển thị thông báo lỗi và chuyển hướng về trang danh sách sự kiện
            if (ev == null)
            {
                TempData["Msg"] = "❌ Event không tồn tại!";
                TempData["MsgType"] = "danger";
                return Redirect("/admin/event/index");
            }

            // Kiểm tra xem người dùng đã đăng nhập hay chưa trước khi thêm thông báo
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return Redirect("/auth/login");

            model.CreatedBy = int.Parse(User.FindFirst("UserId")!.Value);

            // Thêm thông báo mới vào cơ sở dữ liệu thông qua service và hiển thị thông báo thành công hoặc thất bại
            if (announcementService.Create(model))
            {
                TempData["Msg"] = "✅ Thêm announcement thành công!";
                TempData["MsgType"] = "success";
            }
            else
            {
                TempData["Msg"] = "❌ Thêm announcement thất bại!";
                TempData["MsgType"] = "danger";
            }

            return Redirect($"/admin/event-announcement/index/{model.EventId}");
        }

        // Xử lý xóa thông báo của sự kiện
        [HttpGet("delete/{id}")]
        public IActionResult Delete(int id)
        {
            // Kiểm tra xem thông báo có tồn tại hay không trước khi xóa
            var item = announcementService.FindById(id);

            // Nếu thông báo không tồn tại, hiển thị thông báo lỗi và chuyển hướng về trang danh sách sự kiện
            if (item == null)
            {
                TempData["Msg"] = "❌ Announcement không tồn tại!";
                TempData["MsgType"] = "danger";
                return Redirect("/admin/event/index");
            }

            int eventId = item.EventId;

            // Xóa thông báo khỏi cơ sở dữ liệu thông qua service và hiển thị thông báo thành công hoặc thất bại
            if (announcementService.Delete(id))
            {
                TempData["Msg"] = "✅ Xóa announcement thành công!";
                TempData["MsgType"] = "success";
            }
            else
            {
                TempData["Msg"] = "❌ Xóa announcement thất bại!";
                TempData["MsgType"] = "danger";
            }

            return Redirect($"/admin/event-announcement/index/{eventId}");
        }
    }
}