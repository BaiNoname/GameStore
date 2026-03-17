namespace GameStore.Models
{
    public class GiaoDich
    {
        public string MaGD { get; set; } = null!;
        public int MaNguoiDung { get; set; }
        public DateOnly NgayMua { get; set; }
        public decimal ThanhTien { get; set; }
        public string TrangThai { get; set; } // Success / Failed
        public string PhuongThuc { get; set; } // Balance / VNPAY


        public NguoiDung NguoiDung { get; set; } = null!;
        public List<ChiTietGiaoDich> ChiTietGiaoDiches { get; set; } = new();
    }
}
