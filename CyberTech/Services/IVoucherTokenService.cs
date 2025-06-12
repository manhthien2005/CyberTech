using CyberTech.Models;
using System;
using System.Threading.Tasks;

namespace CyberTech.Services
{
    public interface IVoucherTokenService
    {
        Task<string> GenerateVoucherTokenAsync(int userId, string voucherCode, TimeSpan? expiration = null);
        Task<(bool Success, string Message, VoucherToken Token)> ValidateTokenAsync(string token);
        Task<bool> MarkTokenAsUsedAsync(string token);
        Task<bool> IsTokenValidAsync(string token);
    }
}