using GameStore.Pagination.User;
using GameStore.Services;
using Microsoft.AspNetCore.Mvc;
using GameStore.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GameStore.Controllers
{
    [Route("event")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class EventController : Controller
    {
        private readonly EventService eventService;
        private readonly EventParticipantService participantService;
        private readonly EventAnnouncementService announcementService;
        private readonly EventMessageService messageService;
        private readonly PaymentService paymentService;
        private readonly IHubContext<EventChatHub> eventChatHub;

        public EventController(
            EventService _eventService,
            EventParticipantService _participantService,
            EventAnnouncementService _announcementService,
            EventMessageService _messageService,
            PaymentService _paymentService,
            IHubContext<EventChatHub> _eventChatHub)
        {
            eventService = _eventService;
            participantService = _participantService;
            announcementService = _announcementService;
            messageService = _messageService;
            paymentService = _paymentService;
            eventChatHub = _eventChatHub;
        }

        private IActionResult RedirectToLoginWithReturnUrl(string returnUrl)
        {
            return Redirect($"/auth/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        [Route("")]
        [Route("index")]
        public IActionResult Index(string eventType = "All", string status = "All", int page = 1)
        {
            ViewBag.HideSubBar = true;
            int pageSize = 6;
            int totalPages;

            var featured = eventService.GetFeatured(1);
            var events = eventService.FindPublic(eventType, status, page, pageSize, out totalPages);
            var live = eventService.GetLive(3);
            var upcoming = eventService.GetUpcoming(6);

            var vm = new EventPageVM
            {
                FeaturedEvents = featured,
                Events = events,
                LiveEvents = live,
                UpcomingEvents = upcoming,
                EventType = string.IsNullOrWhiteSpace(eventType) ? "All" : eventType,
                Status = string.IsNullOrWhiteSpace(status) ? "All" : status,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View("~/Views/Event/Index.cshtml", vm);
        }

        [Route("detail/{slug}")]
        public IActionResult Detail(string slug)
        {
            ViewBag.HideSubBar = true;
            if (string.IsNullOrWhiteSpace(slug))
                return RedirectToAction("Index");

            var ev = eventService.FindBySlug(slug);
            if (ev == null)
            {
                TempData["ToastMessage"] = "Sự kiện không tồn tại";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }

            bool joined = false;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                int userId = int.Parse(User.FindFirst("UserId")!.Value);
                joined = participantService.IsJoined(ev.EventId, userId);
            }

            ViewBag.IsJoined = joined;
            ViewBag.Announcements = announcementService.GetByEvent(ev.EventId);

            return View("~/Views/Event/Detail.cshtml", ev);
        }

        [HttpPost]
        [Route("join/{id}")]
        public async Task<IActionResult> Join(int id, string method = "balance")
        {
            var ev = eventService.FindById(id);
            if (ev == null)
            {
                TempData["ToastMessage"] = "Sự kiện không tồn tại";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }

            if ((ev.Status ?? "").Trim().Equals("Ended", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ToastMessage"] = "Sự kiện đã kết thúc, không thể tham gia mới.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Detail", new { slug = ev.Slug });
            }

            if (ev.MaxParticipants.HasValue && ev.CurrentParticipants >= ev.MaxParticipants.Value)
            {
                TempData["ToastMessage"] = "Sự kiện đã đủ số lượng người tham gia";
                TempData["ToastType"] = "error";
                return RedirectToAction("Detail", new { slug = ev.Slug });
            }

            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                var returnUrl = Url.Action("Detail", "Event", new { slug = ev.Slug }) ?? "/event";
                return RedirectToLoginWithReturnUrl(returnUrl);
            }

            int userId = int.Parse(User.FindFirst("UserId")!.Value);


            if (participantService.IsJoined(id, userId))
            {
                TempData["ToastMessage"] = "Bạn đã tham gia sự kiện này rồi";
                TempData["ToastType"] = "success";
                return RedirectToAction("Room", new { id });
            }

            if ((ev.AccessType ?? "").Trim().ToLower() == "free")
            {
                if (participantService.JoinFree(id, userId))
                {
                    TempData["ToastMessage"] = "Tham gia sự kiện thành công";
                    TempData["ToastType"] = "success";
                    return RedirectToAction("Room", new { id });
                }

                TempData["ToastMessage"] = "Không thể tham gia sự kiện";
                TempData["ToastType"] = "error";
                return RedirectToAction("Detail", new { slug = ev.Slug });
            }

            var maGD = paymentService.CreatePendingEventBalance(userId, id);
            var result = await paymentService.CompleteEventBalance(maGD);

            TempData["ToastMessage"] = result
                ? "Mua quyền tham gia sự kiện thành công"
                : "Thanh toán bằng số dư thất bại";
            TempData["ToastType"] = result ? "success" : "error";

            return result
                ? RedirectToAction("Room", new { id })
                : RedirectToAction("Detail", new { slug = ev.Slug });
        }

        [Route("room/{id}")]
        public IActionResult Room(int id)
        {
            ViewBag.HideSubBar = true;

            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                var returnUrl = Url.Action("Room", "Event", new { id }) ?? "/event";
                return RedirectToLoginWithReturnUrl(returnUrl);
            }

            int userId = int.Parse(User.FindFirst("UserId")!.Value);

            var ev = eventService.FindById(id);
            if (ev == null)
                return RedirectToAction("Index");

            if (!participantService.IsJoined(id, userId))
            {
                TempData["ToastMessage"] = "Bạn cần tham gia sự kiện trước";
                TempData["ToastType"] = "error";
                return RedirectToAction("Detail", new { slug = ev.Slug });
            }

            ViewBag.Participants = participantService.GetParticipantsByEvent(id);
            ViewBag.Announcements = announcementService.GetByEvent(id);
            ViewBag.Messages = messageService.GetByEvent(id, 100);
            ViewBag.MyParticipant = participantService.FindParticipant(id, userId);
            ViewBag.IsArchivedRoom = (ev.Status ?? "").Trim().Equals("Ended", StringComparison.OrdinalIgnoreCase);

            return View("~/Views/Event/Room.cshtml", ev);
        }

        [HttpPost]
        [Route("room/{id}/send-message")]
        public async Task<IActionResult> SendMessage(int id, string content)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                var returnUrl = Url.Action("Room", "Event", new { id }) ?? "/event";
                return RedirectToLoginWithReturnUrl(returnUrl);
            }

            int userId = int.Parse(User.FindFirst("UserId")!.Value);

            var ev = eventService.FindById(id);
            if (ev == null)
                return RedirectToAction("Index");

            if ((ev.Status ?? "").Trim().Equals("Ended", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ToastMessage"] = "Sự kiện đã kết thúc, chat đã bị khóa.";
                TempData["ToastType"] = "error";
                return BadRequest();
            }

            if (!participantService.IsJoined(id, userId))
            {
                TempData["ToastMessage"] = "Bạn không có quyền gửi tin nhắn trong sự kiện này";
                TempData["ToastType"] = "error";
                return RedirectToAction("Detail", new { slug = ev.Slug });
            }

            content = content?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction("Room", new { id });

            if (messageService.Send(id, userId, content))
            {
                var latest = messageService.GetLatestMessage(id, userId, content);

                if (latest != null)
                {
                    await eventChatHub.Clients.Group($"event-room-{id}").SendAsync("ReceiveEventMessage", new
                    {
                        userName = latest.NguoiDung?.TenNguoiDung ?? latest.NguoiDung?.Email ?? "User",
                        content = latest.Content,
                        createdAt = latest.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                    });
                }
            }

            return Ok();
        }

        [HttpPost]
        [Route("room/{id}/checkin")]
        public IActionResult CheckIn(int id)
        {
            ViewBag.HideSubBar = true;

            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                var returnUrl = Url.Action("Room", "Event", new { id }) ?? "/event";
                return RedirectToLoginWithReturnUrl(returnUrl);
            }

            int userId = int.Parse(User.FindFirst("UserId")!.Value);

            var ev = eventService.FindById(id);
            if (ev == null)
                return RedirectToAction("Index");

            if ((ev.Status ?? "").Trim().Equals("Ended", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ToastMessage"] = "Sự kiện đã kết thúc, check-in đã đóng.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Room", new { id });
            }

            if (!participantService.IsJoined(id, userId))
            {
                TempData["ToastMessage"] = "Bạn chưa tham gia sự kiện này";
                TempData["ToastType"] = "error";
                return RedirectToAction("Detail", new { slug = ev.Slug });
            }

            if (participantService.CheckIn(id, userId))
            {
                TempData["ToastMessage"] = "Check-in thành công";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = "Check-in thất bại";
                TempData["ToastType"] = "error";
            }

            return RedirectToAction("Room", new { id });
        }

        [Route("my-events")]
        public IActionResult MyEvents()
        {
            ViewBag.HideSubBar = true;

            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                var returnUrl = Url.Action("MyEvents", "Event") ?? "/event";
                return RedirectToLoginWithReturnUrl(returnUrl);
            }

            int userId = int.Parse(User.FindFirst("UserId")!.Value);

            var myParticipants = participantService.GetMyEvents(userId);

            var model = myParticipants.Select(x => new MyEventCardVM
            {
                Participant = x,
                Event = x.Event,
                LatestAnnouncement = x.Event != null ? announcementService.GetLatestByEvent(x.Event.EventId) : null,
                LatestMessage = x.Event != null ? messageService.GetLatestByEvent(x.Event.EventId) : null
            }).ToList();

            return View("~/Views/Event/MyEvents.cshtml", model);
        }
    }
}