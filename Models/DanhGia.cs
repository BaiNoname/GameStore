namespace GameStore.Models
{
    // Lớp DanhGia đại diện cho đánh giá của người dùng về một game
    public class DanhGia
    {
        public string MaDG { get; set; } = null!;
        public int MaNguoiDung { get; set; }
        public string MaGame { get; set; } = null!;
        public int MucDiem { get; set; }
        public string? NhanXet { get; set; }

        public DateTime NgayDanhGia { get; set; }

        public NguoiDung NguoiDung { get; set; } = null!;
        public Game Game { get; set; } = null!;
    }
}
