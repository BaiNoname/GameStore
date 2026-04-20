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

        builder.Services.AddControllersWithViews();


        builder.Services.AddHttpContextAccessor();

        var vnpayConfig = builder.Configuration.GetSection("VNPAY");

        builder.Services.AddVnpayClient(config =>
        {
            config.TmnCode = vnpayConfig["TmnCode"]!;
            config.HashSecret = vnpayConfig["HashSecret"]!;
            config.CallbackUrl = vnpayConfig["CallbackUrl"]!;
            config.BaseUrl = vnpayConfig["BaseUrl"]!;

        });

        builder.Services.AddHttpClient();
        builder.Services.Configure<ResendClientOptions>(options =>
        {
            options.ApiToken = builder.Configuration["Resend:ApiKey"];
        });
        builder.Services.AddTransient<ResendClient>();

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/auth/login";
            options.AccessDeniedPath = "/auth/access-denied";

            options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
            options.SlidingExpiration = true; // hoặc false nếu muốn hết 15p là out luôn

            options.Events = new CookieAuthenticationEvents
            {
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

        builder.Services.AddControllersWithViews()
        .AddJsonOptions(x =>
        {
            x.JsonSerializerOptions.ReferenceHandler =
                System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });

        var upstash = builder.Configuration.GetSection("Upstash");

        var host = new Uri(upstash["Url"]).Host;
        var token = upstash["Token"];

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


        builder.Services.AddSignalR();
        builder.Services.AddScoped<LocalAiService>();


        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(15);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"];

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");

        builder.Services.AddDbContext<GameStoreContext>(
            option => option.UseNpgsql(connectionString)
        );


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

        // Register Vnpay service implementation
        builder.Services.AddScoped<VnpayService, VnpayServiceImpl>();
        builder.Services.AddScoped<IMomoService, MomoServiceImpl>();

        builder.Services.AddScoped<MailHelper>();


        var app = builder.Build();

        app.MapGet("/ping", () => "Wake up Sever !!!");
        
        app.UseStaticFiles();

        app.UseRouting();

        app.UseSession();

        app.UseAuthentication();
        app.UseAuthorization();

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

        app.MapHub<GameStore.Hubs.GameHub>("/gameHub"); 
        app.MapHub<GameStore.Hubs.AiChatHub>("/aiChatHub");
        app.MapHub<GameStore.Hubs.ChatHub>("/chatHub");
        app.MapHub<GameStore.Hubs.EventChatHub>("/eventChatHub");

        app.MapControllers();


        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}"
            );

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GameStoreContext>();
            db.Database.Migrate();
        }

        app.Run();
    }
}

