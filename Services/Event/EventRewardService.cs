namespace GameStore.Services
{
    public interface EventRewardService
    {
        bool CanClaimReward(int eventId, int userId);
        RewardClaimResult ClaimReward(int eventId, int userId);
    }

    public class RewardClaimResult
    {
        public bool Success { get; set; }
        public string? PrizeType { get; set; }
        public string Message { get; set; } = "";
        public string RoomNotice { get; set; } = "";
    }
}