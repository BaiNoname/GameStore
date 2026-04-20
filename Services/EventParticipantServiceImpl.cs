using GameStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Services
{
    public class EventParticipantServiceImpl : EventParticipantService
    {
        private readonly GameStoreContext db;

        public EventParticipantServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        private DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }

        public bool IsJoined(int eventId, int userId)
        {
            return db.EventParticipants.Any(x =>
                x.EventId == eventId &&
                x.UserId == userId &&
                x.JoinStatus != null &&
                x.JoinStatus.Trim().ToLower() == "joined");
        }

        public int CountJoined(int eventId)
        {
            return db.EventParticipants.Count(x =>
                x.EventId == eventId &&
                x.JoinStatus != null &&
                x.JoinStatus.Trim().ToLower() == "joined");
        }

        public List<EventParticipant> GetParticipantsByEvent(int eventId)
        {
            return db.EventParticipants
                .Include(x => x.NguoiDung)
                .Where(x => x.EventId == eventId)
                .OrderByDescending(x => x.JoinedAt)
                .ToList();
        }

        public EventParticipant? FindParticipant(int eventId, int userId)
        {
            return db.EventParticipants
                .Include(x => x.NguoiDung)
                .FirstOrDefault(x => x.EventId == eventId && x.UserId == userId);
        }

        public bool JoinFree(int eventId, int userId)
        {
            try
            {
                if (IsJoined(eventId, userId))
                    return true;

                var ev = db.Events.FirstOrDefault(x => x.EventId == eventId);
                if (ev == null) return false;

                if (ev.MaxParticipants.HasValue && ev.CurrentParticipants >= ev.MaxParticipants.Value)
                    return false;

                var participant = new EventParticipant
                {
                    EventId = eventId,
                    UserId = userId,
                    JoinStatus = "Joined",
                    PaidAmount = 0,
                    JoinedAt = UtcNow(),
                    IsCheckedIn = false
                };

                db.EventParticipants.Add(participant);
                ev.CurrentParticipants += 1;
                ev.UpdatedAt = UtcNow();

                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EVENT JOIN FREE ERROR: " + ex.Message);
                return false;
            }
        }

        public bool JoinPaid(int eventId, int userId, decimal paidAmount)
        {
            try
            {
                if (IsJoined(eventId, userId))
                    return true;

                var ev = db.Events.FirstOrDefault(x => x.EventId == eventId);
                if (ev == null) return false;

                if (ev.MaxParticipants.HasValue && ev.CurrentParticipants >= ev.MaxParticipants.Value)
                    return false;

                var participant = new EventParticipant
                {
                    EventId = eventId,
                    UserId = userId,
                    JoinStatus = "Joined",
                    PaidAmount = paidAmount,
                    JoinedAt = UtcNow(),
                    IsCheckedIn = false
                };

                db.EventParticipants.Add(participant);
                ev.CurrentParticipants += 1;
                ev.UpdatedAt = UtcNow();

                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EVENT JOIN PAID ERROR: " + ex.Message);
                return false;
            }
        }

        public bool CheckIn(int eventId, int userId)
        {
            try
            {
                var participant = db.EventParticipants
                    .FirstOrDefault(x => x.EventId == eventId && x.UserId == userId);

                if (participant == null)
                    return false;

                if (participant.IsCheckedIn)
                    return true;

                participant.IsCheckedIn = true;
                participant.CheckedInAt = UtcNow();

                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EVENT CHECKIN ERROR: " + ex.Message);
                return false;
            }
        }

        public EventParticipant? FindById(int participantId)
        {
            return db.EventParticipants
                .Include(x => x.NguoiDung)
                .Include(x => x.Event)
                .FirstOrDefault(x => x.ParticipantId == participantId);
        }

        public bool RemoveParticipant(int participantId)
        {
            try
            {
                var participant = db.EventParticipants
                    .Include(x => x.Event)
                    .FirstOrDefault(x => x.ParticipantId == participantId);

                if (participant == null)
                    return false;

                var ev = participant.Event;
                if (ev != null && ev.CurrentParticipants > 0)
                {
                    ev.CurrentParticipants -= 1;
                    ev.UpdatedAt = DateTime.UtcNow;
                }

                db.EventParticipants.Remove(participant);
                return db.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EVENT REMOVE PARTICIPANT ERROR: " + ex.Message);
                return false;
            }
        }

    }
}