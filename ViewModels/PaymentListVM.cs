using GameStore.Models;

namespace GameStore.ViewModels
{
    public class PaymentListVM
    {
        public List<GiaoDich> Payments { get; set; } = new();

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public string? Keyword { get; set; } // email
        public string? Status { get; set; }  // Success / Failed
    }
}