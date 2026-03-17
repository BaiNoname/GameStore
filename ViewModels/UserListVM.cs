using GameStore.Models;

namespace GameStore.ViewModels
{
    public class UserListVM
    {
        public List<NguoiDung> Users { get; set; } = new();

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public string? Keyword { get; set; }
    }
}