using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CyberTech.Data;
using CyberTech.Models;
using CyberTech.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CyberTech.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly PaymentService _paymentService;

        public PaymentController(
            ILogger<PaymentController> logger,
            PaymentService paymentService)
        {
            _logger = logger;
            _paymentService = paymentService;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessVNPayPayment(int orderId)
        {
            try
            {
                var result = await _paymentService.ProcessPaymentAsync(
                    orderId,
                    "VNPay",
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    paymentUrl = result.RedirectUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay payment for order {OrderId}", orderId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý thanh toán" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> VNPayReturn()
        {
            try
            {
                var responseData = Request.Query.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToString()
                );

                // Check if transaction was cancelled by user (response code 24)
                bool isCancelledByUser = false;
                string errorMessage = null;
                if (responseData.TryGetValue("vnp_ResponseCode", out string responseCode) && responseCode == "24")
                {
                    isCancelledByUser = true;
                    errorMessage = "Giao dịch không thành công do: Khách hàng hủy giao dịch";
                    _logger.LogInformation("Payment cancelled by user for order {OrderId}", GetOrderIdFromVNPayResponse(responseData));
                }

                // Process the payment callback - our updated service now handles user cancellation internally
                var result = await _paymentService.HandlePaymentCallbackAsync("VNPay", responseData);

                if (result.Success)
                {
                    return RedirectToAction("PaymentSuccess", new { orderId = GetOrderIdFromVNPayResponse(responseData) });
                }

                // If payment failed, show the appropriate message
                int orderId = GetOrderIdFromVNPayResponse(responseData);
                return RedirectToAction("PaymentFailed", new
                {
                    orderId = orderId,
                    message = isCancelledByUser ? errorMessage : result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay return");
                return RedirectToAction("PaymentFailed");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessCODPayment([FromQuery] int orderId)
        {
            try
            {
                var result = await _paymentService.ProcessPaymentAsync(orderId, "COD");

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    redirectUrl = result.RedirectUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing COD payment for order {OrderId}", orderId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý đơn hàng COD" });
            }
        }

        public IActionResult PaymentSuccess(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }

        public IActionResult PaymentFailed(int? orderId = null, string message = null)
        {
            ViewBag.OrderId = orderId;
            ViewBag.ErrorMessage = message;
            return View();
        }

        private int GetOrderIdFromVNPayResponse(IDictionary<string, string> responseData)
        {
            if (responseData.TryGetValue("vnp_TxnRef", out string orderId))
            {
                return int.Parse(orderId);
            }
            return 0;
        }
    }
}