namespace GameStore.Models
{
    // Thông báo hiển thị trên thanh chạy ngang (marquee) ở trang người dùng.
    // LoaiTB: Custom (admin tự tạo) | NewGame | Event | Purchase (các loại tự sinh động không lưu ở đây).
    public class ThongBao
    {
        public int MaTB { get; set; }
        public string LoaiTB { get; set; } = "Custom";
        public string NoiDung { get; set; } = null!;
        public string? Link { get; set; }

        public string? MaGame { get; set; }
        public int? MaNguoiDung { get; set; }
        public int? EventId { get; set; }

        public int ThuTu { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime NgayTao { get; set; }
        public DateTime? NgayHetHan { get; set; }
    }
}
