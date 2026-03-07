namespace GameStore.Models
{
    public class TheLoaiGame
    {
        public string MaTheLoai { get; set; } = null!;
        public string TenLoaiGame { get; set; } = null!;

        public List<Game> Games { get; set; } = new();
    }
}
