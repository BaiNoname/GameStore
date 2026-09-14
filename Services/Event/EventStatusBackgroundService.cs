using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GameStore.Services
{
    // Background service để tự động cập nhật trạng thái của các sự kiện
    public class EventStatusBackgroundService : BackgroundService
    {
        // Sử dụng IServiceScopeFactory để tạo scope cho các dịch vụ cần thiết trong background service
        private readonly IServiceScopeFactory scopeFactory;

        public EventStatusBackgroundService(IServiceScopeFactory _scopeFactory)
        {
            scopeFactory = _scopeFactory;
        }

        // Phương thức thực thi chính của background service, sẽ chạy liên tục cho đến khi bị hủy
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var eventService = scope.ServiceProvider.GetRequiredService<EventService>();
                    eventService.RefreshEventStatuses();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("EVENT STATUS BACKGROUND ERROR: " + ex.Message);
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}