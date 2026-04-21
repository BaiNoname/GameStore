using GameStore.Models;

namespace GameStore.Pagination.User
{
    public class MyEventCardVM
    {
        public EventParticipant Participant { get; set; } = null!;
        public Event? Event { get; set; }
        public EventAnnouncement? LatestAnnouncement { get; set; }
        public EventMessage? LatestMessage { get; set; }
    }
}