namespace GameStore;

using GameStore.Helpers;
using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Resend;
using StackExchange.Redis;
using System.Security.Claims;
using VNPAY.Extensions;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Đăng ký các dịch vụ cần thiết cho ứng dụng
        builder.Services.AddControllersWithViews();

        // Đăng ký HttpContextAccessor để có thể truy cập HttpContext trong các dịch vụ khác
        builder.Services.AddHttpContextAccessor();

        var vnpayConfig = builder.Configuration.GetSection("VNPAY");

        builder.Services.AddVnpayClient(config =>
        {
            config.TmnCode = vnpayConfig["TmnCode"]!;
            config.HashSecret = vnpayConfig["HashSecret"]!;
            config.CallbackUrl = vnpayConfig["CallbackUrl"]!;
            config.BaseUrl = vnpayConfig["BaseUrl"]!;

        });

        // Đăng ký ResendClient để gửi email, cấu hình API token từ appsettings.json
        builder.Services.AddHttpClient();

        // Cấu hình ResendClientOptions với API token lấy từ cấu hình
        builder.Services.Configure<ResendClientOptions>(options =>
        {
            options.ApiToken = builder.Configuration["Resend:ApiKey"];
        });
        builder.Services.AddTransient<ResendClient>();

        // Cấu hình cookie authentication để quản lý phiên đăng nhập của người dùng
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/auth/login";
            options.AccessDeniedPath = "/auth/access-denied";

            options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
            options.SlidingExpiration = true; // hoặc false nếu muốn hết 15p là out luôn

            options.Events = new CookieAuthenticationEvents
            {
                // Khi người dùng chưa đăng nhập mà truy cập vào trang yêu cầu authentication, sẽ redirect về trang login
                OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/auth"))
                    {
                        context.Response.Redirect("/auth/login");
                    }
                    else
                    {
                        context.Response.Redirect("/auth/login?expired=true");
                    }

                    return Task.CompletedTask;
                }
            };
        });

        // Cấu hình JSON serializer để tránh lỗi vòng tham chiếu khi serialize đối tượng có quan hệ
        builder.Services.AddControllersWithViews()
        .AddJsonOptions(x =>
        {
            x.JsonSerializerOptions.ReferenceHandler =
                System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });

        // Cấu hình Redis cache với StackExchange.Redis, lấy thông tin kết nối từ appsettings.json
        var upstash = builder.Configuration.GetSection("Upstash");

        var host = new Uri(upstash["Url"]).Host;
        var token = upstash["Token"];

        // Đăng ký IConnectionMultiplexer để có thể sử dụng Redis trong các dịch vụ khác
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            return ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                EndPoints = { $"{host}:6379" },
                Password = token,
                Ssl = true,
                AbortOnConnectFail = false
            });
        });

        //builder.Services.AddStackExchangeRedisCache(options =>
        //{
        //    options.Configuration = "localhost:6379";
        //    options.InstanceName = "GameStore_";
        //});

        // Đăng ký SignalR để hỗ trợ realtime communication giữa server và client
        builder.Services.AddSignalR();
        builder.Services.AddScoped<LocalAiService>();

        // Cấu hình session để lưu trữ thông tin phiên làm việc của người dùng, thời gian hết hạn 15 phút
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(15);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        // Lấy chuỗi kết nối đến database từ appsettings.json
        var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"];

        // Nếu chuỗi kết nối không tồn tại hoặc rỗng, ném lỗi để thông báo cấu hình bị thiếu
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");

        // Đăng ký DbContext với Entity Framework Core, sử dụng PostgreSQL làm database
        builder.Services.AddDbContext<GameStoreContext>(
            option => option.UseNpgsql(connectionString)
        );

        // Đăng ký các dịch vụ của ứng dụng, mỗi dịch vụ sẽ có một implementation cụ thể để thực hiện các chức năng liên quan đến game, category, user, auth, payment, cart, review, news, event, v.v.
        builder.Services.AddScoped<GameService, GameServiceImpl>();
        builder.Services.AddScoped<CategoryService, CategoryServiceImpl>();
        builder.Services.AddScoped<UserService, UserServiceImpl>();
        builder.Services.AddScoped<AuthService, AuthServiceImpl>();
        builder.Services.AddScoped<PaymentService, PaymentServiceImpl>();
        builder.Services.AddScoped<CartService, CartServiceImpl>();
        builder.Services.AddScoped<ReviewService, ReviewServiceImpl>();
        builder.Services.AddScoped<NewsService, NewsServiceImpl>();
        builder.Services.AddScoped<EventService, EventServiceImpl>();
        builder.Services.AddScoped<EventParticipantService, EventParticipantServiceImpl>();
        builder.Services.AddScoped<EventAnnouncementService, EventAnnouncementServiceImpl>();
        builder.Services.AddScoped<EventMessageService, EventMessageServiceImpl>();
        builder.Services.AddScoped<EventRewardService, EventRewardServiceImpl>();
        builder.Services.AddScoped<UserIconEffectService, UserIconEffectServiceImpl>();
        builder.Services.AddHostedService<EventStatusBackgroundService>();

        // Register Vnpay service implementation
        builder.Services.AddScoped<VnpayService, VnpayServiceImpl>();
        builder.Services.AddScoped<IMomoService, MomoServiceImpl>();

        builder.Services.AddScoped<MailHelper>();


        var app = builder.Build();

        // Thêm endpoint để kiểm tra server có đang chạy hay không, trả về "Wake up Sever !!!" khi truy cập vào /ping
        app.MapGet("/ping", () => "Wake up Sever !!!");

        // Cấu hình middleware pipeline để xử lý các yêu cầu HTTP, bao gồm phục vụ file tĩnh, định tuyến, session, authentication, authorization, và các hub của SignalR
        app.UseStaticFiles();

        // Cấu hình middleware để kiểm tra quyền truy cập dựa trên role của người dùng, nếu là admin thì không được vào các trang user và ngược lại
        app.UseRouting();

        // Cấu hình session trước authentication để có thể sử dụng session trong quá trình xác thực người dùng
        app.UseSession();

        // Cấu hình authentication và authorization để bảo vệ các trang và API của ứng dụng, chỉ cho phép người dùng đã đăng nhập mới có thể truy cập
        app.UseAuthentication();
        app.UseAuthorization();

        // Middleware tùy chỉnh để kiểm tra role của người dùng và điều hướng nếu họ cố gắng truy cập vào trang không phù hợp với role của mình
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.ToString().ToLower();

            if (context.User.Identity.IsAuthenticated)
            {
                // lấy role từ claims
                var roleClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "";
                var isAdmin = roleClaim.Equals("admin", StringComparison.OrdinalIgnoreCase);

                // ❌ Admin không được vào user pages, bao gồm "/"
                if (isAdmin &&
                    (path == "/" ||
                     path.StartsWith("/cart") ||
                     path.StartsWith("/library") ||
                     path.StartsWith("/home") ||
                     path.StartsWith("/account") ||
                     path.StartsWith("/news") ||
                     path.StartsWith("/event") ||
                     path.StartsWith("/checkout")))
                {
                    context.Response.Redirect("/admin");
                    return;
                }

                // ❌ User không được vào admin
                if (!isAdmin && path.StartsWith("/admin"))
                {
                    context.Response.Redirect("/");
                    return;
                }
            }

            await next();
        });

        // Định nghĩa các hub của SignalR để hỗ trợ realtime communication, mỗi hub sẽ có một endpoint riêng để client có thể kết nối
        app.MapHub<GameStore.Hubs.GameHub>("/gameHub"); 
        app.MapHub<GameStore.Hubs.AiChatHub>("/aiChatHub");
        app.MapHub<GameStore.Hubs.ChatHub>("/chatHub");
        app.MapHub<GameStore.Hubs.EventChatHub>("/eventChatHub");

        app.MapControllers();

        // Định nghĩa route mặc định cho các controller, nếu không có controller hoặc action nào được chỉ định trong URL, sẽ sử dụng HomeController và Index action
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}"
            );

        // Khi ứng dụng khởi động, tự động chạy migration để cập nhật database schema theo các model đã định nghĩa, đảm bảo rằng database luôn sẵn sàng để sử dụng
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GameStoreContext>();
            db.Database.Migrate();
        }

        app.Run();
    }
}

