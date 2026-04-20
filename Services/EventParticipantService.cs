using GameStore.Models;

namespace GameStore.Services
{
    public interface EventParticipantService
    {
        bool IsJoined(int eventId, int userId);
        bool JoinFree(int eventId, int userId);
        bool JoinPaid(int eventId, int userId, decimal paidAmount);
        List<EventParticipant> GetParticipantsByEvent(int eventId);
        int CountJoined(int eventId);

        EventParticipant? FindParticipant(int eventId, int userId);
        bool CheckIn(int eventId, int userId);

        EventParticipant? FindById(int participantId);
        bool RemoveParticipant(int participantId);
    }
}