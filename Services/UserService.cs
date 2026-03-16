using GameStore.Models;

namespace GameStore.Services
{
    public interface UserService
    {
        public List<NguoiDung> findAll();
        public NguoiDung findById(string id);
        public bool Create(NguoiDung user);
        public bool Update(NguoiDung user);
        public bool Delete(string id);
    }
}
