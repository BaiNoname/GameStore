namespace GameStore.Models
{
    // Yêu cầu hoàn trả game (refund) - kiểu Steam
    // User mua game trong vòng 30 phút có thể yêu cầu hoàn trả (chọn lý do + ghi nội dung),
    // admin duyệt thì trừ game khỏi thư viện và hoàn tiền về số dư.
    public class HoanTra
    {
        public int MaHoanTra { get; set; }
        public int MaNguoiDung { get; set; }
        public string MaGame { get; set; } = null!;
        public string? MaGD { get; set; }            // mã giao dịch mua (nếu xác định được)

        public string LyDo { get; set; } = null!;     // lý do (chọn từ danh sách)
        public string? NoiDung { get; set; }          // nội dung mô tả thêm

        public decimal SoTienHoan { get; set; }

        public string TrangThai { get; set; } = "Pending";   // Pending / Approved / Rejected

        public DateTime NgayYeuCau { get; set; }
        public DateTime? NgayXuLy { get; set; }
        public string? GhiChuAdmin { get; set; }

        public NguoiDung? NguoiDung { get; set; }
        public Game? Game { get; set; }
    }
}
