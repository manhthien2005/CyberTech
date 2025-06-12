using System.Threading.Tasks;

namespace CyberTech.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string email, string resetUrl);
        Task SendEmailAsync(string email, string subject, string htmlContent);
        Task SendRankUpgradeEmailAsync(string email, string userName, string oldRankName, string newRankName, decimal newDiscountPercent);
        Task SendStockNotificationEmailAsync(string email, string userName, string productName, string productUrl, decimal price);
    }
}