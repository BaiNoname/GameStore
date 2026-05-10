namespace GameStore.Models
{
    // Lớp ChiTietGioHang đại diện cho chi tiết của một mục trong giỏ hàng, liên kết giữa GioHang và Game
    public class ChiTietGioHang
    {
        public string MaGH { get; set; } = null!;
        public string MaGame { get; set; } = null!;
        public decimal DonGiaHienTai { get; set; }

        public GioHang GioHang { get; set; } = null!;
        public Game Game { get; set; } = null!;
    }
}