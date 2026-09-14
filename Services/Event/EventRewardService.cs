namespace GameStore.Services
{
    public interface EventRewardService
    {
        // Kiểm tra xem người dùng có thể nhận thưởng cho sự kiện hay không
        bool CanClaimReward(int eventId, int userId);
        // Nhận thưởng cho sự kiện
        RewardClaimResult ClaimReward(int eventId, int userId);
    }

    public class RewardClaimResult
    {
        // Kết quả thành công hay không
        public bool Success { get; set; }
        // Loại phần thưởng
        public string? PrizeType { get; set; }
        // Thông điệp trả về
        public string Message { get; set; } = "";
        // Thông báo trong phòng
        public string RoomNotice { get; set; } = "";
    }
}