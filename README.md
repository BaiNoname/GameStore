# 🎮 GameStore — Nền tảng bán game trực tuyến (Steam-like)

> Đồ án web bán game bản quyền theo phong cách Steam: người dùng nạp tiền / thanh toán online để mua game, tải về thư viện cá nhân, đánh giá game, tham gia sự kiện (event) có chat realtime và trợ lý AI, nhận hiệu ứng icon, dùng mã khuyến mãi, và hoàn trả game. Có trang quản trị (admin) đầy đủ cho game, người dùng, giao dịch, sự kiện, khuyến mãi, hoàn trả và thông báo.

---

## 1. Tổng quan

GameStore là ứng dụng web **ASP.NET Core 8 MVC** kết nối **SQL Server**, chia làm hai khu vực:

- **Client (người dùng):** duyệt & tìm game, giỏ hàng, thanh toán (Số dư / MoMo), thư viện game đã mua, tải game, đánh giá, tham gia sự kiện, chat realtime trong phòng sự kiện, hỏi trợ lý AI về sự kiện, lấy & dùng mã khuyến mãi, hoàn trả game, quản lý hồ sơ & avatar.
- **Admin (quản trị):** dashboard thống kê, quản lý game/thể loại, người dùng, giao dịch, tin tức, sự kiện & người tham gia, mã khuyến mãi, duyệt hoàn trả, và thông báo chạy ngang (marquee).

Phân quyền dựa trên cookie authentication với hai vai trò **admin** và **user**, chặn chéo bằng middleware (admin không vào được trang user và ngược lại).

---

## 2. Công nghệ sử dụng

| Nhóm | Công nghệ |
|------|-----------|
| Nền tảng | **.NET 8** (ASP.NET Core MVC), C#, Nullable + ImplicitUsings bật |
| ORM / DB | **Entity Framework Core 8** (SqlServer, Proxies, Design, Tools) + **SQL Server** (localhost) |
| Realtime | **SignalR** (chat sự kiện, cập nhật lượt tải realtime) |
| Xác thực | Cookie Authentication (claim `UserId`, role admin/user), mật khẩu băm bằng **BCrypt.Net** |
| Cache | **Redis** qua **Upstash** (StackExchange.Redis) |
| Email / OTP | **Resend** + **Gmail SMTP** (gửi mã xác thực, đặt lại mật khẩu) |
| Thanh toán | **MoMo** (sandbox) — VNPAY đã tích hợp sẵn nhưng **hiện tắt** |
| AI | **Google Gemini** REST API (trợ lý tư vấn trong phòng sự kiện) |
| Giao diện | Razor Views (.cshtml) + **Bootstrap 5.3** + CSS thuần (hiệu ứng lửa/nước, marquee, glow) |
| Nền tảng phụ | OpenAI SDK, SendGrid (có trong package, không phải luồng chính) |

**Kiến trúc:** MVC + **Service/Interface pattern** (mỗi nghiệp vụ có `XxxService` (interface) và `XxxServiceImpl`), đăng ký qua **Dependency Injection** (`AddScoped`) trong `Program.cs`. Database tự động migrate khi khởi động (`db.Database.Migrate()`).

---

## 3. Cấu trúc thư mục

```
GameStore/
├── Program.cs                  # Cấu hình DI, middleware, auth, SignalR, migrate
├── appsettings.json            # ⚠️ Chứa secret — KHÔNG push lên git (đã .gitignore)
│
├── Models/                     # Entity + DbContext (GameStoreContext)
│
├── Controllers/
│   ├── Auth/                   # AuthController (đăng nhập, đăng ký, OTP, quên MK)
│   ├── Client/                 # Cart, Checkout, Coupon, Game, Library, MoMo
│   ├── Admin/                  # Dashboard, Game, Category, User, Payment,
│   │                           #   Event, EventParticipant, EventAnnouncement,
│   │                           #   News, Coupon, Notification, Refund, AdminAccount
│   ├── EventController.cs       # Phòng sự kiện (chat, AI, reward, tham gia)
│   ├── HomeController.cs        # Trang chủ, catalog, tìm kiếm
│   ├── NewsController.cs, ProfileController.cs, AccountController.cs, VnpayController.cs
│
├── Services/                   # Xxx + XxxImpl theo từng domain
│   ├── Ai/       (EventAiService, LocalAiService)
│   ├── Auth/  Cart/  Category/  Coupon/  Game/  News/  Review/  User/
│   ├── Event/  (Event, Participant, Message, Announcement, Reward,
│   │            EventStatusBackgroundService — job tự đổi trạng thái sự kiện)
│   ├── Payment/  MoMo/  VnPay/
│   ├── Refund/   Notification/
│
├── Hubs/                       # SignalR: GameHub, EventChatHub, ChatHub, AiChatHub
│
├── Views/                      # Razor views (Home, Game, Cart, Library, Event,
│                               #   Coupon, Profile, Account, News, Admin/*, Shared/*)
└── wwwroot/                    # CSS, JS, images (game, events, avatars)
```

---

## 4. Cơ sở dữ liệu

Database `gamestore` gồm **20 bảng** (tên bảng & cột viết thường theo convention SQL Server). Khóa chính game là chuỗi (`MaGame`), người dùng là số (`MaNguoiDung`), giao dịch là chuỗi (`MaGD`).

### Các bảng chính

| Bảng | Vai trò | Điểm đáng chú ý |
|------|---------|-----------------|
| `nguoidung` | Người dùng | `matkhau` băm BCrypt, `quyen` (admin/user), `sodu` (số dư ví), `avatar`, `resetcode` + `resetcodeexpiry` (OTP), `isverified`, `isactive` |
| `theloaigame` | Thể loại game | |
| `game` | Game | `gia`, `hinh`, `soluottai` (lượt tải/mua), `linkgame`, FK thể loại |
| `giohang` / `chitietgiohang` | Giỏ hàng & dòng giỏ | mỗi user 1 giỏ |
| `giaodich` / `chitietgiaodich` | Giao dịch & dòng giao dịch | `thanhtien`, `trangthai`, `phuongthuc`, `loaigiaodich`, `makm` + `sotiengiam` (khuyến mãi), `vnptransactionno`, FK `eventid` (khi mua vé sự kiện) |
| `thuviengame` | Thư viện game đã sở hữu | `datai` (đã tải về hay chưa — dùng cho logic hoàn trả) |
| `danhgia` | Đánh giá / review game | điểm + nội dung |
| `news` | Tin tức | |
| `event` | Sự kiện | `eventtype`, `accesstype` (Free/Paid) + `price`, `status` (Upcoming/Ongoing/Ended), `startat`/`endat`, `maxparticipants`/`currentparticipants`, thông tin phần thưởng (`prizetype`/`prizevalue`/`prizecondition`) |
| `eventparticipant` | Người tham gia sự kiện | trạng thái check-in |
| `eventmessage` | Tin nhắn phòng chat sự kiện | |
| `eventannouncement` | Thông báo trong sự kiện | |
| `iconeffect` | Hiệu ứng khung/icon | `cssclass`, `rarity` |
| `usericoneffect` | Hiệu ứng user sở hữu | `isequipped` (đang trang bị), `expiredat` |
| `hoantra` | Yêu cầu hoàn trả | trạng thái Pending/Approved/Rejected |
| `khuyenmai` | Mã khuyến mãi (coupon) | `loaigiam` (Percent/Fixed), `giatri`, `giamtoida`, `dontoithieu`, `soluong`/`dadung`, thời gian hiệu lực |
| `nguoidungkhuyenmai` | Coupon user đã “lấy” | `dasudung`, liên kết `magd` khi đã dùng |
| `thongbao` | Thông báo chạy ngang (marquee) | `loaitb` (Custom/NewGame/Event/Purchase), `link`, thứ tự, hiệu lực |

### Quan hệ nổi bật
- `nguoidung 1—n giaodich`, `giaodich 1—n chitietgiaodich —n→ game`.
- `nguoidung 1—n thuviengame —1→ game` (game đã sở hữu).
- `nguoidung 1—1 giohang 1—n chitietgiohang —1→ game`.
- `event 1—n eventparticipant / eventmessage / eventannouncement`.
- `nguoidung n—n khuyenmai` qua `nguoidungkhuyenmai`; `giaodich` tham chiếu `makm`.
- `nguoidung n—n iconeffect` qua `usericoneffect`.

---

## 5. Các chức năng chính

### 5.1. Người dùng (Client)
- **Tài khoản:** đăng ký + xác thực OTP qua email, đăng nhập, quên/đặt lại mật khẩu bằng mã, hồ sơ cá nhân, **upload avatar** từ máy.
- **Cửa hàng:** trang chủ với Hot Games (carousel **tự động chuyển slide**), New Games, catalog lọc theo thể loại, tìm kiếm, phân trang; xem chi tiết game.
- **Giỏ hàng & thanh toán:** thêm/xóa game, **áp mã khuyến mãi**, thanh toán bằng **Số dư ví** hoặc **MoMo**.
- **Thư viện:** game đã mua, **tải game về** (đánh dấu `datai`), **hoàn trả** game (theo điều kiện).
- **Đánh giá:** review game đã sở hữu.
- **Sự kiện:** xem danh sách/chi tiết, tham gia (miễn phí hoặc mua vé), vào **phòng sự kiện** → chat realtime + thông báo + **trợ lý AI (Gemini)** tư vấn trong phạm vi sự kiện + nhận **phần thưởng** (hiệu ứng icon).
- **Khuyến mãi:** trang danh sách mã công khai, bấm **“Lấy mã”** (giảm số lượng còn lại ngay khi lấy), dùng khi thanh toán.
- **Hiệu ứng icon:** trang “My Effects” xem & **trang bị** hiệu ứng khung avatar (hiển thị trong chat sự kiện).

### 5.2. Quản trị (Admin)
- **Dashboard:** thống kê doanh thu, giao dịch, người dùng, game…
- **Quản lý:** Game, Thể loại, Người dùng, Tin tức, Giao dịch.
- **Sự kiện:** tạo/sửa/xóa sự kiện, quản lý người tham gia, gửi thông báo trong sự kiện.
- **Khuyến mãi:** CRUD mã coupon.
- **Hoàn trả:** duyệt/từ chối yêu cầu hoàn trả (Pending → Approved/Rejected).
- **Thông báo marquee:** tạo thông báo tùy chỉnh chạy ngang trên đầu trang.

---

## 6. Các luồng nghiệp vụ chính (Business Logic)

### 6.1. Đăng ký & xác thực
Đăng ký → sinh **OTP**, gửi email (Resend/Gmail) → người dùng nhập mã → `isverified = true`. Mật khẩu luôn băm **BCrypt**. Đăng nhập tạo cookie với claim `UserId` + role. Middleware chặn: admin không vào trang user, user không vào `/admin`.

### 6.2. Mua game
1. Thêm game vào giỏ (không cho mua game đã sở hữu / đã trong giỏ).
2. (Tùy chọn) áp mã khuyến mãi → tính lại tổng: `finalTotal = tổng - giảm giá`. Khuyến mãi chỉ áp dụng cho **Số dư** và **MoMo**.
3. Thanh toán:
   - **Số dư:** trừ ví ngay, tạo `giaodich` (Completed), ghi vào `thuviengame`, đánh dấu coupon đã dùng, xóa giỏ.
   - **MoMo:** tạo giao dịch Pending + redirect sang MoMo; callback IPN xác nhận → hoàn tất, ghi thư viện, đánh dấu coupon.
4. `soluottai` của game tăng, phát realtime qua **GameHub** để cập nhật số lượt trên mọi client.

### 6.3. Mã khuyến mãi (Coupon)
- Cơ chế **claim-based**: mã có `soluong` giới hạn; khi user bấm **“Lấy mã”**, `dadung` tăng ngay trong transaction (mỗi user chỉ lấy 1 lần) → hết lượt là không lấy được nữa.
- Loại giảm: **Percent** (giảm %, có trần `giamtoida`) hoặc **Fixed** (giảm số tiền cố định); có điều kiện đơn tối thiểu `dontoithieu`; số tiền giảm được kẹp không vượt tổng đơn.
- Khi thanh toán thành công, bản ghi `nguoidungkhuyenmai` được đánh dấu `dasudung` + gắn `magd`.

### 6.4. Hoàn trả (Refund) — mô phỏng DRM kiểu Steam
- Chỉ hoàn trả được khi **game chưa được tải về** (`datai = false`). Đã tải → **không cho hoàn** (“Game đã được tải về nên không thể hoàn trả”).
- Yêu cầu hoàn ở trạng thái **Pending** sẽ khóa việc mua lại; admin duyệt → **Approved** (thu hồi game khỏi thư viện) hoặc **Rejected**.
- Chỉ tính các yêu cầu **Pending** khi kiểm tra “đang chờ hoàn” (tránh bug mua lại bị chặn nhầm).

### 6.5. Sự kiện & realtime
- Tham gia sự kiện (Free hoặc mua vé Paid → tạo `giaodich` loại vé). Vào **phòng sự kiện**:
  - **Chat realtime** qua **EventChatHub** (SignalR group theo `event-room-{id}`); mỗi tin hiển thị **avatar thật** của người gửi + **hiệu ứng icon** đang trang bị.
  - **Thông báo** trong sự kiện + **check-in**.
  - **Trợ lý AI (Gemini):** widget hỏi–đáp **chỉ trong phạm vi sự kiện** (thể lệ, thời gian, phần thưởng, cách tham gia…); từ chối câu hỏi ngoài phạm vi. Cấu hình `Gemini:ApiKey` + `Gemini:Model`.
  - **Phần thưởng:** đủ điều kiện → nhận **hiệu ứng icon** (ghi vào `usericoneffect`).
- **EventStatusBackgroundService:** job nền tự động chuyển trạng thái sự kiện (Upcoming → Ongoing → Ended) theo thời gian; sự kiện Ended → phòng chuyển chế độ lưu trữ (khóa chat).

### 6.6. Thông báo marquee
Thanh chạy ngang tổng hợp: thông báo admin tùy chỉnh + game mới + sự kiện sắp diễn ra + “user X vừa mua game Y”, hiển thị trên layout.

---

## 7. Realtime (SignalR Hubs)

| Hub | Endpoint | Chức năng |
|-----|----------|-----------|
| `GameHub` | `/gameHub` | Cập nhật số lượt tải/mua game realtime trên trang chủ |
| `EventChatHub` | `/eventChatHub` | Chat trong phòng sự kiện (group theo sự kiện) |
| `ChatHub` | `/chatHub` | Chat cơ bản |
| `AiChatHub` | `/aiChatHub` | Kênh chat AI |

---

## 8. Tài liệu SRS (tóm tắt)

### 8.1. Tác nhân (Actors)
- **Khách (Guest):** xem cửa hàng, tin tức, danh sách sự kiện; cần đăng nhập để mua/tham gia.
- **Người dùng (User):** toàn bộ chức năng mua/tải/đánh giá/sự kiện/khuyến mãi/hoàn trả/hồ sơ.
- **Quản trị (Admin):** quản lý toàn hệ thống.
- **Hệ thống ngoài:** MoMo (thanh toán), Gemini (AI), Resend/Gmail (email), Redis/Upstash (cache).

### 8.2. Yêu cầu chức năng (Functional Requirements) — nhóm chính
1. **Quản lý tài khoản:** đăng ký + OTP, đăng nhập/đăng xuất, quên/đặt lại mật khẩu, cập nhật hồ sơ, upload avatar.
2. **Cửa hàng & tìm kiếm:** danh sách/chi tiết game, lọc thể loại, tìm kiếm, phân trang, Hot/New games.
3. **Giỏ hàng & thanh toán:** thêm/xóa, áp mã, thanh toán Số dư/MoMo.
4. **Thư viện & tải game:** danh sách sở hữu, tải về, hoàn trả có điều kiện.
5. **Đánh giá game.**
6. **Sự kiện:** danh sách/chi tiết, tham gia, phòng chat realtime, trợ lý AI, phần thưởng.
7. **Khuyến mãi:** lấy mã, dùng mã.
8. **Hiệu ứng icon:** sở hữu & trang bị.
9. **Quản trị:** dashboard + CRUD game/thể loại/user/tin tức/sự kiện/khuyến mãi/thông báo + duyệt hoàn trả + quản lý giao dịch.

### 8.3. Yêu cầu phi chức năng (Non-Functional)
- **Bảo mật:** mật khẩu băm BCrypt; phân quyền theo role + middleware chặn chéo; xác thực email bằng OTP; secret không đưa lên git.
- **Hiệu năng:** cache Redis; job nền cho trạng thái sự kiện; realtime bằng SignalR.
- **Khả dụng:** giao diện responsive (Bootstrap 5), hỗ trợ mobile.
- **Bảo trì:** kiến trúc phân lớp Service/Impl + DI, dễ mở rộng.
- **Toàn vẹn dữ liệu:** transaction cho claim coupon & thanh toán; ràng buộc FK trong DB.

### 8.4. Một số Use Case tiêu biểu
`Đăng ký/Xác thực OTP`, `Đăng nhập`, `Tìm & xem game`, `Thêm vào giỏ`, `Áp mã khuyến mãi`, `Thanh toán (Số dư/MoMo)`, `Tải game`, `Hoàn trả game`, `Đánh giá game`, `Tham gia sự kiện`, `Chat trong sự kiện`, `Hỏi trợ lý AI`, `Nhận phần thưởng sự kiện`, `Trang bị hiệu ứng`, `Lấy mã khuyến mãi`; phía admin: `Quản lý game/user/giao dịch`, `Duyệt hoàn trả`, `Quản lý sự kiện & người tham gia`, `Quản lý khuyến mãi`, `Quản lý thông báo`, `Xem dashboard`.

---

## 9. Cấu hình & bảo mật

Cấu hình trong `appsettings.json` (⚠️ **KHÔNG commit lên git** — đã nằm trong `.gitignore`):

- `ConnectionStrings:DefaultConnection` — SQL Server localhost, database `gamestore`.
- `Upstash` — Redis (Url + Token).
- `Email` — Gmail SMTP (host/port/from/app-password) & `Resend:ApiKey`.
- `MoMo` — PartnerCode/AccessKey/SecretKey/Endpoint/RedirectUrl/IpnUrl (sandbox).
- `Gemini` — `ApiKey` (lấy tại https://aistudio.google.com/app/apikey) + `Model` (vd `gemini-flash-latest`).
- `VNPAY` — hiện **comment tắt** (không dùng).

> **Lưu ý bảo mật:** file này chứa API key và mật khẩu thật. Đã bật GitHub Push Protection; tuyệt đối không đẩy `appsettings.json` lên repo. Khi deploy nên dùng biến môi trường hoặc User Secrets.

---

## 10. Cài đặt & chạy

**Yêu cầu:** .NET 8 SDK, SQL Server (localhost), (tùy chọn) tài khoản Upstash/MoMo/Gemini.

```bash
# 1. Khôi phục package
dotnet restore

# 2. Tạo/điền appsettings.json với các key ở mục 9

# 3. Tạo database (EF Core tự migrate khi khởi động, hoặc chạy thủ công)
dotnet ef database update

# 4. Chạy các script SQL bổ sung (theo thứ tự đã cấp):
#    - gamestore_bosung_chucnang.sql      (avatar, khuyenmai, nguoidungkhuyenmai, thongbao, giaodich.makm/sotiengiam)
#    - gamestore_hoantra.sql              (hoàn trả)
#    - gamestore_effect_legendary_full.sql (hiệu ứng icon mẫu, nếu cần)

# 5. Chạy ứng dụng
dotnet run
```

Truy cập theo địa chỉ hiển thị (mặc định dev thường là `http://localhost:5285`). Endpoint kiểm tra server sống: `GET /ping`.

> Muốn expose localhost ra ngoài để MoMo callback về được, có thể dùng **Cloudflare Tunnel** rồi cập nhật `MoMo:RedirectUrl` và `MoMo:IpnUrl` sang domain đó.

---

## 11. Ghi chú kỹ thuật

- **Migrate tự động** lúc khởi động (`db.Database.Migrate()`), nên chỉ cần chạy app là schema được đồng bộ theo model.
- **Service/Impl + DI:** thêm nghiệp vụ mới → tạo interface + impl, đăng ký `AddScoped` trong `Program.cs`.
- **Session** dùng cho lưu coupon đang áp trong giỏ; **TempData** cho toast (`ToastMessage`/`ToastType`).
- Cột & bảng trong DB đặt **chữ thường**, map qua Fluent API trong `GameStoreContext`.

---

*Tài liệu này mô tả tổng quan toàn hệ thống GameStore tại thời điểm hiện tại. Khi bổ sung chức năng mới, hãy cập nhật lại các mục 4 (Database), 5–6 (Chức năng & Logic) và 8 (SRS) cho khớp.*
