using CyberTech.Models;

namespace CyberTech.Services
{
    public interface ICartService
    {
        Task<Cart> GetCartAsync(int userId);
        Task<CartItem> GetCartItemAsync(int cartId, int productId);
        Task<bool> AddToCartAsync(int userId, int productId, int quantity);
        Task<bool> UpdateCartItemAsync(int userId, int productId, int quantity);
        Task<bool> RemoveFromCartAsync(int userId, int productId);
        Task<bool> ClearCartAsync(int userId);
        Task<int> GetCartItemCountAsync(int userId);
        Task<decimal> GetCartTotalAsync(int userId);
        Task<List<CartItem>> GetCartItemsAsync(int userId);
        Task<(bool Success, decimal DiscountAmount, string Message)> ApplyVoucherAsync(int userId, string voucherCode);
        Task<bool> RemoveVoucherAsync(int userId);
        Task<(bool Success, string Message, int? OrderId)> CheckoutAsync(int userId, int addressId, string paymentMethod, int? voucherId = null);
    }
}