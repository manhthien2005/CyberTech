using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CyberTech.Models;
using Microsoft.Extensions.Logging;

namespace CyberTech.Services.PaymentProcessors
{
    public class CODPaymentProcessor : IPaymentProcessor
    {
        private readonly ILogger<CODPaymentProcessor> _logger;

        public CODPaymentProcessor(ILogger<CODPaymentProcessor> logger)
        {
            _logger = logger;
        }

        public string PaymentMethod => "COD";

        public async Task<PaymentResult> ProcessPaymentAsync(Order order, string ipAddress = null)
        {
            try
            {
                _logger.LogInformation("Processing COD payment for order {OrderId}", order.OrderID);

                // For COD, we just mark the payment as pending since it will be paid upon delivery
                return PaymentResult.Pending(
                    message: "COD payment registered successfully",
                    redirectUrl: $"/Payment/PaymentSuccess?orderId={order.OrderID}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing COD payment for order {OrderId}", order.OrderID);
                return PaymentResult.Failed("Error processing COD payment");
            }
        }

        public async Task<PaymentResult> HandleCallbackAsync(IDictionary<string, string> callbackData)
        {
            // COD doesn't have a callback process
            return await Task.FromResult(PaymentResult.Successful());
        }

        public async Task<PaymentResult> ValidatePaymentAsync(Order order)
        {
            // For COD, basic validation is enough since payment happens on delivery
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