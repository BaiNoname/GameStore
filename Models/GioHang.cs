namespace GameStore.Models
{
    public class GioHang
    {
        public string MaGH { get; set; } = null!;
        public string MaNguoiDung { get; set; } = null!;

        public NguoiDung NguoiDung { get; set; } = null!;
        public List<ChiTietGioHang> ChiTietGioHangs { get; set; } = new();
    }
}
