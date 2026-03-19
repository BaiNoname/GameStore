using GameStore.Models;

namespace GameStore.Pagination.Admin
{
    public class GameListVM
    {
        public List<Game> Games { get; set; } = new();

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public string? Keyword { get; set; }
        public string? CategoryId { get; set; }

        public List<TheLoaiGame> Categories { get; set; } = new();
    }
}