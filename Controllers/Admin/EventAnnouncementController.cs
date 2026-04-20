using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
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

            ViewBag.Event = ev;
            ViewBag.Announcements = announcementService.GetByEvent(eventId);

            return View("~/Views/Admin/EventAnnouncement/Index.cshtml", new EventAnnouncement
            {
                EventId = eventId
            });
        }

        [HttpPost("add")]
        public IActionResult Add(EventAnnouncement model)
        {
            var ev = eventService.FindById(model.EventId);
            if (ev == null)
            {
                TempData["Msg"] = "❌ Event không tồn tại!";
                TempData["MsgType"] = "danger";
                return Redirect("/admin/event/index");
            }

            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return Redirect("/auth/login");

            model.CreatedBy = int.Parse(User.FindFirst("UserId")!.Value);

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

        [HttpGet("delete/{id}")]
        public IActionResult Delete(int id)
        {
            var item = announcementService.FindById(id);
            if (item == null)
            {
                TempData["Msg"] = "❌ Announcement không tồn tại!";
                TempData["MsgType"] = "danger";
                return Redirect("/admin/event/index");
            }

            int eventId = item.EventId;

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