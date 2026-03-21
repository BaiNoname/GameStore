using GameStore.Models;

public class GiaoDich
{
    public string MaGD { get; set; } = null!;
    public int MaNguoiDung { get; set; }
    public DateOnly NgayMua { get; set; }
    public decimal ThanhTien { get; set; }
    public string TrangThai { get; set; }
    public string PhuongThuc { get; set; }

    // 🔥 thêm cái này
    public DateTime CreatedAt { get; set; }
    public string? VnpTransactionNo { get; set; }

    public NguoiDung NguoiDung { get; set; } = null!;
    public List<ChiTietGiaoDich> ChiTietGiaoDiches { get; set; } = new();
}