using GameStore.Models;

namespace GameStore.Services
{
    // Dịch vụ xử lý hoàn trả game
    public interface RefundService
    {
        // Số phút cho phép hoàn trả kể từ lúc mua
        int RefundWindowMinutes { get; }

        // Danh sách lý do mẫu (giống Steam)
        string[] Reasons { get; }

        // Lấy tập MaGame mà user đang có yêu cầu hoàn trả còn hiệu lực (Pending/Approved)
        // -> dùng để ẩn nút Refund ở thư viện
        HashSet<string> GetActiveRefundGameIds(int userId);

        // Kiểm tra 1 game trong thư viện có còn trong hạn 30 phút và chưa yêu cầu hoàn không
        bool CanRefund(ThuVienGame item, HashSet<string> activeRefundGameIds);

        // User gửi yêu cầu hoàn trả
        (bool ok, string message) RequestRefund(int userId, string gameId, string lyDo, string noiDung);

        // Danh sách yêu cầu của user (xem trạng thái)
        List<HoanTra> GetUserRefunds(int userId);

        // ----- Admin -----
        List<HoanTra> FindAll(string status, int page, int pageSize, out int totalPages);
        (bool ok, string message) Approve(int maHoanTra);
        (bool ok, string message) Reject(int maHoanTra, string ghiChu);
    }
}
