namespace GameStore.Models
{
    public class Game
    {
        public string MaGame { get; set; } = null!;
        public string TenGame { get; set; } = null!;
        public string? MoTa { get; set; }
        public string? MaTheLoai { get; set; }
        public decimal Gia { get; set; }
        public DateOnly? NgayRaMat { get; set; }
        public string? Hinh { get; set; }
        public int SoLuotTai { get; set; }

        public TheLoaiGame? TheLoaiGame { get; set; }
        public List<DanhGia> DanhGias { get; set; } = new();
        public List<ChiTietGioHang> ChiTietGioHangs { get; set; } = new();
    }
}
