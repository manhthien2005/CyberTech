using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CyberTech.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CyberTech.Services.PaymentProcessors
{
    public class VNPayPaymentProcessor : IPaymentProcessor
    {
        private readonly ILogger<VNPayPaymentProcessor> _logger;
        private readonly VNPayService _vnPayService;

        public VNPayPaymentProcessor(
            ILogger<VNPayPaymentProcessor> logger,
            VNPayService vnPayService)
        {
            _logger = logger;
            _vnPayService = vnPayService;
        }

        public string PaymentMethod => "VNPay";

        public async Task<PaymentResult> ProcessPaymentAsync(Order order, string ipAddress = null)
        {
            try
            {
                _logger.LogInformation("Processing VNPay payment for order {OrderId}", order.OrderID);

                var orderInfo = $"Thanh toan don hang {order.OrderID} - {order.User?.Email}";
                var paymentUrl = _vnPayService.CreatePaymentUrl(order.OrderID, order.FinalPrice, orderInfo, ipAddress ?? "127.0.0.1");

                if (string.IsNullOrEmpty(paymentUrl))
                {
                    return PaymentResult.Failed("Could not create VNPay payment URL");
                }

                return PaymentResult.Pending(
                    message: "Redirecting to VNPay payment gateway",
                    redirectUrl: paymentUrl
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay payment for order {OrderId}", order.OrderID);
                return PaymentResult.Failed("Error processing VNPay payment");
            }
        }

        public async Task<PaymentResult> HandleCallbackAsync(IDictionary<string, string> callbackData)
        {
            try
            {
                var vnPayResponse = _vnPayService.ProcessVNPayResponse(callbackData);

                if (vnPayResponse.Success)
                {
                    return new PaymentResult
                    {
                        Success = true,
                        Message = "Payment successful",
                        TransactionId = vnPayResponse.TransactionId,
                        Amount = vnPayResponse.Amount,
                        PaymentDate = vnPayResponse.PayDate,
                        PaymentStatus = "Completed",
                        RedirectUrl = $"/Payment/PaymentSuccess?orderId={vnPayResponse.OrderId}"
                    };
                }

                return new PaymentResult
                {
                    Success = false,
                    Message = "Payment failed",
                    TransactionId = vnPayResponse.TransactionId,
                    Amount = vnPayResponse.Amount,
                    PaymentDate = vnPayResponse.PayDate,
                    PaymentStatus = "Failed",
                    RedirectUrl = $"/Payment/PaymentFailed?orderId={vnPayResponse.OrderId}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling VNPay callback");
                return PaymentResult.Failed("Error processing VNPay callback");
            }
        }

        public async Task<PaymentResult> ValidatePaymentAsync(Order order)
        {
            if (order == null)
            {
                return PaymentResult.Failed("Invalid order");
            }

            if (order.Status == "Cancelled")
            {
                return PaymentResult.Failed("Order has been cancelled");
            }

            var payment = order.Payments?.FirstOrDefault();
            if (payment != null && payment.PaymentStatus == "Completed")
            {
                return PaymentResult.Failed("Order has already been paid");
            }

            return PaymentResult.Successful();
        }
    }
}