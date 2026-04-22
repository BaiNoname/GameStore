using GameStore.Models;

namespace GameStore.Services
{
    public class EventRewardServiceImpl : EventRewardService
    {
        private readonly GameStoreContext db;

        public EventRewardServiceImpl(GameStoreContext _db)
        {
            db = _db;
        }

        public bool CanClaimReward(int eventId, int userId)
        {
            var ev = db.Events.FirstOrDefault(x => x.EventId == eventId);
            if (ev == null) return false;

            if (!string.Equals(ev.Status?.Trim(), "Ended", StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.IsNullOrWhiteSpace(ev.PrizeType) || string.IsNullOrWhiteSpace(ev.PrizeValue))
                return false;

            var participant = db.EventParticipants.FirstOrDefault(x => x.EventId == eventId && x.UserId == userId);
            if (participant == null) return false;

            if (!participant.IsCheckedIn) return false;
            if (participant.RewardGranted) return false;

            return true;
        }

        public RewardClaimResult ClaimReward(int eventId, int userId)
        {
            using var tran = db.Database.BeginTransaction();

            try
            {
                var ev = db.Events.FirstOrDefault(x => x.EventId == eventId);
                if (ev == null)
                    return new RewardClaimResult { Success = false, Message = "Sự kiện không tồn tại." };

                var participant = db.EventParticipants.FirstOrDefault(x => x.EventId == eventId && x.UserId == userId);
                if (participant == null)
                    return new RewardClaimResult { Success = false, Message = "Bạn không thuộc sự kiện này." };

                if (!string.Equals(ev.Status?.Trim(), "Ended", StringComparison.OrdinalIgnoreCase))
                    return new RewardClaimResult { Success = false, Message = "Sự kiện chưa kết thúc." };

                if (!participant.IsCheckedIn)
                    return new RewardClaimResult { Success = false, Message = "Bạn cần check-in trước khi nhận thưởng." };

                if (participant.RewardGranted)
                    return new RewardClaimResult { Success = false, Message = "Bạn đã nhận thưởng rồi." };

                var prizeType = (ev.PrizeType ?? "").Trim();
                var prizeValue = (ev.PrizeValue ?? "").Trim();

                var user = db.NguoiDungs.FirstOrDefault(x => x.MaNguoiDung == userId);
                var displayName = user?.TenNguoiDung ?? user?.Email ?? "Người dùng";

                if (string.Equals(prizeType, "Balance", StringComparison.OrdinalIgnoreCase))
                {
                    if (!decimal.TryParse(prizeValue, out decimal amount) || amount <= 0)
                        return new RewardClaimResult { Success = false, Message = "Giá trị thưởng Balance không hợp lệ." };

                    if (user == null)
                        return new RewardClaimResult { Success = false, Message = "Người dùng không tồn tại." };

                    user.SoDu += amount;

                    db.GiaoDiches.Add(new GiaoDich
                    {
                        MaGD = "EVR" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"),
                        MaNguoiDung = userId,
                        NgayMua = DateTime.UtcNow,
                        ThanhTien = amount,
                        TrangThai = "Success",
                        PhuongThuc = "EventReward",
                        LoaiGiaoDich = "EventReward",
                        CreatedAt = DateTime.UtcNow,
                        EventId = eventId
                    });

                    participant.RewardGranted = true;
                    participant.RewardGrantedAt = DateTime.UtcNow;

                    db.SaveChanges();
                    tran.Commit();

                    return new RewardClaimResult
                    {
                        Success = true,
                        PrizeType = "Balance",
                        Message = "Bạn đã nhận thưởng balance thành công!",
                        RoomNotice = $"🎉 {displayName} đã nhận thưởng Balance: {amount:N0} VND"
                    };
                }
                else if (string.Equals(prizeType, "Effect", StringComparison.OrdinalIgnoreCase))
                {
                    var effect = db.IconEffects.FirstOrDefault(x =>
                        x.EffectCode == prizeValue &&
                        x.IsActive);

                    if (effect == null)
                        return new RewardClaimResult { Success = false, Message = "Effect thưởng không tồn tại." };

                    var existed = db.UserIconEffects.FirstOrDefault(x =>
                        x.MaNguoiDung == userId &&
                        x.EffectId == effect.EffectId);

                    if (existed == null)
                    {
                        db.UserIconEffects.Add(new UserIconEffect
                        {
                            MaNguoiDung = userId,
                            EffectId = effect.EffectId,
                            EventId = eventId,
                            IsEquipped = false,
                            GrantedAt = DateTime.UtcNow
                        });
                    }

                    participant.RewardGranted = true;
                    participant.RewardGrantedAt = DateTime.UtcNow;

                    db.SaveChanges();
                    tran.Commit();

                    return new RewardClaimResult
                    {
                        Success = true,
                        PrizeType = "Effect",
                        Message = "Bạn đã nhận effect thành công!",
                        RoomNotice = $"✨ {displayName} đã nhận thưởng Effect: {effect.EffectName}"
                    };
                }

                return new RewardClaimResult
                {
                    Success = false,
                    Message = "Loại phần thưởng không hợp lệ."
                };
            }
            catch
            {
                tran.Rollback();
                return new RewardClaimResult
                {
                    Success = false,
                    Message = "Không thể nhận phần thưởng."
                };
            }
        }
    }
}