using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GameStore.Services
{
    public class EventStatusBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;

        public EventStatusBackgroundService(IServiceScopeFactory _scopeFactory)
        {
            scopeFactory = _scopeFactory;
        }

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