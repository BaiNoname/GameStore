using GameStore.Models;

// Giao dịch mua game của người dùng
public class GiaoDich
{
    public string MaGD { get; set; } = null!;
    public int MaNguoiDung { get; set; }

    public int? EventId { get; set; }

    public int? MaKM { get; set; }                 // mã khuyến mãi đã áp (nếu có)
    public decimal SoTienGiam { get; set; }        // số tiền được giảm

    public DateTime NgayMua { get; set; }
    public decimal ThanhTien { get; set; }
    public string TrangThai { get; set; }
    public string PhuongThuc { get; set; }
    public string LoaiGiaoDich { get; set; } = "GamePurchase";
    public DateTime CreatedAt { get; set; }
    public string? VnpTransactionNo { get; set; }

    public NguoiDung NguoiDung { get; set; } = null!;
    public Event? Event { get; set; }
    public List<ChiTietGiaoDich> ChiTietGiaoDiches { get; set; } = new();
}