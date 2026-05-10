namespace GameStore.Models
{
    // Lớp đại diện cho thư viện game của người dùng, lưu trữ thông tin về các game mà người dùng đã mua hoặc tải về
    public class ThuVienGame
    {
        public int MaNguoiDung { get; set; }
        public string MaGame { get; set; }

        public bool DaTai { get; set; } = false;

        public DateTime NgayMua { get; set; }

        public NguoiDung NguoiDung { get; set; }
        public Game Game { get; set; }
    }
}
