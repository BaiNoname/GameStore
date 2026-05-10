using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
    // Controller để quản lý người tham gia sự kiện, chỉ admin mới có quyền truy cập
    [Authorize(Roles = "admin")]
    [Route("admin/event-participant")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class EventParticipantController : Controller
    {
        private readonly EventService eventService;
        private readonly EventParticipantService participantService;

        public EventParticipantController(
            EventService _eventService,
            EventParticipantService _participantService)
        {
            eventService = _eventService;
            participantService = _participantService;
        }

        // Hiển thị danh sách người tham gia của một sự kiện cụ thể
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
            ViewBag.Participants = participantService.GetParticipantsByEvent(eventId);

            return View("~/Views/Admin/EventParticipant/Index.cshtml");
        }

        // Xóa một người tham gia khỏi sự kiện
        [HttpGet("delete/{participantId}")]
        public IActionResult Delete(int participantId)
        {
            // Tìm participant theo ID
            var participant = participantService.FindById(participantId);
            // Kiểm tra nếu participant tồn tại
            if (participant == null)
            {
                TempData["Msg"] = "❌ Participant không tồn tại!";
                TempData["MsgType"] = "danger";
                return Redirect("/admin/event/index");
            }

            int eventId = participant.EventId;

            if (participantService.RemoveParticipant(participantId))
            {
                TempData["Msg"] = "✅ Xóa participant thành công!";
                TempData["MsgType"] = "success";
            }
            else
            {
                TempData["Msg"] = "❌ Xóa participant thất bại!";
                TempData["MsgType"] = "danger";
            }

            return Redirect($"/admin/event-participant/index/{eventId}");
        }
    }
}