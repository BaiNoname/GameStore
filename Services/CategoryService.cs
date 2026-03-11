using GameStore.Models;

namespace GameStore.Services
{
    public interface CategoryService
    {
        public List<TheLoaiGame> findAll();

        TheLoaiGame findById(string id);
    }
}
