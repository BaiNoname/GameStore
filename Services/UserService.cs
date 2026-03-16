using GameStore.Models;

namespace GameStore.Services
{
    public interface UserService
    {
        public List<NguoiDung> findAll();
        public NguoiDung findById(int id);
        public bool Create(NguoiDung user);
        public bool Update(NguoiDung user);
        public bool Delete(int id);
    }
}
