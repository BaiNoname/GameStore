using GameStore.Models;

namespace GameStore.ViewModels
{
    public class CategoryListVM
    {
        public List<TheLoaiGame> Categories { get; set; } = new();

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public string? Keyword { get; set; }
    }
}