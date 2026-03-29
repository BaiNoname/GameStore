using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Client
{
    [Route("vnpay")]
    public class VnpayController : Controller
    {
        private readonly VnpayService vnpayService;

        public VnpayController(VnpayService _vnpayService)
        {
            vnpayService = _vnpayService;
        }

        
    }
}
