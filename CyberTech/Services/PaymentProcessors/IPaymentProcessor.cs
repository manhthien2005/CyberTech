using System.Threading.Tasks;
using CyberTech.Models;

namespace CyberTech.Services.PaymentProcessors
{
    public interface IPaymentProcessor
    {
        string PaymentMethod { get; }

        Task<PaymentResult> ProcessPaymentAsync(Order order, string ipAddress = null);
        Task<PaymentResult> HandleCallbackAsync(IDictionary<string, string> callbackData);
        Task<PaymentResult> ValidatePaymentAsync(Order order);
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string RedirectUrl { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentStatus { get; set; }

        public static PaymentResult Successful(string message = "Payment processed successfully", string redirectUrl = null)
        {
            return new PaymentResult
            {
                Success = true,
                Message = message,
                RedirectUrl = redirectUrl,
                PaymentStatus = "Completed",
                PaymentDate = DateTime.Now
            };
        }

        public static PaymentResult Failed(string message = "Payment processing failed")
        {
            return new PaymentResult
            {
                Success = false,
                Message = message,
                PaymentStatus = "Failed",
                PaymentDate = DateTime.Now
            };
        }

        public static PaymentResult Pending(string message = "Payment is pending", string redirectUrl = null)
        {
            return new PaymentResult
            {
                Success = true,
                Message = message,
                RedirectUrl = redirectUrl,
                PaymentStatus = "Pending",
                PaymentDate = DateTime.Now
            };
        }
    }
}