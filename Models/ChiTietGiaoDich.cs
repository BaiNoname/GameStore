namespace GameStore.Models
{
    public class ChiTietGiaoDich
    {
        public string MaGD { get; set; }
        public string MaGame { get; set; }

        public decimal DonGia { get; set; }

        public GiaoDich GiaoDich { get; set; }
        public Game Game { get; set; }
    }
}
