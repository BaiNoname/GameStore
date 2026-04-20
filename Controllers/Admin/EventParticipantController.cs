using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Admin
{
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

        [HttpGet("delete/{participantId}")]
        public IActionResult Delete(int participantId)
        {
            var participant = participantService.FindById(participantId);
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