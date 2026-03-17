using GameStore.Models;

namespace GameStore.Services
{
    public interface PaymentService
    {
        List<GiaoDich> findAll();
        List<GiaoDich> findAll(string keyword, string status, int page, int pageSize, out int totalPages);
        GiaoDich findById(string id);
        bool UpdateStatus(string id, string status);
    }
}
