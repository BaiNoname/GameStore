using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace GameStore.Services;

public class MomoServiceImpl : IMomoService
{
    // Các thông tin cấu hình cần thiết để tích hợp với MoMo
    private readonly string _partnerCode;
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly string _endpoint;
    private readonly string _redirectUrl;
    private readonly string _ipnUrl;

    // HttpClient để gửi yêu cầu đến API của MoMo
    private readonly HttpClient _httpClient;

    public MomoServiceImpl(IConfiguration config, HttpClient httpClient)
    {
        var momo = config.GetSection("MoMo");
        _partnerCode = momo["PartnerCode"]!;
        _accessKey = momo["AccessKey"]!;
        _secretKey = momo["SecretKey"]!;
        _endpoint = momo["Endpoint"]!;
        _redirectUrl = momo["RedirectUrl"]!;
        _ipnUrl = momo["IpnUrl"]!;
        _httpClient = httpClient;
    }

    // Tạo URL thanh toán cho đơn hàng
    public async Task<string> CreatePaymentUrlForOrder(int userId, string maGD, decimal amount, string baseUrl)
    {
        return await CreatePaymentUrl($"ORDER_{maGD}", $"Thanh toan don hang {maGD}", amount);
    }

    // Tạo URL thanh toán để nạp tiền vào tài khoản
    public async Task<string> CreatePaymentUrlForTopup(int userId, decimal amount, string baseUrl)
    {
        var requestId = $"TOPUP_{userId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        return await CreatePaymentUrl(requestId, $"Nap tien tai khoan user {userId}", amount);
    }
    
    // Tạo URL thanh toán chung
    private async Task<string> CreatePaymentUrl(string orderId, string orderInfo, decimal amount)
    {
        var requestId = $"{orderId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var requestType = "payWithMethod";
        var extraData = "";
        var amountStr = ((long)amount).ToString();

        // Chuỗi raw signature theo đúng thứ tự và định dạng mà MoMo yêu cầu
        var rawSignature =
            $"accessKey={_accessKey}" +
            $"&amount={amountStr}" +
            $"&extraData={extraData}" +
            $"&ipnUrl={_ipnUrl}" +
            $"&orderId={orderId}" +
            $"&orderInfo={orderInfo}" +
            $"&partnerCode={_partnerCode}" +
            $"&redirectUrl={_redirectUrl}" +
            $"&requestId={requestId}" +
            $"&requestType={requestType}";

        var signature = ComputeHmacSha256(rawSignature, _secretKey);

        // Tạo body của yêu cầu theo định dạng JSON mà MoMo yêu cầu
        var body = new
        {
            partnerCode = _partnerCode,
            accessKey = _accessKey,
            requestId,
            amount = amountStr,
            orderId,
            orderInfo,
            redirectUrl = _redirectUrl,
            ipnUrl = _ipnUrl,
            extraData,
            requestType,
            signature,
            lang = "vi"
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(_endpoint, content);
        var resStr = await response.Content.ReadAsStringAsync();
        var resJson = JsonDocument.Parse(resStr).RootElement;

        // Kiểm tra mã lỗi trả về từ MoMo, nếu không phải 0 thì ném ngoại lệ với thông báo lỗi
        if (resJson.GetProperty("resultCode").GetInt32() != 0)
            throw new Exception($"MoMo error: {resJson.GetProperty("message").GetString()}");

        return resJson.GetProperty("payUrl").GetString()!;
    }
    
    // Xác thực callback từ MoMo
    public bool VerifyCallback(IQueryCollection query)
    {
        // Chuỗi raw signature theo đúng thứ tự và định dạng mà MoMo yêu cầu
        var rawSignature =
            $"accessKey={_accessKey}" +
            $"&amount={query["amount"]}" +
            $"&extraData={query["extraData"]}" +
            $"&message={query["message"]}" +
            $"&orderId={query["orderId"]}" +
            $"&orderInfo={query["orderInfo"]}" +
            $"&orderType={query["orderType"]}" +
            $"&partnerCode={query["partnerCode"]}" +
            $"&payType={query["payType"]}" +
            $"&requestId={query["requestId"]}" +
            $"&responseTime={query["responseTime"]}" +
            $"&resultCode={query["resultCode"]}" +
            $"&transId={query["transId"]}";

        // Tính HMAC SHA256 và so sánh với chữ ký trong query để xác thực callback
        var expected = ComputeHmacSha256(rawSignature, _secretKey);
        return expected == query["signature"].ToString();
    }

    // Hàm tính HMAC SHA256
    private static string ComputeHmacSha256(string data, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA256(keyBytes);
        return Convert.ToHexString(hmac.ComputeHash(dataBytes)).ToLower();
    }
}