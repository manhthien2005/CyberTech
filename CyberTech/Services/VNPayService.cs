using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CyberTech.Services
{
    public class VNPayService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<VNPayService> _logger;
        private readonly string _tmnCode;
        private readonly string _hashSecret;
        private readonly string _baseUrl;
        private readonly string _returnUrl;

        public VNPayService(IConfiguration configuration, ILogger<VNPayService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _tmnCode = _configuration["VNPay:TmnCode"];
            _hashSecret = _configuration["VNPay:HashSecret"];
            _baseUrl = _configuration["VNPay:BaseUrl"];
            _returnUrl = _configuration["VNPay:ReturnUrl"];
        }

        public string CreatePaymentUrl(int orderId, decimal amount, string orderInfo, string ipAddress)
        {
            try
            {
                var vnpay = new VnPayLibrary();
                var createDate = DateTime.Now;

                vnpay.AddRequestData("vnp_Version", "2.1.0");
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", _tmnCode);
                vnpay.AddRequestData("vnp_Amount", ((long)(amount * 100)).ToString()); // Convert to long to avoid decimal issues
                vnpay.AddRequestData("vnp_CreateDate", createDate.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", ipAddress);
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_OrderInfo", orderInfo);
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", _returnUrl);
                vnpay.AddRequestData("vnp_TxnRef", orderId.ToString());
                vnpay.AddRequestData("vnp_ExpireDate", createDate.AddMinutes(15).ToString("yyyyMMddHHmmss")); // Add expire date

                string paymentUrl = vnpay.CreateRequestUrl(_baseUrl, _hashSecret);
                return paymentUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPay payment URL for order {OrderId}", orderId);
                return null;
            }
        }

        public VNPayResponse ProcessVNPayResponse(IDictionary<string, string> responseData)
        {
            try
            {
                var vnpay = new VnPayLibrary();
                foreach (var kvp in responseData)
                {
                    if (!string.IsNullOrEmpty(kvp.Value))
                    {
                        vnpay.AddResponseData(kvp.Key, kvp.Value);
                    }
                }

                var orderId = Convert.ToInt32(vnpay.GetResponseData("vnp_TxnRef"));
                var vnPayTranId = vnpay.GetResponseData("vnp_TransactionNo");
                var vnpResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                var vnpSecureHash = responseData["vnp_SecureHash"];
                var orderInfo = vnpay.GetResponseData("vnp_OrderInfo");
                var vnpAmount = Convert.ToDecimal(vnpay.GetResponseData("vnp_Amount")) / 100;
                var vnpPayDate = DateTime.ParseExact(vnpay.GetResponseData("vnp_PayDate"), "yyyyMMddHHmmss", CultureInfo.InvariantCulture);

                bool checkSignature = vnpay.ValidateSignature(vnpSecureHash, _hashSecret);

                return new VNPayResponse
                {
                    Success = checkSignature && vnpResponseCode == "00",
                    OrderId = orderId,
                    TransactionId = vnPayTranId,
                    Amount = vnpAmount,
                    OrderInfo = orderInfo,
                    PayDate = vnpPayDate,
                    ResponseCode = vnpResponseCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay response");
                return new VNPayResponse { Success = false };
            }
        }
    }

    public class VNPayResponse
    {
        public bool Success { get; set; }
        public int OrderId { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string OrderInfo { get; set; }
        public DateTime PayDate { get; set; }
        public string ResponseCode { get; set; }
    }

    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
        private readonly SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var retValue) ? retValue : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
        {
            var data = new StringBuilder();

            foreach (var kvp in _requestData.Where(kvp => !string.IsNullOrEmpty(kvp.Value)))
            {
                data.Append(WebUtility.UrlEncode(kvp.Key) + "=" + WebUtility.UrlEncode(kvp.Value) + "&");
            }

            string querystring = data.ToString();

            baseUrl += "?" + querystring;
            string signData = querystring;
            if (signData.Length > 0)
            {
                signData = signData.Remove(data.Length - 1, 1);
            }

            string vnp_SecureHash = HmacSHA512(vnp_HashSecret, signData);
            baseUrl += "vnp_SecureHash=" + vnp_SecureHash;

            return baseUrl;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            var rspRaw = GetResponseData();
            string myChecksum = HmacSHA512(secretKey, rspRaw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string GetResponseData()
        {
            var data = new StringBuilder();
            if (_responseData.ContainsKey("vnp_SecureHash"))
            {
                _responseData.Remove("vnp_SecureHash");
            }

            foreach (var kvp in _responseData.Where(kvp => !string.IsNullOrEmpty(kvp.Value)))
            {
                data.Append(WebUtility.UrlEncode(kvp.Key) + "=" + WebUtility.UrlEncode(kvp.Value) + "&");
            }

            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1);
            }

            return data.ToString();
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }
}