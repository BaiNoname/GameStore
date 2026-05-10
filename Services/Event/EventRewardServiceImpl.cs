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

        // Kiểm tra và lấy thông tin người dùng nếu còn hoạt động
        private NguoiDung? GetActiveUser(int userId)
        {
            return db.NguoiDungs.FirstOrDefault(x => x.MaNguoiDung == userId && x.IsActive);
        }

        // Kiểm tra xem người dùng có đủ điều kiện để nhận thưởng hay không
        public bool CanClaimReward(int eventId, int userId)
        {
            var user = GetActiveUser(userId);
            if (user == null) return false;

            // Kiểm tra xem sự kiện có tồn tại hay không
            var ev = db.Events.FirstOrDefault(x => x.EventId == eventId);
            if (ev == null) return false;

            // Chỉ cho phép nhận thưởng khi sự kiện đã kết thúc
            if (!string.Equals(ev.Status?.Trim(), "Ended", StringComparison.OrdinalIgnoreCase))
                return false;

            // Kiểm tra xem sự kiện có phần thưởng hợp lệ hay không
            if (string.IsNullOrWhiteSpace(ev.PrizeType) || string.IsNullOrWhiteSpace(ev.PrizeValue))
                return false;

            // Kiểm tra xem người dùng có phải là người tham gia sự kiện hay không
            var participant = db.EventParticipants.FirstOrDefault(x => x.EventId == eventId && x.UserId == userId);
            if (participant == null) return false;

            if (!participant.IsCheckedIn) return false;
            if (participant.RewardGranted) return false;

            return true;
        }
        
        // Nhận thưởng cho sự kiện
        public RewardClaimResult ClaimReward(int eventId, int userId)
        {
            // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
            using var tran = db.Database.BeginTransaction();

            try
            {
                var user = GetActiveUser(userId);
                if (user == null)
                {
                    return new RewardClaimResult
                    {
                        Success = false,
                        Message = "Tài khoản không hợp lệ hoặc đã bị khóa."
                    };
                }

                // Kiểm tra xem sự kiện có tồn tại hay không
                var ev = db.Events.FirstOrDefault(x => x.EventId == eventId);
                if (ev == null)
                    return new RewardClaimResult { Success = false, Message = "Sự kiện không tồn tại." };

                // Kiểm tra xem người dùng có phải là người tham gia sự kiện hay không
                var participant = db.EventParticipants.FirstOrDefault(x => x.EventId == eventId && x.UserId == userId);
                if (participant == null)
                    return new RewardClaimResult { Success = false, Message = "Bạn không thuộc sự kiện này." };

                // Chỉ cho phép nhận thưởng khi sự kiện đã kết thúc
                if (!string.Equals(ev.Status?.Trim(), "Ended", StringComparison.OrdinalIgnoreCase))
                    return new RewardClaimResult { Success = false, Message = "Sự kiện chưa kết thúc." };

                // Kiểm tra xem sự kiện có phần thưởng hợp lệ hay không
                if (!participant.IsCheckedIn)
                    return new RewardClaimResult { Success = false, Message = "Bạn cần check-in trước khi nhận thưởng." };

                //  Kiểm tra xem người dùng đã nhận thưởng chưa
                if (participant.RewardGranted)
                    return new RewardClaimResult { Success = false, Message = "Bạn đã nhận thưởng rồi." };

                var prizeType = (ev.PrizeType ?? "").Trim();
                var prizeValue = (ev.PrizeValue ?? "").Trim();

                var displayName = user.TenNguoiDung ?? user.Email ?? "Người dùng";

                // Xử lý phần thưởng dựa trên loại phần thưởng
                if (string.Equals(prizeType, "Balance", StringComparison.OrdinalIgnoreCase))
                {
                    if (!decimal.TryParse(prizeValue, out decimal amount) || amount <= 0)
                        return new RewardClaimResult { Success = false, Message = "Giá trị thưởng Balance không hợp lệ." };

                    user.SoDu += amount;

                    // Tạo giao dịch cho phần thưởng balance
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

                    // Cập nhật trạng thái đã nhận thưởng cho người tham gia
                    participant.RewardGranted = true;
                    participant.RewardGrantedAt = DateTime.UtcNow;

                    db.SaveChanges();
                    tran.Commit();

                    // Trả về kết quả nhận thưởng thành công với thông tin phần thưởng
                    return new RewardClaimResult
                    {
                        Success = true,
                        PrizeType = "Balance",
                        Message = "Bạn đã nhận thưởng balance thành công!",
                        RoomNotice = $"🎉 {displayName} đã nhận thưởng Balance: {amount:N0} VND"
                    };
                }
                // Xử lý phần thưởng là Effect
                else if (string.Equals(prizeType, "Effect", StringComparison.OrdinalIgnoreCase))
                {
                    // Kiểm tra xem effect có tồn tại và đang hoạt động hay không
                    var effect = db.IconEffects.FirstOrDefault(x =>
                        x.EffectCode == prizeValue &&
                        x.IsActive);

                    // Nếu effect không tồn tại hoặc không hoạt động, trả về lỗi
                    if (effect == null)
                        return new RewardClaimResult { Success = false, Message = "Effect thưởng không tồn tại." };

                    // Kiểm tra xem người dùng đã có effect này chưa
                    var existed = db.UserIconEffects.FirstOrDefault(x =>
                        x.MaNguoiDung == userId &&
                        x.EffectId == effect.EffectId);

                    // Nếu chưa có thì thêm mới, nếu đã có rồi thì không cần thêm
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

                    // Trả về kết quả nhận thưởng thành công với thông tin phần thưởng
                    return new RewardClaimResult
                    {
                        Success = true,
                        PrizeType = "Effect",
                        Message = "Bạn đã nhận effect thành công!",
                        RoomNotice = $"✨ {displayName} đã nhận thưởng Effect: {effect.EffectName}"
                    };
                }

                // Nếu loại phần thưởng không hợp lệ, trả về lỗi
                return new RewardClaimResult
                {
                    Success = false,
                    Message = "Loại phần thưởng không hợp lệ."
                };
            }
            catch
            {
                // Nếu có lỗi xảy ra trong quá trình xử lý, rollback transaction và trả về lỗi chung
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