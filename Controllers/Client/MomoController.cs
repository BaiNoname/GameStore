using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Client;

[Route("api/momo")]
public class MomoController : Controller
{
    private readonly IMomoService _momoService;
    private readonly PaymentService _paymentService;

    public MomoController(IMomoService momoService, PaymentService paymentService)
    {
        _momoService = momoService;
        _paymentService = paymentService;
    }

    [HttpGet("pay/{maGD}")]
    public async Task<IActionResult> Pay(string maGD, [FromQuery] decimal amount, [FromQuery] int userId)
    {
        try
        {
            var url = await _momoService.CreatePaymentUrlForOrder(userId, maGD, amount, Request.Host.ToString());
            return Redirect(url);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("topup")]
    public async Task<IActionResult> Topup([FromQuery] decimal amount, [FromQuery] int userId)
    {
        if (amount < 5000)
            return BadRequest("Số tiền tối thiểu là 5.000 VND");

        try
        {
            var url = await _momoService.CreatePaymentUrlForTopup(userId, amount, Request.Host.ToString());
            return Redirect(url);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback()
    {
        try
        {
            if (!_momoService.VerifyCallback(Request.Query))
                return Redirect($"{Request.Scheme}://{Request.Host}/cart?error=momo");

            var resultCode = Request.Query["resultCode"].ToString();
            var orderId = Request.Query["orderId"].ToString();

            // 🔥 FIX PREFIX ORDER_
            if (orderId.StartsWith("ORDER_"))
                orderId = orderId.Replace("ORDER_", "");

            if (string.IsNullOrEmpty(orderId))
                return Redirect($"{Request.Scheme}://{Request.Host}/cart?error=momo");

            if (resultCode == "0")
            {
                await _paymentService.CompleteMomo(orderId);
                return Redirect($"{Request.Scheme}://{Request.Host}/cart?success=momo");
            }

            await _paymentService.FailMomo(orderId);
            return Redirect($"{Request.Scheme}://{Request.Host}/cart?error=momo");
        }
        catch (Exception ex)
        {
            return Content("ERROR: " + ex.Message);
        }
    }

    [HttpPost("ipn")]
    public IActionResult Ipn()
    {
        // MoMo gọi IPN để xác nhận server-to-server
        return Ok();
    }
}