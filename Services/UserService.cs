using GameStore.Models;

namespace GameStore.Services
{
    public interface UserService
    {
        public List<NguoiDung> findAll(string keyword, int page, int pageSize, out int totalPages);
        public NguoiDung findById(int id);
        public bool Create(NguoiDung user);
        public bool Update(NguoiDung user);
        public bool Delete(int id);
    }
}
