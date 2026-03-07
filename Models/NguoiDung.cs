namespace GameStore.Models
{
    public class NguoiDung
    {
        public string MaNguoiDung { get; set; } = null!;
        public string TenNguoiDung { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string MatKhau { get; set; } = null!;
        public DateOnly NgayDangKy { get; set; }
        public string Quyen { get; set; } = null!;
        public decimal SoDu { get; set; }

        public List<GiaoDich> GiaoDiches { get; set; } = new();
        public List<DanhGia> DanhGias { get; set; } = new();
        public List<GioHang> GioHangs { get; set; } = new();
    }
}
