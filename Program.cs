namespace GameStore;
using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;


public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();

        builder.Services.AddSession();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/auth/login";
            options.AccessDeniedPath = "/auth/login";

            // 🧠 tự redirect nếu chưa login
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.Redirect("/auth/login");
                return Task.CompletedTask;
            };
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


        var app = builder.Build();

        app.MapGet("/ping", () => "ok");
        
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseSession();

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

