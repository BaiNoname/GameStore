using GameStore.Hubs;
using GameStore.Pagination.User;
using GameStore.Services;
using Microsoft.AspNetCore.Mvc;
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
        private readonly EventRewardService eventRewardService;
        private readonly UserIconEffectService userIconEffectService;

        public EventController(
            EventService _eventService,
            EventParticipantService _participantService,
            EventAnnouncementService _announcementService,
            EventMessageService _messageService,
            PaymentService _paymentService,
            IHubContext<EventChatHub> _eventChatHub,
            EventRewardService _eventRewardService,
            UserIconEffectService _userIconEffectService)
        {
            eventService = _eventService;
            participantService = _participantService;
            announcementService = _announcementService;
            messageService = _messageService;
            paymentService = _paymentService;
            eventChatHub = _eventChatHub;
            eventRewardService = _eventRewardService;
            userIconEffectService = _userIconEffectService;
        }

        private string BuildEventListUrl(string eventType = "All", string status = "All", int page = 1)
        {
            return Url.Action("Index", "Event", new { eventType, status, page }) ?? "/event?eventType=All&status=All&page=1";
        }

        private string BuildEventDetailUrl(string slug, string? returnUrl = null)
        {
            var url = Url.Action("Detail", "Event", new { slug, returnUrl });
            return string.IsNullOrWhiteSpace(url) ? $"/event/detail/{slug}" : url;
        }

        private string BuildEventRoomUrl(int id, string? returnUrl = null)
        {
            var url = Url.Action("Room", "Event", new { id, returnUrl });
            return string.IsNullOrWhiteSpace(url) ? $"/event/room/{id}" : url;
        }

        private string NormalizeReturnUrl(string? returnUrl, string? fallback = null)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return returnUrl;

            if (!string.IsNullOrWhiteSpace(fallback) && Url.IsLocalUrl(fallback))
                return fallback;

            return BuildEventListUrl();
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
        public IActionResult Detail(string slug, string? returnUrl = null)
        {
            ViewBag.HideSubBar = true;

            var fallbackListUrl = BuildEventListUrl();
            returnUrl = NormalizeReturnUrl(returnUrl, fallbackListUrl);

            if (string.IsNullOrWhiteSpace(slug))
                return Redirect(returnUrl);

            var ev = eventService.FindBySlug(slug);
            if (ev == null)
            {
                TempData["ToastMessage"] = "Sự kiện không tồn tại";
                TempData["ToastType"] = "error";
                return Redirect(returnUrl);
            }

            bool joined = false;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                int userId = int.Parse(User.FindFirst("UserId")!.Value);
                joined = participantService.IsJoined(ev.EventId, userId);
            }

            ViewBag.IsJoined = joined;
            ViewBag.Announcements = announcementService.GetByEvent(ev.EventId);
            ViewBag.ReturnUrl = returnUrl;

            return View("~/Views/Event/Detail.cshtml", ev);
        }

        [HttpPost]
        [Route("join/{id}")]
        public async Task<IActionResult> Join(int id, string method = "balance", string? returnUrl = null)
        {
            var fallbackListUrl = BuildEventListUrl();
            returnUrl = NormalizeReturnUrl(returnUrl, fallbackListUrl);

            var ev = eventService.FindById(id);
            if (ev == null)
            {
                TempData["ToastMessage"] = "Sự kiện không tồn tại";
                TempData["ToastType"] = "error";
                return Redirect(returnUrl);
            }

            if ((ev.Status ?? "").Trim().Equals("Ended", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ToastMessage"] = "Sự kiện đã kết thúc, không thể tham gia mới.";
                TempData["ToastType"] = "error";
                return Redirect(BuildEventDetailUrl(ev.Slug, returnUrl));
            }

            if (ev.MaxParticipants.HasValue && ev.CurrentParticipants >= ev.MaxParticipants.Value)
            {
                TempData["ToastMessage"] = "Sự kiện đã đủ số lượng người tham gia";
                TempData["ToastType"] = "error";
                return Redirect(BuildEventDetailUrl(ev.Slug, returnUrl));
            }

            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                var loginReturnUrl = BuildEventDetailUrl(ev.Slug, returnUrl);
                return RedirectToLoginWithReturnUrl(loginReturnUrl);
            }

            int userId = int.Parse(User.FindFirst("UserId")!.Value);

            if (participantService.IsJoined(id, userId))
            {
                TempData["ToastMessage"] = "Bạn đã tham gia sự kiện này rồi";
                TempData["ToastType"] = "success";
                return Redirect(BuildEventRoomUrl(id, returnUrl));
            }

            if ((ev.AccessType ?? "").Trim().Equals("free", StringComparison.OrdinalIgnoreCase))
            {
                if (participantService.JoinFree(id, userId))
                {
                    TempData["ToastMessage"] = "Tham gia sự kiện thành công";
                    TempData["ToastType"] = "success";
                    return Redirect(BuildEventRoomUrl(id, returnUrl));
                }

                TempData["ToastMessage"] = "Không thể tham gia sự kiện";
                TempData["ToastType"] = "error";
                return Redirect(BuildEventDetailUrl(ev.Slug, returnUrl));
            }

            var maGD = paymentService.CreatePendingEventBalance(userId, id);
            var result = await paymentService.CompleteEventBalance(maGD);

            TempData["ToastMessage"] = result
                ? "Mua quyền tham gia sự kiện thành công"
                : "Thanh toán bằng số dư thất bại";
            TempData["ToastType"] = result ? "success" : "error";

            return result
                ? Redirect(BuildEventRoomUrl(id, returnUrl))
                : Redirect(BuildEventDetailUrl(ev.Slug, returnUrl));
        }

        [Route("room/{id}")]
        public IActionResult Room(int id, string? returnUrl = null)
        {
            ViewBag.HideSubBar = true;

            var fallbackListUrl = BuildEventListUrl();
            returnUrl = NormalizeReturnUrl(returnUrl, fallbackListUrl);

            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                var loginReturnUrl = BuildEventRoomUrl(id, returnUrl);
                return RedirectToLoginWithReturnUrl(loginReturnUrl);
            }

            int userId = int.Parse(User.FindFirst("UserId")!.Value);

            var ev = eventService.FindById(id);
            if (ev == null)
            {
                TempData["ToastMessage"] = "Sự kiện không tồn tại";
                TempData["ToastType"] = "error";
                return Redirect(returnUrl);
            }

            if (!participantService.IsJoined(id, userId))
            {
                TempData["ToastMessage"] = "Bạn cần tham gia sự kiện trước";
                TempData["ToastType"] = "error";
                return Redirect(BuildEventDetailUrl(ev.Slug, returnUrl));
            }

            var participants = participantService.GetParticipantsByEvent(id);
            var announcements = announcementService.GetByEvent(id);
            var messages = messageService.GetByEvent(id, 100);
            var myParticipant = participantService.FindParticipant(id, userId);

            var effectUserIds = messages
                .Where(x => x.UserId > 0)
                .Select(x => x.UserId)
                .Distinct()
                .ToList();

            ViewBag.Participants = participants;
            ViewBag.Announcements = announcements;
            ViewBag.Messages = messages;
            ViewBag.MyParticipant = myParticipant;
            ViewBag.IsArchivedRoom = (ev.Status ?? "").Trim().Equals("Ended", StringComparison.OrdinalIgnoreCase);
            ViewBag.CanClaimReward = eventRewardService.CanClaimReward(id, userId);
            ViewBag.UserEffectMap = userIconEffectService.GetEquippedCssClassMap(effectUserIds);
            ViewBag.ReturnUrl = returnUrl;

            return View("~/Views/Event/Room.cshtml", ev);
        }

        [HttpPost]
        [Route("room/{id}/send-message")]
        public async Task<IActionResult> SendMessage(int id, string content, string? returnUrl = null)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Unauthorized();

                var loginReturnUrl = BuildEventRoomUrl(id, returnUrl);
                return RedirectToLoginWithReturnUrl(loginReturnUrl);
            }

            int userId = int.Parse(User.FindFirst("UserId")!.Value);

            var ev = eventService.FindById(id);
            if (ev == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return NotFound();

                TempData["ToastMessage"] = "Sự kiện không tồn tại";
                TempData["ToastType"] = "error";
                return Redirect(NormalizeReturnUrl(returnUrl, BuildEventListUrl()));
            }

            if ((ev.Status ?? "").Trim().Equals("Ended", StringComparison.OrdinalIgnoreCase))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest();

                TempData["ToastMessage"] = "Sự kiện đã kết thúc, chat đã bị khóa.";
                TempData["ToastType"] = "error";
                return Redirect(BuildEventRoomUrl(id, returnUrl));
            }

            if (!participantService.IsJoined(id, userId))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Forbid();

                TempData["ToastMessage"] = "Bạn chưa tham gia sự kiện này";
                TempData["ToastType"] = "error";
                return Redirect(BuildEventDetailUrl(ev.Slug, returnUrl));
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest();

                TempData["ToastMessage"] = "Nội dung tin nhắn không hợp lệ";
                TempData["ToastType"] = "error";
                return Redirect(BuildEventRoomUrl(id, returnUrl));
            }

            if (messageService.Send(id, userId, content))
            {
                var latest = messageService.GetLatestMessage(id, userId, content);
                var effectCssClass = userIconEffectService.GetEquippedCssClass(userId);

                if (latest != null)
                {
                    await eventChatHub.Clients.Group($"event-room-{id}").SendAsync("ReceiveEventMessage", new
                    {
                        userName = latest.NguoiDung?.TenNguoiDung ?? latest.NguoiDung?.Email ?? "User",
                        content = latest.Content,
                        createdAt = latest.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                        effectCssClass = effectCssClass ?? ""
                    });
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Ok();
            }
            else
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest();
            }

            return Redirect(BuildEventRoomUrl(id, returnUrl));
        }

        [HttpPost]
        [Route("room/{id}/checkin")]
        public IActionResult CheckIn(int id, string? returnUrl = null)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                var loginReturnUrl = BuildEventRoomUrl(id, returnUrl);
                return RedirectToLoginWithReturnUrl(loginReturnUrl);
            }

            int userId = int.Parse(User.FindFirst("UserId")!.Value);

            var ev = eventService.FindById(id);
            if (ev == null)
            {
                TempData["ToastMessage"] = "Sự kiện không tồn tại";
                TempData["ToastType"] = "error";
                return Redirect(NormalizeReturnUrl(returnUrl, BuildEventListUrl()));
            }

            if ((ev.Status ?? "").Trim().Equals("Ended", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ToastMessage"] = "Sự kiện đã kết thúc, check-in đã đóng.";
                TempData["ToastType"] = "error";
                return Redirect(BuildEventRoomUrl(id, returnUrl));
            }

            if (!participantService.IsJoined(id, userId))
            {
                TempData["ToastMessage"] = "Bạn chưa tham gia sự kiện này";
                TempData["ToastType"] = "error";
                return Redirect(BuildEventDetailUrl(ev.Slug, returnUrl));
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

            return Redirect(BuildEventRoomUrl(id, returnUrl));
        }

        [Route("my-events")]
        public IActionResult MyEvents()
        {
            ViewBag.HideSubBar = true;

            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                var returnUrl = Url.Action("MyEvents", "Event") ?? "/event/my-events";
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

        [HttpPost]
        [Route("room/{id}/claim-reward")]
        public async Task<IActionResult> ClaimReward(int id, string? returnUrl = null)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                var loginReturnUrl = BuildEventRoomUrl(id, returnUrl);
                return RedirectToLoginWithReturnUrl(loginReturnUrl);
            }

            int userId = int.Parse(User.FindFirst("UserId")!.Value);

            var result = eventRewardService.ClaimReward(id, userId);

            if (result.Success)
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "success";

                if (!string.IsNullOrWhiteSpace(result.RoomNotice))
                {
                    messageService.Send(id, userId, result.RoomNotice);

                    var latest = messageService.GetLatestMessage(id, userId, result.RoomNotice);
                    var effectCssClass = userIconEffectService.GetEquippedCssClass(userId);

                    if (latest != null)
                    {
                        await eventChatHub.Clients.Group($"event-room-{id}").SendAsync("ReceiveEventMessage", new
                        {
                            userName = latest.NguoiDung?.TenNguoiDung ?? latest.NguoiDung?.Email ?? "User",
                            content = latest.Content,
                            createdAt = latest.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                            effectCssClass = effectCssClass ?? ""
                        });
                    }
                }
            }
            else
            {
                TempData["ToastMessage"] = string.IsNullOrWhiteSpace(result.Message)
                    ? "Không thể nhận phần thưởng."
                    : result.Message;
                TempData["ToastType"] = "error";
            }

            return Redirect(BuildEventRoomUrl(id, returnUrl));
        }
    }
}