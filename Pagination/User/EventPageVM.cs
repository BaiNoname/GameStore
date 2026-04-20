using GameStore.Models;

namespace GameStore.Pagination.User
{
    public class EventPageVM
    {
        public List<Event> FeaturedEvents { get; set; } = new();
        public List<Event> Events { get; set; } = new();
        public List<Event> LiveEvents { get; set; } = new();
        public List<Event> UpcomingEvents { get; set; } = new();

        public string EventType { get; set; } = "All";
        public string Status { get; set; } = "All";
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}