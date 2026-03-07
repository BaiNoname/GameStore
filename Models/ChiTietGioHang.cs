namespace GameStore.Models
{
    public class ChiTietGioHang
    {
        public string MaGH { get; set; } = null!;
        public string MaGame { get; set; } = null!;
        public int SoLuong { get; set; }
        public decimal DonGiaHienTai { get; set; }

        public GioHang GioHang { get; set; } = null!;
        public Game Game { get; set; } = null!;
    }
}