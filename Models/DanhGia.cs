namespace GameStore.Models
{
    public class DanhGia
    {
        public string MaDG { get; set; } = null!;
        public string MaNguoiDung { get; set; } = null!;
        public string MaGame { get; set; } = null!;
        public int MucDiem { get; set; }
        public string? NhanXet { get; set; }

        public NguoiDung NguoiDung { get; set; } = null!;
        public Game Game { get; set; } = null!;
    }
}
