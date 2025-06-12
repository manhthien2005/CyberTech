using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using CyberTech.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;

namespace CyberTech.Services
{
    public class StockNotificationBackgroundService : BackgroundService
    {
        private readonly ILogger<StockNotificationBackgroundService> _logger;
        private readonly IServiceProvider _services;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30); // Kiểm tra mỗi 30 giây

        public StockNotificationBackgroundService(
            ILogger<StockNotificationBackgroundService> logger,
            IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stock Notification Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessStockNotifications(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing stock notifications");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task ProcessStockNotifications(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Processing stock notifications at: {time}", DateTimeOffset.Now);

            using (var scope = _services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                // Lấy các thông báo cần xử lý - không dùng AsNoTracking() để có thể cập nhật entities
                var notifications = await context.ProductStockNotifications
                    .Include(n => n.Product)
                    .Include(n => n.User)
                    .Where(n => n.IsActive && !n.IsNotified && n.Product.Stock > 0)
                    .Take(100) // Giới hạn số lượng xử lý mỗi lần để tránh quá tải
                    .ToListAsync(stoppingToken);

                if (notifications.Count == 0)
                {
                    return; // Không có thông báo cần xử lý
                }

                var sentCount = 0;

                foreach (var notification in notifications)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    try
                    {
                        // Gửi email thông báo
                        await emailService.SendStockNotificationEmailAsync(
                            notification.User.Email,
                            notification.User.Name,
                            notification.Product.Name,
                            $"http://localhost:5246/Product/ProductDetail/{notification.ProductID}",
                            notification.Product.GetEffectivePrice()
                        );

                        // Cập nhật trạng thái ngay sau khi gửi email thành công
                        notification.IsNotified = true;
                        notification.NotificationSentAt = DateTime.Now;
                        sentCount++;

                        _logger.LogWarning(
                            "Sent notification for Product {ProductId} to User {UserId}",
                            notification.ProductID,
                            notification.UserID
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to send notification for Product {ProductId} to User {UserId}",
                            notification.ProductID,
                            notification.UserID
                        );
                    }
                }

                if (sentCount > 0)
                {
                    // Lưu tất cả các thay đổi vào database
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogWarning("Successfully processed {Count} notifications", sentCount);

                    // Kiểm tra xác nhận các thông báo đã được đánh dấu
                    var unnotifiedCount = await context.ProductStockNotifications
                        .CountAsync(n => n.IsActive && !n.IsNotified &&
                                  notifications.Select(x => x.NotificationID).Contains(n.NotificationID) &&
                                  n.Product.Stock > 0, stoppingToken);

                    if (unnotifiedCount > 0)
                    {
                        _logger.LogError("Found {Count} notifications that were not properly marked as notified", unnotifiedCount);
                    }
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stock Notification Service is stopping.");
            await base.StopAsync(stoppingToken);
        }
    }
}