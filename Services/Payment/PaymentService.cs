using GameStore.Models;

namespace GameStore.Services
{
    // GiaoDich ở đây có thể là giao dịch mua game, nạp tiền, hoặc giao dịch liên quan đến sự kiện
    public interface PaymentService
    {
        // admin page
        // Lấy tất cả giao dịch, có thể dùng để hiển thị trong trang quản lý
        List<GiaoDich> findAll();
        // Tìm kiếm giao dịch theo từ khóa (có thể là tên người dùng, mã giao dịch, trạng thái, v.v.)
        List<GiaoDich> findAll(string keyword, string status, int page, int pageSize, out int totalPages);
        // Tìm kiếm giao dịch theo ID
        GiaoDich findById(string id);
        // Cập nhật trạng thái giao dịch (ví dụ: "pending", "completed", "failed")
        bool UpdateStatus(string id, string status);

        // user page
        // Tạo giao dịch mua game mới, trả về mã giao dịch để theo dõi
        Task<bool> Checkout(int userId);
        // Tạo giao dịch nạp tiền mới qua Momo, trả về mã giao dịch để theo dõi
        public void CreatePendingMomo(int userId, string maGD, decimal amount);
        // Hoàn tất giao dịch Momo khi nhận được callback từ Momo, cập nhật trạng thái giao dịch và số dư người dùng
        public Task CompleteMomo(string maGD);
        // Thất bại giao dịch Momo khi nhận được callback từ Momo, cập nhật trạng thái giao dịch
        public Task FailMomo(string maGD);

        // Tạo giao dịch nạp tiền mới qua thẻ ngân hàng, trả về mã giao dịch để theo dõi
        Task CompleteTopup(int userId, decimal amount);

        // event payment
        // Tạo giao dịch tạm thời cho việc thanh toán sự kiện, trả về mã giao dịch để theo dõi
        string CreatePendingEventBalance(int userId, int eventId);
        // Hoàn tất giao dịch sự kiện khi người dùng thanh toán thành công, cập nhật trạng thái giao dịch và số dư người dùng
        Task<bool> CompleteEventBalance(string maGD);
    }
}
