using GameStore.Models;

namespace GameStore.Services
{
    // Dịch vụ xử lý mã khuyến mãi (coupon)
    public interface CouponService
    {
        // ----- Trang công khai / user -----
        // Danh sách mã đang phát hành (active + trong hạn) để hiển thị công khai
        List<KhuyenMai> GetPublicList();
        // Tập MaKM mà user đã lấy (để đánh dấu "Đã lấy" trên trang danh sách)
        HashSet<int> GetClaimedIds(int userId);
        // User lấy mã (trừ số lượng NGAY khi lấy)
        (bool ok, string message) Claim(int userId, int maKM);
        // Mã user đã lấy, CHƯA dùng và còn hiệu lực -> để chọn áp trong giỏ
        List<KhuyenMai> GetMyUsableCoupons(int userId);

        // ----- Áp mã vào giỏ -----
        // Kiểm tra + tính giảm giá; trả về mã hợp lệ nếu ok
        (bool ok, string message, decimal discount, KhuyenMai? km) TryApply(int userId, string code, decimal orderTotal);
        // Lấy mã theo id nếu user còn được dùng (đã lấy, chưa dùng, còn hạn)
        KhuyenMai? GetUsableById(int userId, int maKM);
        // Tính số tiền giảm cho 1 mã + tổng đơn
        decimal ComputeDiscount(KhuyenMai km, decimal orderTotal);
        // Đánh dấu đã dùng khi thanh toán thành công
        void MarkUsed(int userId, int maKM, string maGD);

        // ----- Admin -----
        List<KhuyenMai> FindAll(string keyword, int page, int pageSize, out int totalPages);
        KhuyenMai? GetById(int maKM);
        (bool ok, string message) Create(KhuyenMai km);
        (bool ok, string message) Update(KhuyenMai km);
        (bool ok, string message) ToggleActive(int maKM);
        (bool ok, string message) Delete(int maKM);
    }
}
