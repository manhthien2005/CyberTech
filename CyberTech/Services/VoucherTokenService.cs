using CyberTech.Data;
using CyberTech.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CyberTech.Services
{
    public class VoucherTokenService : IVoucherTokenService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VoucherTokenService> _logger;

        public VoucherTokenService(ApplicationDbContext context, ILogger<VoucherTokenService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GenerateVoucherTokenAsync(int userId, string voucherCode, TimeSpan? expiration = null)
        {
            try
            {
                // Check if user already has an unused voucher with this code
                var existingVoucher = await _context.UserVouchers
                    .Include(uv => uv.Voucher)
                    .FirstOrDefaultAsync(uv => uv.UserID == userId && uv.Voucher.Code == voucherCode && !uv.IsUsed);

                if (existingVoucher != null)
                {
                    _logger.LogInformation("User {UserId} already has an unused voucher with code {VoucherCode}", userId, voucherCode);
                    return null;
                }

                // Default expiration is 7 days if not specified
                if (!expiration.HasValue)
                {
                    expiration = TimeSpan.FromDays(7);
                }

                // Generate a unique token
                string token = GenerateUniqueToken();

                // Create a new voucher token
                var voucherToken = new VoucherToken
                {
                    UserID = userId,
                    Token = token,
                    CreatedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.Add(expiration.Value),
                    IsUsed = false,
                    VoucherCode = voucherCode
                };

                _context.VoucherTokens.Add(voucherToken);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Generated voucher token {Token} for user {UserId} with voucher code {VoucherCode}", token, userId, voucherCode);
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating voucher token for user {UserId}", userId);
                throw;
            }
        }

        public async Task<(bool Success, string Message, VoucherToken Token)> ValidateTokenAsync(string token)
        {
            try
            {
                var voucherToken = await _context.VoucherTokens
                    .FirstOrDefaultAsync(vt => vt.Token == token);

                if (voucherToken == null)
                {
                    return (false, "Token không tồn tại", null);
                }

                if (voucherToken.IsUsed)
                {
                    return (false, "Token đã được sử dụng", voucherToken);
                }

                if (voucherToken.ExpiresAt < DateTime.Now)
                {
                    return (false, "Token đã hết hạn", voucherToken);
                }

                return (true, "Token hợp lệ", voucherToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token {Token}", token);
                return (false, "Có lỗi xảy ra khi xác thực token", null);
            }
        }

        public async Task<bool> MarkTokenAsUsedAsync(string token)
        {
            try
            {
                var voucherToken = await _context.VoucherTokens
                    .FirstOrDefaultAsync(vt => vt.Token == token);

                if (voucherToken == null)
                {
                    return false;
                }

                voucherToken.IsUsed = true;
                voucherToken.UsedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked token {Token} as used", token);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking token {Token} as used", token);
                return false;
            }
        }

        public async Task<bool> IsTokenValidAsync(string token)
        {
            try
            {
                var voucherToken = await _context.VoucherTokens
                    .FirstOrDefaultAsync(vt => vt.Token == token);

                return voucherToken != null && !voucherToken.IsUsed && voucherToken.ExpiresAt >= DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if token {Token} is valid", token);
                return false;
            }
        }

        private string GenerateUniqueToken()
        {
            // Generate a random token using cryptographically secure random number generator
            using var rng = RandomNumberGenerator.Create();
            var tokenBytes = new byte[32]; // 256 bits
            rng.GetBytes(tokenBytes);

            // Convert to base64 string and remove special characters
            string token = Convert.ToBase64String(tokenBytes)
                .Replace("/", "_")
                .Replace("+", "-")
                .Replace("=", "");

            return token;
        }
    }
}