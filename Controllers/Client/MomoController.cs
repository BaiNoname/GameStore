using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Client;

[Route("api/momo")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
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
            var resultCode = Request.Query["resultCode"].ToString();
            var orderId = Request.Query["orderId"].ToString();

            Console.WriteLine($"[MOMO CB] resultCode={resultCode}, orderId={orderId}");

            // Strip prefix "ORDER_"
            if (orderId.StartsWith("ORDER_"))
                orderId = orderId["ORDER_".Length..];

            if (string.IsNullOrEmpty(orderId))
                return Redirect("/cart?error=momo");

            if (resultCode == "0")
            {
                await _paymentService.CompleteMomo(orderId);
                return Redirect("/cart?success=momo");
            }

            await _paymentService.FailMomo(orderId);
            return Redirect("/cart?error=momo");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[MOMO CB] Exception: " + ex.Message);
            return Redirect("/cart?error=momo");
        }
    }

    [HttpPost("ipn")]
    public IActionResult Ipn()
    {
        // MoMo gọi IPN để xác nhận server-to-server
        return Ok();
    }
}