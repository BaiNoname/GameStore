namespace GameStore.Models
{
    public class GiaoDich
    {
        public string MaGD { get; set; } = null!;
        public string MaNguoiDung { get; set; } = null!;
        public DateOnly NgayMua { get; set; }
        public decimal ThanhTien { get; set; }
        public string? TrangThai { get; set; }

        public NguoiDung NguoiDung { get; set; } = null!;
    }
}
