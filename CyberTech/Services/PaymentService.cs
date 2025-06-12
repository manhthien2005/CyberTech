using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CyberTech.Data;
using CyberTech.Models;
using CyberTech.Services.PaymentProcessors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CyberTech.Services
{
    public class PaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentService> _logger;
        private readonly IEmailService _emailService;
        private readonly IEnumerable<IPaymentProcessor> _paymentProcessors;
        private readonly EmailTemplateService _emailTemplateService;
        private readonly IVoucherTokenService _voucherTokenService;

        public PaymentService(
            ApplicationDbContext context,
            ILogger<PaymentService> logger,
            IEmailService emailService,
            IEnumerable<IPaymentProcessor> paymentProcessors,
            EmailTemplateService emailTemplateService,
            IVoucherTokenService voucherTokenService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _paymentProcessors = paymentProcessors;
            _emailTemplateService = emailTemplateService;
            _voucherTokenService = voucherTokenService;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(int orderId, string paymentMethod, string ipAddress = null)
        {
            try
            {
                var order = await GetOrderWithDetailsAsync(orderId);
                if (order == null)
                {
                    return PaymentResult.Failed("Order not found");
                }

                var processor = GetPaymentProcessor(paymentMethod);
                if (processor == null)
                {
                    return PaymentResult.Failed($"Payment method {paymentMethod} not supported");
                }

                // Validate payment
                var validationResult = await processor.ValidatePaymentAsync(order);
                if (!validationResult.Success)
                {
                    return validationResult;
                }

                // Process payment
                var result = await processor.ProcessPaymentAsync(order, ipAddress);
                if (result.Success)
                {
                    // Update payment record
                    await UpdatePaymentRecordAsync(order, result, processor.PaymentMethod);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for order {OrderId}", orderId);
                return PaymentResult.Failed("Error processing payment");
            }
        }

        public async Task<PaymentResult> HandlePaymentCallbackAsync(string paymentMethod, IDictionary<string, string> callbackData)
        {
            try
            {
                var processor = GetPaymentProcessor(paymentMethod);
                if (processor == null)
                {
                    return PaymentResult.Failed($"Payment method {paymentMethod} not supported");
                }

                // Check for VNPay user cancellation (response code 24)
                bool isVNPayUserCancellation = false;
                if (paymentMethod.Equals("VNPay", StringComparison.OrdinalIgnoreCase) &&
                    callbackData.TryGetValue("vnp_ResponseCode", out string responseCode) &&
                    responseCode == "24")
                {
                    isVNPayUserCancellation = true;
                    _logger.LogInformation("VNPay transaction cancelled by user (code 24)");
                }

                var result = await processor.HandleCallbackAsync(callbackData);

                // For VNPay user cancellation, we handle it specially
                if (isVNPayUserCancellation)
                {
                    // Override the result message
                    result = PaymentResult.Failed("Giao dịch không thành công do: Khách hàng hủy giao dịch");
                }

                // Process the result
                int orderId = GetOrderIdFromCallback(paymentMethod, callbackData, result);
                var order = await GetOrderWithDetailsAsync(orderId);

                if (order != null)
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Update payment status
                        await UpdatePaymentRecordAsync(order, result, paymentMethod);

                        // Update order status
                        order.Status = result.Success ? "Processing" : "Cancelled";

                        // If payment successful, update user stats and handle rank upgrade
                        if (result.Success && order.User != null)
                        {
                            await UpdateUserStatsAsync(order);

                            // Send order confirmation email
                            await SendOrderConfirmationEmailAsync(order);

                            // Send voucher emails if applicable
                            await SendVoucherEmailsAsync(order);
                        }
                        // If payment failed, restore product quantities
                        else if (!result.Success)
                        {
                            await RestoreProductQuantitiesAsync(order);
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error processing payment callback for order {OrderId}", orderId);
                        return PaymentResult.Failed("Error processing payment callback");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment callback");
                return PaymentResult.Failed("Error handling payment callback");
            }
        }

        private async Task<Order> GetOrderWithDetailsAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.User)
                    .ThenInclude(u => u.Rank)
                .Include(o => o.Payments)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);
        }

        private IPaymentProcessor GetPaymentProcessor(string paymentMethod)
        {
            return _paymentProcessors.FirstOrDefault(p => p.PaymentMethod.Equals(paymentMethod, StringComparison.OrdinalIgnoreCase));
        }

        private async Task UpdatePaymentRecordAsync(Order order, PaymentResult result, string paymentMethod)
        {
            var payment = order.Payments.FirstOrDefault();
            if (payment == null)
            {
                payment = new Payment
                {
                    OrderID = order.OrderID,
                    PaymentMethod = paymentMethod,
                    Amount = result.Amount > 0 ? result.Amount : order.FinalPrice,
                    PaymentStatus = result.PaymentStatus,
                    PaymentDate = result.PaymentDate ?? DateTime.Now
                };
                _context.Payments.Add(payment);
            }
            else
            {
                payment.PaymentStatus = result.PaymentStatus;
                payment.PaymentDate = result.PaymentDate ?? DateTime.Now;
                if (result.Amount > 0)
                {
                    payment.Amount = result.Amount;
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task UpdateUserStatsAsync(Order order)
        {
            var user = order.User;
            if (user == null)
            {
                _logger.LogWarning("Cannot update user stats - user is null for order {OrderId}", order.OrderID);
                return;
            }

            var oldRank = user.Rank;
            var oldRankName = oldRank?.RankName ?? "Thành viên";

            user.TotalSpent += order.FinalPrice;
            user.OrderCount++;

            // Check for rank upgrade
            var newRank = await _context.Ranks
                .Where(r => r.MinTotalSpent <= user.TotalSpent)
                .OrderByDescending(r => r.MinTotalSpent)
                .FirstOrDefaultAsync();

            if (newRank != null && (user.RankId != newRank.RankId || user.RankId == null))
            {
                var oldRankId = user.RankId;
                user.RankId = newRank.RankId;
                _context.Users.Update(user);

                // Send rank upgrade email only if there's an actual rank change
                if (!string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        await _emailService.SendRankUpgradeEmailAsync(
                            email: user.Email,
                            userName: user.Name ?? "Valued Customer",
                            oldRankName: oldRankName,
                            newRankName: newRank.RankName,
                            newDiscountPercent: newRank.DiscountPercent ?? 0
                        );
                        _logger.LogInformation(
                            "Rank upgrade email sent to user {UserId} - Old Rank: {OldRank}, New Rank: {NewRank}",
                            user.UserID, oldRankId, newRank.RankId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending rank upgrade email to user {UserId}", user.UserID);
                    }
                }
                else
                {
                    _logger.LogWarning("Cannot send rank upgrade email - email is null for user {UserId}", user.UserID);
                }
            }
        }

        private async Task RestoreProductQuantitiesAsync(Order order)
        {
            foreach (var orderItem in order.OrderItems)
            {
                var product = orderItem.Product;
                if (product != null)
                {
                    product.Stock += orderItem.Quantity;
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task<bool> RestoreProductStockAsync(int orderId)
        {
            try
            {
                var order = await GetOrderWithDetailsAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Cannot restore product stock - order {OrderId} not found", orderId);
                    return false;
                }

                await RestoreProductQuantitiesAsync(order);

                // Update order status to Cancelled
                order.Status = "Cancelled";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product stock restored for cancelled order {OrderId}", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring product stock for order {OrderId}", orderId);
                return false;
            }
        }

        private int GetOrderIdFromCallback(string paymentMethod, IDictionary<string, string> callbackData, PaymentResult result)
        {
            // Extract order ID based on payment method
            switch (paymentMethod.ToUpper())
            {
                case "VNPAY":
                    if (callbackData.TryGetValue("vnp_TxnRef", out string orderId))
                    {
                        return int.Parse(orderId);
                    }
                    break;
                    // Add cases for other payment methods here
            }

            return 0;
        }

        private async Task SendOrderConfirmationEmailAsync(Order order)
        {
            try
            {
                if (order.User == null || string.IsNullOrEmpty(order.User.Email))
                {
                    _logger.LogWarning("Cannot send order confirmation email - user or email is null for order {OrderId}", order.OrderID);
                    return;
                }

                var (subject, content) = _emailTemplateService.GetOrderConfirmationTemplate(order);

                await _emailService.SendEmailAsync(order.User.Email, subject, content);
                _logger.LogInformation("Order confirmation email sent for order {OrderId} to {Email}", order.OrderID, order.User.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order confirmation email for order {OrderId}", order.OrderID);
            }
        }

        private async Task SendVoucherEmailsAsync(Order order)
        {
            try
            {
                if (order.User == null || string.IsNullOrEmpty(order.User.Email))
                {
                    _logger.LogWarning("Cannot send voucher emails - user or email is null for order {OrderId}", order.OrderID);
                    return;
                }

                // Check if order total is over 50,000 VND
                if (order.FinalPrice >= 50000)
                {
                    // For first order, send the USERPROMO50 voucher
                    if (order.User.OrderCount == 1)
                    {
                        await SendFirstOrderVoucherEmailAsync(order.User);
                    }
                    // For orders over 1,000,000 VND, send a 10% discount voucher
                    else if (order.FinalPrice >= 1000000)
                    {
                        await SendPremiumVoucherEmailAsync(order.User);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending voucher emails for order {OrderId}", order.OrderID);
            }
        }

        private async Task SendFirstOrderVoucherEmailAsync(User user)
        {
            try
            {
                string voucherCode = "USERPROMO50";
                string token = await _voucherTokenService.GenerateVoucherTokenAsync(user.UserID, voucherCode, TimeSpan.FromDays(7));

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Could not generate voucher token for user {UserId}", user.UserID);
                    return;
                }

                string claimUrl = $"http://localhost:5246/voucher/claim?token={token}";
                var (subject, content) = _emailTemplateService.GetFirstOrderVoucherTemplate(user.Name, voucherCode, claimUrl);

                await _emailService.SendEmailAsync(user.Email, subject, content);
                _logger.LogInformation("First order voucher email sent to user {UserId}", user.UserID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending first order voucher email to user {UserId}", user.UserID);
            }
        }

        private async Task SendPremiumVoucherEmailAsync(User user)
        {
            try
            {
                string voucherCode = "PREMIUM10";
                string token = await _voucherTokenService.GenerateVoucherTokenAsync(user.UserID, voucherCode, TimeSpan.FromDays(14));

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Could not generate voucher token for user {UserId}", user.UserID);
                    return;
                }

                string claimUrl = $"http://localhost:5246/voucher/claim?token={token}";
                var (subject, content) = _emailTemplateService.GetPremiumVoucherTemplate(user.Name, voucherCode, claimUrl);

                await _emailService.SendEmailAsync(user.Email, subject, content);
                _logger.LogInformation("Premium voucher email sent to user {UserId}", user.UserID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending premium voucher email to user {UserId}", user.UserID);
            }
        }
    }
}