using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers.Client;

// Controller để xử lý các yêu cầu liên quan đến thanh toán qua MoMo
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

    // Tạo URL thanh toán MoMo cho đơn hàng
    [HttpGet("pay/{maGD}")]
    public async Task<IActionResult> Pay(string maGD, [FromQuery] decimal amount, [FromQuery] int userId)
    {
        // Kiểm tra số tiền tối thiểu
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

    // Tạo URL thanh toán MoMo để nạp tiền vào ví
    [HttpGet("topup")]
    public async Task<IActionResult> Topup([FromQuery] decimal amount, [FromQuery] int userId)
    {
        // Kiểm tra số tiền tối thiểu
        if (amount < 5000)
            return BadRequest("Số tiền tối thiểu là 5.000 VND");

        // Tạo URL thanh toán MoMo cho việc nạp tiền vào ví
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

    // Callback URL mà MoMo sẽ gọi sau khi người dùng hoàn tất thanh toán
    [HttpGet("callback")]
    public async Task<IActionResult> Callback()
    {
        // MoMo sẽ trả về resultCode và orderId qua query string
        try
        {
            var resultCode = Request.Query["resultCode"].ToString();
            var orderId = Request.Query["orderId"].ToString();

            Console.WriteLine($"[MOMO CB] resultCode={resultCode}, orderId={orderId}");

            // Strip prefix "ORDER_"
            // MoMo có thể trả về orderId với prefix "ORDER_", cần loại bỏ nó để lấy đúng orderId
            if (orderId.StartsWith("ORDER_"))
                orderId = orderId["ORDER_".Length..];

            // Nếu không có orderId, coi như lỗi
            if (string.IsNullOrEmpty(orderId))
                return Redirect("/cart?error=momo");

            // resultCode "0" nghĩa là thanh toán thành công, các giá trị khác coi như thất bại
            if (resultCode == "0")
            {
                await _paymentService.CompleteMomo(orderId);
                return Redirect("/cart?success=momo");
            }

            // Nếu không thành công, gọi service để đánh dấu đơn hàng thất bại
            await _paymentService.FailMomo(orderId);
            return Redirect("/cart?error=momo");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[MOMO CB] Exception: " + ex.Message);
            return Redirect("/cart?error=momo");
        }
    }

    // IPN URL mà MoMo sẽ gọi để xác nhận giao dịch server-to-server
    [HttpPost("ipn")]
    public IActionResult Ipn()
    {
        // MoMo gọi IPN để xác nhận server-to-server
        return Ok();
    }
}