using GameStore.Hubs;
using GameStore.Models;
using GameStore.Pagination.User;
using GameStore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace GameStore.Controllers
{
    // Controller quản lý các chức năng liên quan đến sự kiện trong GameStore
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
        private readonly GameStoreContext db;

        public EventController(
            EventService _eventService,
            EventParticipantService _participantService,
            EventAnnouncementService _announcementService,
            EventMessageService _messageService,
            PaymentService _paymentService,
            IHubContext<EventChatHub> _eventChatHub,
            EventRewardService _eventRewardService,
            UserIconEffectService _userIconEffectService,
            GameStoreContext _db)
        {
            eventService = _eventService;
            participantService = _participantService;
            announcementService = _announcementService;
            messageService = _messageService;
            paymentService = _paymentService;
            eventChatHub = _eventChatHub;
            eventRewardService = _eventRewardService;
            userIconEffectService = _userIconEffectService;
            db = _db;
        }

        // Lấy thông tin người dùng hiện tại nếu còn hoạt động, nếu không sẽ đăng xuất và trả về null
        private async Task<NguoiDung?> GetCurrentActiveUserAsync()
        {
            // Kiểm tra nếu người dùng chưa xác thực thì trả về null ngay
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return null;

            // Lấy claim UserId từ token
            var claim = User.FindFirst("UserId")?.Value;
            // Nếu claim không tồn tại hoặc không thể chuyển đổi sang int thì trả về null
            if (string.IsNullOrWhiteSpace(claim) || !int.TryParse(claim, out int userId))
                return null;

            // Truy vấn cơ sở dữ liệu để lấy thông tin người dùng và kiểm tra xem họ còn hoạt động hay không
            var user = db.NguoiDungs.FirstOrDefault(x => x.MaNguoiDung == userId && x.IsActive);

            // Nếu người dùng không tồn tại hoặc không còn hoạt động thì đăng xuất và trả về null
            if (user == null)
            {
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return null;
            }

            return user;
        }

        // Các phương thức hỗ trợ để xây dựng URL cho các trang liên quan đến sự kiện, giúp đảm bảo tính nhất quán và dễ bảo trì
        private string BuildEventListUrl(string eventType = "All", string status = "All", int page = 1)
        {
            return Url.Action("Index", "Event", new { eventType, status, page }) ?? "/event?eventType=All&status=All&page=1";
        }

        // Xây dựng URL cho trang chi tiết sự kiện dựa trên slug, có thể kèm theo returnUrl để điều hướng sau khi thực hiện hành động nào đó
        private string BuildEventDetailUrl(string slug, string? returnUrl = null)
        {
            var url = Url.Action("Detail", "Event", new { slug, returnUrl });
            return string.IsNullOrWhiteSpace(url) ? $"/event/detail/{slug}" : url;
        }

        // Xây dựng URL cho phòng sự kiện dựa trên ID, có thể kèm theo returnUrl để điều hướng sau khi thực hiện hành động nào đó
        private string BuildEventRoomUrl(int id, string? returnUrl = null)
        {
            var url = Url.Action("Room", "Event", new { id, returnUrl });
            return string.IsNullOrWhiteSpace(url) ? $"/event/room/{id}" : url;
        }

        // Phương thức để chuẩn hóa returnUrl, đảm bảo rằng nó là một URL hợp lệ và an toàn để chuyển hướng. Nếu returnUrl không hợp lệ, sẽ sử dụng fallback hoặc URL mặc định
        private string NormalizeReturnUrl(string? returnUrl, string? fallback = null)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return returnUrl;

            if (!string.IsNullOrWhiteSpace(fallback) && Url.IsLocalUrl(fallback))
                return fallback;

            return BuildEventListUrl();
        }

        //  Phương thức để chuyển hướng người dùng đến trang đăng nhập với returnUrl được mã hóa, giúp đảm bảo rằng sau khi đăng nhập thành công, người dùng sẽ được chuyển hướng trở lại trang họ đã định trước đó
        private IActionResult RedirectToLoginWithReturnUrl(string returnUrl)
        {
            return Redirect($"/auth/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        // Trang chính hiển thị danh sách sự kiện với khả năng lọc theo loại sự kiện, trạng thái và phân trang
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

            // Chuẩn bị ViewModel để truyền dữ liệu đến view, bao gồm danh sách sự kiện nổi bật, sự kiện theo bộ lọc, sự kiện đang diễn ra và sắp diễn ra, cùng với thông tin phân trang
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

        // Trang chi tiết của một sự kiện, hiển thị thông tin chi tiết về sự kiện, trạng thái tham gia của người dùng, các thông báo liên quan và các tin nhắn trong phòng sự kiện
        [Route("detail/{slug}")]
        public async Task<IActionResult> Detail(string slug, string? returnUrl = null)
        {
            ViewBag.HideSubBar = true;

            // Chuẩn bị returnUrl để chuyển hướng sau khi thực hiện các hành động như tham gia sự kiện, check-in, v.v. Nếu returnUrl không hợp lệ, sẽ sử dụng URL danh sách sự kiện làm fallback
            var fallbackListUrl = BuildEventListUrl();
            returnUrl = NormalizeReturnUrl(returnUrl, fallbackListUrl);

            // Nếu slug không hợp lệ thì chuyển hướng về trang danh sách sự kiện
            if (string.IsNullOrWhiteSpace(slug))
                return Redirect(returnUrl);

            // Tìm kiếm sự kiện theo slug, nếu không tìm thấy thì hiển thị thông báo lỗi và chuyển hướng về returnUrl
            var ev = eventService.FindBySlug(slug);
            if (ev == null)
            {
                TempData["ToastMessage"] = "Sự kiện không tồn tại";
                TempData["ToastType"] = "error";
                return Redirect(returnUrl);
            }

            // Kiểm tra xem người dùng hiện tại đã tham gia sự kiện này chưa để hiển thị trạng thái tham gia trên trang chi tiết
            bool joined = false;

            // Nếu người dùng đã đăng nhập và còn hoạt động, kiểm tra xem họ đã tham gia sự kiện này chưa
            var activeUser = await GetCurrentActiveUserAsync();
            if (activeUser != null)
            {
                joined = participantService.IsJoined(ev.EventId, activeUser.MaNguoiDung);
            }

            ViewBag.IsJoined = joined;
            ViewBag.Announcements = announcementService.GetByEvent(ev.EventId);
            ViewBag.ReturnUrl = returnUrl;

            return View("~/Views/Event/Detail.cshtml", ev);
        }

        // Phương thức xử lý yêu cầu tham gia sự kiện, bao gồm cả việc kiểm tra điều kiện tham gia, xử lý thanh toán nếu sự kiện có phí, và chuyển hướng người dùng đến phòng sự kiện nếu tham gia thành công
        [HttpPost]
        [Route("join/{id}")]
        public async Task<IActionResult> Join(int id, string method = "balance", string? returnUrl = null)
        {
            // Chuẩn bị returnUrl để chuyển hướng sau khi thực hiện hành động tham gia sự kiện. Nếu returnUrl không hợp lệ, sẽ sử dụng URL danh sách sự kiện làm fallback
            var fallbackListUrl = BuildEventListUrl();
            returnUrl = NormalizeReturnUrl(returnUrl, fallbackListUrl);

            // Nếu id không hợp lệ thì chuyển hướng về trang danh sách sự kiện
            var ev = eventService.FindById(id);
            if (ev == null)
            {
                TempData["ToastMessage"] = "Sự kiện không tồn tại";
                TempData["ToastType"] = "error";
                return Redirect(returnUrl);
            }

            // Kiểm tra nếu sự kiện đã kết thúc thì không cho phép tham gia mới và hiển thị thông báo lỗi
            if ((ev.Status ?? "").Trim().Equals("Ended", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ToastMessage"] = "Sự kiện đã kết thúc, không thể tham gia mới.";
                TempData["ToastType"] = "error";
                return Redirect(BuildEventDetailUrl(ev.Slug, returnUrl));
            }

            // Kiểm tra nếu sự kiện đã đủ số lượng người tham gia thì không cho phép tham gia mới và hiển thị thông báo lỗi
            if (ev.MaxParticipants.HasValue && ev.CurrentParticipants >= ev.MaxParticipants.Value)
            {
                TempData["ToastMessage"] = "Sự kiện đã đủ số lượng người tham gia";
                TempData["ToastType"] = "error";
                return Redirect(BuildEventDetailUrl(ev.Slug, returnUrl));
            }

            // Kiểm tra nếu sự kiện có phí tham gia mà phương thức thanh toán là số dư thì kiểm tra xem người dùng đã đăng nhập chưa, nếu chưa thì chuyển hướng đến trang đăng nhập.
            // Nếu đã đăng nhập thì kiểm tra xem họ đã tham gia sự kiện này chưa, nếu rồi thì chuyển hướng đến phòng sự kiện.
            // Nếu chưa tham gia và sự kiện miễn phí thì cho phép tham gia ngay, nếu sự kiện có phí thì tạo giao dịch thanh toán và xử lý kết quả
            var activeUser = await GetCurrentActiveUserAsync();
            if (activeUser == null)
            {
                var loginReturnUrl = BuildEventDetailUrl(ev.Slug, returnUrl);
                return RedirectToLoginWithReturnUrl(loginReturnUrl);
            }

            int userId = activeUser.MaNguoiDung;

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

        // Trang phòng sự kiện, nơi người tham gia có thể xem thông tin chi tiết về sự kiện, các thông báo mới nhất, tin nhắn trong phòng và thực hiện các hành động như check-in, gửi tin nhắn, nhận phần thưởng, v.v.
        [Route("room/{id}")]
        public async Task<IActionResult> Room(int id, string? returnUrl = null)
        {
            ViewBag.HideSubBar = true;

            // Chuẩn bị returnUrl để chuyển hướng sau khi thực hiện các hành động trong phòng sự kiện. Nếu returnUrl không hợp lệ, sẽ sử dụng URL danh sách sự kiện làm fallback
            var fallbackListUrl = BuildEventListUrl();
            returnUrl = NormalizeReturnUrl(returnUrl, fallbackListUrl);

            // Nếu id không hợp lệ thì chuyển hướng về trang danh sách sự kiện
            var activeUser = await GetCurrentActiveUserAsync();
            if (activeUser == null)
            {
                var loginReturnUrl = BuildEventRoomUrl(id, returnUrl);
                return RedirectToLoginWithReturnUrl(loginReturnUrl);
            }

            // Lấy thông tin sự kiện theo id, nếu không tìm thấy thì hiển thị thông báo lỗi và chuyển hướng về returnUrl
            int userId = activeUser.MaNguoiDung;

            // Kiểm tra nếu sự kiện đã kết thúc thì vẫn cho phép người dùng vào phòng nhưng sẽ hiển thị thông báo rằng sự kiện đã kết thúc và chat đã bị khóa.
            // Nếu sự kiện chưa kết thúc thì kiểm tra xem người dùng đã tham gia sự kiện này chưa, nếu chưa thì hiển thị thông báo lỗi và chuyển hướng về trang chi tiết sự kiện.
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

            // Lấy danh sách người tham gia, thông báo và tin nhắn liên quan đến sự kiện để hiển thị trong phòng sự kiện.
            // Cũng lấy thông tin về người tham gia hiện tại để hiển thị trạng thái check-in và khả năng nhận phần thưởng
            var participants = participantService.GetParticipantsByEvent(id);
            var announcements = announcementService.GetByEvent(id);
            var messages = messageService.GetByEvent(id, 100);
            var myParticipant = participantService.FindParticipant(id, userId);

            // Lấy danh sách userId của những người đã gửi tin nhắn trong phòng sự kiện để lấy hiệu ứng icon tương ứng
            var effectUserIds = messages
                .Where(x => x.UserId > 0)
                .Select(x => x.UserId)
                .Distinct()
                .ToList();

            // Chuẩn bị dữ liệu để truyền đến view, bao gồm thông tin sự kiện, danh sách người tham gia, thông báo, tin nhắn,
            // trạng thái của người tham gia hiện tại, khả năng nhận phần thưởng và hiệu ứng icon cho những người đã gửi tin nhắn
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

        // Phương thức xử lý việc gửi tin nhắn trong phòng sự kiện, bao gồm kiểm tra điều kiện người dùng có thể gửi tin nhắn hay không,
        // lưu tin nhắn vào cơ sở dữ liệu và phát tin nhắn mới đến tất cả các client đang kết nối trong phòng sự kiện thông qua SignalR
        [HttpPost]
        [Route("room/{id}/send-message")]
        public async Task<IActionResult> SendMessage(int id, string content, string? returnUrl = null)
        {
            // Chuẩn bị returnUrl để chuyển hướng sau khi thực hiện hành động gửi tin nhắn. Nếu returnUrl không hợp lệ, sẽ sử dụng URL danh sách sự kiện làm fallback
            var activeUser = await GetCurrentActiveUserAsync();
            if (activeUser == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Unauthorized();

                var loginReturnUrl = BuildEventRoomUrl(id, returnUrl);
                return RedirectToLoginWithReturnUrl(loginReturnUrl);
            }

            // Lấy thông tin sự kiện theo id
            int userId = activeUser.MaNguoiDung;

            // Kiểm tra nếu sự kiện đã kết thúc thì không cho phép gửi tin nhắn mới và hiển thị thông báo lỗi.
            // Nếu sự kiện chưa kết thúc thì kiểm tra xem người dùng đã tham gia sự kiện này chưa, nếu chưa thì hiển thị thông báo lỗi và chuyển hướng về trang chi tiết sự kiện.
            // Nếu nội dung tin nhắn không hợp lệ thì hiển thị thông báo lỗi và chuyển hướng về phòng sự kiện.
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

            // Nếu tất cả điều kiện đều hợp lệ thì lưu tin nhắn vào cơ sở dữ liệu và phát tin nhắn mới đến tất cả các client đang kết nối trong phòng sự kiện thông qua SignalR
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

        // Phương thức xử lý việc check-in trong phòng sự kiện, bao gồm kiểm tra điều kiện người dùng có thể check-in hay không, cập nhật trạng thái check-in của người tham gia và hiển thị thông báo kết quả
        [HttpPost]
        [Route("room/{id}/checkin")]
        public async Task<IActionResult> CheckIn(int id, string? returnUrl = null)
        {
            // Chuẩn bị returnUrl để chuyển hướng sau khi thực hiện hành động check-in. Nếu returnUrl không hợp lệ, sẽ sử dụng URL danh sách sự kiện làm fallback
            var activeUser = await GetCurrentActiveUserAsync();

            // Nếu người dùng chưa đăng nhập hoặc không còn hoạt động thì chuyển hướng đến trang đăng nhập với returnUrl được mã hóa để sau khi đăng nhập thành công sẽ được chuyển hướng trở lại phòng sự kiện
            if (activeUser == null)
            {
                var loginReturnUrl = BuildEventRoomUrl(id, returnUrl);
                return RedirectToLoginWithReturnUrl(loginReturnUrl);
            }

            // Lấy thông tin sự kiện theo id, nếu không tìm thấy thì hiển thị thông báo lỗi và chuyển hướng về returnUrl
            int userId = activeUser.MaNguoiDung;

            // Kiểm tra nếu sự kiện đã kết thúc thì không cho phép check-in mới và hiển thị thông báo lỗi.
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

        //  Trang hiển thị các sự kiện mà người dùng đã tham gia, bao gồm thông tin chi tiết về sự kiện,
        //  các thông báo mới nhất và tin nhắn trong phòng sự kiện để người dùng có thể dễ dàng theo dõi và quản lý các sự kiện mình đã tham gia
        [Route("my-events")]
        public async Task<IActionResult> MyEvents()
        {
            ViewBag.HideSubBar = true;

            // Kiểm tra nếu người dùng chưa đăng nhập hoặc không còn hoạt động thì chuyển hướng đến trang đăng nhập với returnUrl được mã hóa để sau khi đăng nhập thành công sẽ được chuyển hướng trở lại trang My Events
            var activeUser = await GetCurrentActiveUserAsync();
            if (activeUser == null)
            {
                var returnUrl = Url.Action("MyEvents", "Event") ?? "/event/my-events";
                return RedirectToLoginWithReturnUrl(returnUrl);
            }

            // Lấy danh sách các sự kiện mà người dùng đã tham gia, cùng với thông tin chi tiết về sự kiện, các thông báo mới nhất và tin nhắn trong phòng sự kiện để hiển thị trên trang My Events
            int userId = activeUser.MaNguoiDung;

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

        // Phương thức xử lý việc nhận phần thưởng sau khi sự kiện kết thúc, bao gồm kiểm tra điều kiện người dùng có thể nhận phần thưởng hay không, cập nhật trạng thái nhận phần thưởng và hiển thị thông báo kết quả
        [HttpPost]
        [Route("room/{id}/claim-reward")]
        public async Task<IActionResult> ClaimReward(int id, string? returnUrl = null)
        {
            // Chuẩn bị returnUrl để chuyển hướng sau khi thực hiện hành động nhận phần thưởng. Nếu returnUrl không hợp lệ, sẽ sử dụng URL danh sách sự kiện làm fallback
            var activeUser = await GetCurrentActiveUserAsync();
            if (activeUser == null)
            {
                var loginReturnUrl = BuildEventRoomUrl(id, returnUrl);
                return RedirectToLoginWithReturnUrl(loginReturnUrl);
            }

            // Lấy thông tin sự kiện theo id, nếu không tìm thấy thì hiển thị thông báo lỗi và chuyển hướng về returnUrl
            int userId = activeUser.MaNguoiDung;

            var result = eventRewardService.ClaimReward(id, userId);

            if (result.Success)
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "success";

                // Nếu có thông báo phần thưởng thì gửi tin nhắn vào phòng sự kiện để tất cả mọi người đều biết rằng người dùng đã nhận phần thưởng thành công
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
            // Nếu không thành công thì hiển thị thông báo lỗi, nếu có thông báo lỗi cụ thể từ service thì hiển thị, nếu không thì hiển thị thông báo lỗi mặc định
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