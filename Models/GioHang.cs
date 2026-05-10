namespace GameStore.Models
{
    // Lớp đại diện cho giỏ hàng của người dùng
    public class GioHang
    {
        public string MaGH { get; set; } = null!;
        public int MaNguoiDung { get; set; }

        public NguoiDung NguoiDung { get; set; } = null!;
        public List<ChiTietGioHang> ChiTietGioHangs { get; set; } = new();
    }
}
