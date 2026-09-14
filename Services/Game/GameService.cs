using GameStore.Models;

namespace GameStore.Services
{
    public interface GameService
    {
        // Lấy tất cả game
        public List<Game> findAll();
        // Tìm kiếm game với các bộ lọc và phân trang
        public List<Game> findAll(string keyword, string categoryId, int page, int pageSize, out int totalPages);
        // Tìm kiếm game theo ID
        Game? findById(string maGame);
        // Tìm kiếm game theo từ khóa
        public List<Game> SearchGames(string keyword);
        // Lọc game theo các tiêu chí
        public List<Game> FilterGames(string search, string category, int page, int pageSize);
        // Lấy các game mới nhất
        public List<Game> GetNewGames(int count);
        // Lấy các game hot nhất
        public List<Game> GetHotGames(int count);
        // Tạo mới game
        public bool Create(Game game);
        // Cập nhật game
        public bool Update(Game game);
        // Xóa game
        public bool Delete(string id);

        // Lấy danh sách các thể loại game
        GameStoreContext GetDb();

        // Lấy tất cả tên game
        public List<string> GetAllGameNames();
        // Đếm số lượng game theo các tiêu chí
        public int CountGames(string search, string category);
    }
}
