namespace GameStore.Models
{
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
