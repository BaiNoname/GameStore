namespace GameStore;
using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
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
        });

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/auth/login";
            options.AccessDeniedPath = "/auth/login";

            options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
            options.SlidingExpiration = true;

            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.Redirect("/auth/login");
                return Task.CompletedTask;
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

        // Register Vnpay service implementation
        builder.Services.AddScoped<VnpayService, VnpayServiceImpl>();


        var app = builder.Build();

        app.MapGet("/ping", () => "Wake up Sever !!!");
        
        app.UseStaticFiles();

        app.UseRouting();

        app.UseSession();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHub<GameStore.Hubs.GameHub>("/gameHub");
        app.MapHub<GameStore.Hubs.AiChatHub>("/aiChatHub");
        app.MapHub<GameStore.Hubs.ChatHub>("/chatHub");

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

