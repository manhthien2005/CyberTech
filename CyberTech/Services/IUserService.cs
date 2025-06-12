using CyberTech.Models;

namespace CyberTech.Services
{
    public interface IUserService
    {
        Task<bool> RegisterAsync(string name, string username, string email, string password, bool isExternal = false);
        Task<(bool Success, string ErrorMessage, User User)> AuthenticateAsync(string email, string password);
        Task<bool> GeneratePasswordResetTokenAsync(string email);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
        Task<bool> IsEmailTakenAsync(string email);
        Task<bool> IsUsernameTakenAsync(string username);
        Task<User> GetUserByEmailAsync(string email);
        Task<(bool Success, string ErrorMessage, User User)> ExternalLoginAsync(string provider, string providerId, string email, string name);
        Task<User> GetUserByProviderAsync(string provider, string providerId);
        Task<bool> UpdateProfileImageAsync(int userId, string imageUrl);
        Task<UserAuthMethod> GetUserAuthMethodAsync(int userId, string provider);
        Task<bool> AddAuthMethodAsync(int userId, string provider, string providerId);
        Task<bool> CanResetPasswordAsync(string email);
        Task<bool> HasPasswordAuthAsync(string email);
        Task<bool> IsExternalAccountAsync(string email);
        Task<bool> ValidatePasswordResetTokenAsync(string token);
        Task<bool> InvalidatePasswordResetTokenAsync(string token);
        Task<bool> IsPasswordResetTokenExpiredAsync(string token);
        Task<int> GetPasswordResetAttemptsAsync(string email);
        Task<bool> UpdateProfileAsync(string email, string name, string phone, byte? gender, DateTime? dateOfBirth);
        Task<int> GetAddressCountAsync(int userId);
        Task<bool> CanAddMoreAddressesAsync(int userId);
        Task<List<UserAddress>> GetUserAddressesAsync(int userId);
        Task<UserAddress> GetAddressByIdAsync(int addressId, int userId);
        Task<bool> AddAddressAsync(UserAddress address);
        Task<bool> UpdateAddressAsync(UserAddress address);
        Task<bool> DeleteAddressAsync(int addressId, int userId);
        Task<bool> SetPrimaryAddressAsync(int addressId, int userId);

        // Wishlist methods
        Task<List<WishlistItem>> GetWishlistItems(string userId);
        Task<bool> AddToWishlist(string userId, int productId);
        Task<bool> RemoveFromWishlist(string userId, int productId);
        Task<bool> IsInWishlist(string userId, int productId);

        // User Voucher methods
        Task<List<UserVoucher>> GetUserVouchersAsync(int userId);
        Task<bool> AssignVoucherToUserAsync(int userId, int voucherId);
        Task<bool> MarkVoucherAsUsedAsync(int userVoucherId, int orderId);
        Task<UserVoucher> GetUserVoucherByIdAsync(int userVoucherId);
        Task<bool> IsVoucherValidForUserAsync(int userId, int voucherId);
        Task<bool> SendVoucherNotificationAsync(int userId, int voucherId, string notificationMessage);

        // Email verification methods
        Task<bool> GenerateEmailVerificationTokenAsync(int userId);
        Task<bool> VerifyEmailAsync(string token);
        Task<bool> IsEmailVerificationTokenValidAsync(string token);
        Task<bool> ResendVerificationEmailAsync(int userId);
        Task<bool> IsEmailVerifiedAsync(int userId);
        Task<bool> MarkEmailAsVerifiedAsync(int userId);
    }
}