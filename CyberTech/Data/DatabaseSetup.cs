using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CyberTech.Data
{
    public static class DatabaseSetup
    {
        public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider, ILogger logger)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                logger.LogInformation("Checking database connection...");
                
                // Kiểm tra kết nối database
                var canConnect = await dbContext.Database.CanConnectAsync();
                
                if (!canConnect)
                {
                    logger.LogError("Cannot connect to database. Please check connection string.");
                    return;
                }
                
                logger.LogInformation("Database connection successful.");
                
                // Kiểm tra nếu cần migrate
                if ((await dbContext.Database.GetPendingMigrationsAsync()).Any())
                {
                    logger.LogWarning("Database has pending migrations. Consider running migrations.");
                }
                
                // Kiểm tra nếu database có dữ liệu
                var userCount = await dbContext.Users.CountAsync();
                var productCount = await dbContext.Products.CountAsync();
                
                logger.LogInformation("Database contains {UserCount} users and {ProductCount} products", 
                    userCount, productCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database");
            }
        }
    }
} 