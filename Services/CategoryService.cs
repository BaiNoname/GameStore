using GameStore.Models;

namespace GameStore.Services
{
    public interface CategoryService
    {
        public List<TheLoaiGame> findAll();

        public TheLoaiGame findById(string id);

        public bool Create(TheLoaiGame category);
        public bool Update(TheLoaiGame category);
        public bool Delete(string id);
        public List<TheLoaiGame> findAll(string keyword, int page, int pageSize, out int totalPages);

    }
}
