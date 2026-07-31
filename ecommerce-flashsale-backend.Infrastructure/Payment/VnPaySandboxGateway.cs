using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlashSale.Infrastructure.PaymentGateways
{
    public class VnPaySandboxGateway : IPaymentGateway
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<VnPaySandboxGateway> _logger;
        
        // VNPay Sandbox Defaults from Configuration
        private readonly string _tmnCode;
        private readonly string _hashSecret;
        private readonly string _baseUrl;
        private readonly string _returnUrl;

        public VnPaySandboxGateway(IConfiguration configuration, ILogger<VnPaySandboxGateway> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _tmnCode = _configuration["VnPay:TmnCode"];
            _hashSecret = _configuration["VnPay:HashSecret"];
            _baseUrl = _configuration["VnPay:BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            _returnUrl = _configuration["VnPay:ReturnUrl"];
        }

        public Task<PaymentUrlResult> CreatePaymentUrlAsync(PaymentRequestDto request, CancellationToken ct)
        {
            // Các tham số bắt buộc của VNPay
            var vnpParams = new SortedList<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", _tmnCode },
                { "vnp_Amount", ((long)(request.Amount * 100)).ToString() }, // Đơn vị là Xu (x100)
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", request.ClientIpAddress ?? "127.0.0.1" },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", $"Thanh toan don hang {request.OrderId}" },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", _returnUrl },
                { "vnp_TxnRef", request.TransactionCode } // Mã giao dịch do mình tự sinh (Guid)
            };

            // 1. Build Query String (vnp_TxnRef=123&vnp_Version=2.1.0...)
            var query = new StringBuilder();
            foreach (var kv in vnpParams)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    query.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            var queryString = query.ToString();
            if (queryString.EndsWith("&"))
            {
                queryString = queryString.Substring(0, queryString.Length - 1); // Bỏ dấu & cuối cùng
            }

            // 2. Ký HMAC-SHA512
            // VNPay yêu cầu chuỗi dữ liệu ký (signData) phải được sắp xếp alphabet theo Key (đã dùng SortedList ở trên)
            // Chuỗi dữ liệu ký phải giống y hệt chuỗi QueryString đã URL Encode
            var signData = queryString;
            var vnpSecureHash = HmacSha512(_hashSecret, signData);

            // 3. Ghép chữ ký vào URL cuối cùng
            var paymentUrl = $"{_baseUrl}?{queryString}&vnp_SecureHash={vnpSecureHash}";

            return Task.FromResult(new PaymentUrlResult { PaymentUrl = paymentUrl });
        }

        public Task<bool> VerifyCallbackSignatureAsync(IDictionary<string, string> callbackParams, CancellationToken ct)
        {
            if (!callbackParams.TryGetValue("vnp_SecureHash", out var vnpSecureHash) || string.IsNullOrEmpty(vnpSecureHash))
            {
                return Task.FromResult(false);
            }

            var sortedData = new SortedList<string, string>();
            foreach (var kvp in callbackParams)
            {
                if (!string.IsNullOrEmpty(kvp.Key) && kvp.Key.StartsWith("vnp_") 
                    && kvp.Key != "vnp_SecureHash" && kvp.Key != "vnp_SecureHashType")
                {
                    if (!string.IsNullOrEmpty(kvp.Value))
                    {
                        sortedData.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            var query = new StringBuilder();
            foreach (var kv in sortedData)
            {
                query.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }

            var queryString = query.ToString();
            if (queryString.EndsWith("&"))
            {
                queryString = queryString.Substring(0, queryString.Length - 1);
            }

            var signData = queryString;
            var checkSum = HmacSha512(_hashSecret, signData);

            bool isValid = checkSum.Equals(vnpSecureHash, StringComparison.InvariantCultureIgnoreCase);
            
            if (!isValid)
            {
                _logger.LogWarning("VNPay callback signature verification failed. Expected: {Expected}, Actual: {Actual}", checkSum, vnpSecureHash);
            }

            return Task.FromResult(isValid);
        }

        private string HmacSha512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2")); // hex format
                }
            }
            return hash.ToString();
        }
    }
}
