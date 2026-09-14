namespace GameStore.Models
{
    // Mã khuyến mãi (coupon) - admin tạo, hiển thị công khai để user "lấy mã".
    // Mỗi mã là giảm theo % hoặc số tiền cố định, có giới hạn số lượng phát hành.
    // Hết lượt (DaDung >= SoLuong) thì không lấy được nữa.
    public class KhuyenMai
    {
        public int MaKM { get; set; }
        public string Code { get; set; } = null!;          // mã nhập, duy nhất
        public string? MoTa { get; set; }

        public string LoaiGiam { get; set; } = "Percent";  // "Percent" | "Fixed"
        public decimal GiaTri { get; set; }                // % hoặc số tiền
        public decimal? GiamToiDa { get; set; }            // mức giảm tối đa (cho loại %)
        public decimal? DonToiThieu { get; set; }          // đơn tối thiểu để áp dụng

        public int SoLuong { get; set; }                   // tổng số lượng phát hành
        public int DaDung { get; set; }                    // số lượng đã được lấy

        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        public ICollection<NguoiDungKhuyenMai> NguoiDungKhuyenMais { get; set; } = new List<NguoiDungKhuyenMai>();

        // Tiện ích
        public bool ConLuot => DaDung < SoLuong;
    }
}
