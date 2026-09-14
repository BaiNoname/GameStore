namespace GameStore.Models
{
    // Bảng nối: user đã LẤY / ĐÃ DÙNG mã khuyến mãi nào.
    // - Chống lấy trùng bằng unique (MaNguoiDung, MaKM)
    // - MaGD: giao dịch đã áp mã (nếu đã dùng)
    public class NguoiDungKhuyenMai
    {
        public int Id { get; set; }
        public int MaNguoiDung { get; set; }
        public int MaKM { get; set; }
        public string? MaGD { get; set; }

        public DateTime NgayLay { get; set; }
        public bool DaSuDung { get; set; } = false;
        public DateTime? NgaySuDung { get; set; }

        public NguoiDung? NguoiDung { get; set; }
        public KhuyenMai? KhuyenMai { get; set; }
    }
}
