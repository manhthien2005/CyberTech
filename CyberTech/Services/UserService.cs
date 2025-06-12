using BCrypt.Net;
using CyberTech.Data;
using CyberTech.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace CyberTech.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, IEmailService emailService, ILogger<UserService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _logger.LogInformation("Khởi tạo UserService");
        }

        public async Task<bool> RegisterAsync(string name, string username, string email, string password, bool isExternal = false)
        {
            _logger.LogDebug("Bắt đầu đăng ký: {Email}", email);
            try
            {
                if (await IsEmailTakenAsync(email))
                {
                    _logger.LogWarning("Email đã tồn tại: {Email}", email);
                    return false;
                }

                if (await IsUsernameTakenAsync(username))
                {
                    _logger.LogWarning("Tên đăng nhập đã tồn tại: {Username}", username);
                    return false;
                }

                var existingUser = await GetUserByEmailAsync(email);
                if (existingUser != null)
                {
                    var isExternalAccount = await IsExternalAccountAsync(email);
                    if (isExternalAccount)
                    {
                        _logger.LogWarning("Email đã được sử dụng bởi tài khoản ngoài: {Email}", email);
                        return false;
                    }
                }

                var user = new User
                {
                    Name = name,
                    Username = username,
                    Email = email,
                    Role = "Customer",
                    UserStatus = "Active",
                    CreatedAt = DateTime.Now,
                    EmailVerified = isExternal, // Set to true if external account
                    RankId = 1,
                    TotalSpent = 0,
                    OrderCount = 0,
                    Gender = null,
                    DateOfBirth = null
                };

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
                var authMethod = new UserAuthMethod
                {
                    User = user,
                    AuthType = "Password",
                    AuthKey = hashedPassword,
                    CreatedAt = DateTime.Now
                };

                await _context.Users.AddAsync(user);
                await _context.UserAuthMethods.AddAsync(authMethod);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đăng ký thành công: {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi đăng ký {Email}: {Message}", email, ex.Message);
                return false;
            }
        }

        public async Task<(bool Success, string ErrorMessage, User User)> AuthenticateAsync(string email, string password)
        {
            _logger.LogDebug("Bắt đầu đăng nhập: {Email}", email);
            try
            {
                var user = await GetUserByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Tài khoản không tồn tại: {Email}", email);
                    return (false, "Tài khoản không tồn tại", null);
                }

                if (user.UserStatus != "Active")
                {
                    _logger.LogWarning("Tài khoản không hoạt động: {Email}", email);
                    return (false, "Tài khoản bị khóa hoặc bị đình chỉ", null);
                }

                var isExternal = await IsExternalAccountAsync(email);
                if (isExternal)
                {
                    _logger.LogWarning("Tài khoản ngoài không thể đăng nhập bằng mật khẩu: {Email}", email);
                    return (false, "Tài khoản này chỉ có thể đăng nhập bằng Google hoặc Facebook", null);
                }

                var hasPassword = await HasPasswordAuthAsync(email);
                if (!hasPassword)
                {
                    _logger.LogWarning("Tài khoản không có phương thức xác thực bằng mật khẩu: {Email}", email);
                    return (false, "Tài khoản này không có phương thức xác thực bằng mật khẩu", null);
                }

                var authMethod = await _context.UserAuthMethods
                    .AsNoTracking()
                    .Select(uam => new
                    {
                        uam.UserID,
                        uam.AuthType,
                        uam.AuthKey
                    })
                    .Where(uam => uam.UserID == user.UserID &&
                                 uam.AuthType == "Password" &&
                                 uam.AuthKey != null)
                    .FirstOrDefaultAsync();

                if (authMethod == null)
                {
                    _logger.LogWarning("Không tìm thấy phương thức xác thực mật khẩu: {Email}", email);
                    return (false, "Không tìm thấy phương thức xác thực mật khẩu", null);
                }

                if (!BCrypt.Net.BCrypt.Verify(password, authMethod.AuthKey))
                {
                    _logger.LogWarning("Mật khẩu không đúng: {Email}", email);
                    return (false, "Mật khẩu không đúng", null);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Đăng nhập thành công: {Email}", email);
                return (true, null, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi đăng nhập {Email}: {Message}", email, ex.Message);
                return (false, "Đã xảy ra lỗi khi đăng nhập", null);
            }
        }

        public async Task<(bool Success, string ErrorMessage, User User)> ExternalLoginAsync(string provider, string providerId, string email, string name)
        {
            _logger.LogDebug("Bắt đầu đăng nhập ngoài với {Provider} cho {Email}", provider, email);
            try
            {
                // Kiểm tra xem người dùng đã tồn tại với provider này chưa
                var existingUser = await GetUserByProviderAsync(provider, providerId);
                if (existingUser != null)
                {
                    _logger.LogInformation("Người dùng đã tồn tại với {Provider}: {Email}", provider, email);
                    await _context.SaveChangesAsync();
                    return (true, null, existingUser);
                }

                // Nếu không tìm thấy theo provider, kiểm tra theo email
                var userByEmail = await GetUserByEmailAsync(email);
                if (userByEmail != null)
                {
                    _logger.LogInformation("Tìm thấy người dùng theo email: {Email}", email);

                    // Kiểm tra xem người dùng đã có phương thức xác thực này chưa
                    var existingAuthMethod = await GetUserAuthMethodAsync(userByEmail.UserID, provider);
                    if (existingAuthMethod == null)
                    {
                        // Thêm phương thức xác thực mới
                        var success = await AddAuthMethodAsync(userByEmail.UserID, provider, providerId);
                        if (!success)
                        {
                            _logger.LogWarning("Không thể thêm phương thức xác thực {Provider} cho {Email}", provider, email);
                            return (false, "Không thể thêm phương thức xác thực", null);
                        }
                    }

                    await _context.SaveChangesAsync();
                    return (true, null, userByEmail);
                }

                // Tạo người dùng mới
                _logger.LogInformation("Tạo người dùng mới cho {Email}", email);
                var username = GenerateUniqueUsername(email);
                var newUser = new User
                {
                    Name = name,
                    Username = username,
                    Email = email,
                    Role = "Customer",
                    UserStatus = "Active",
                    CreatedAt = DateTime.Now,
                    EmailVerified = true, // External accounts are automatically verified
                    RankId = 1,
                    TotalSpent = 0,
                    OrderCount = 0,
                    Gender = null,
                    DateOfBirth = null
                };

                var newAuthMethod = new UserAuthMethod
                {
                    User = newUser,
                    AuthType = provider,
                    AuthKey = providerId,
                    CreatedAt = DateTime.Now
                };

                await _context.Users.AddAsync(newUser);
                await _context.UserAuthMethods.AddAsync(newAuthMethod);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đã tạo người dùng mới và phương thức xác thực {Provider} cho {Email}", provider, email);
                return (true, null, newUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong ExternalLoginAsync: {Message}", ex.Message);
                return (false, "Đã xảy ra lỗi khi xử lý đăng nhập ngoài", null);
            }
        }

        public async Task<User> GetUserByProviderAsync(string provider, string providerId)
        {
            try
            {
                var authMethod = await _context.UserAuthMethods
                    .Include(uam => uam.User)
                    .Where(uam => uam.AuthType == provider && uam.AuthKey == providerId)
                    .Select(uam => new
                    {
                        User = new User
                        {
                            UserID = uam.User.UserID,
                            Name = uam.User.Name ?? string.Empty,
                            Username = uam.User.Username ?? string.Empty,
                            Email = uam.User.Email ?? string.Empty,
                            ProfileImageURL = uam.User.ProfileImageURL,
                            Role = uam.User.Role,
                            Phone = uam.User.Phone,
                            Salary = uam.User.Salary,
                            TotalSpent = uam.User.TotalSpent,
                            OrderCount = uam.User.OrderCount,
                            RankId = uam.User.RankId,
                            EmailVerified = uam.User.EmailVerified,
                            UserStatus = uam.User.UserStatus,
                            CreatedAt = uam.User.CreatedAt
                        }
                    })
                    .FirstOrDefaultAsync();

                return authMethod?.User;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong GetUserByProviderAsync: {Message}", ex.Message);
                return null;
            }
        }

        public async Task<bool> GeneratePasswordResetTokenAsync(string email)
        {
            _logger.LogDebug("Bắt đầu tạo token đặt lại mật khẩu: {Email}", email);
            try
            {
                var user = await GetUserByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Không tìm thấy tài khoản: {Email}", email);
                    return false;
                }

                if (!await CanResetPasswordAsync(email))
                {
                    _logger.LogWarning("Tài khoản không thể đặt lại mật khẩu: {Email}", email);
                    return false;
                }

                // Kiểm tra số lần yêu cầu trong 1 giờ
                var attempts = await GetPasswordResetAttemptsAsync(email);
                if (attempts >= 3)
                {
                    _logger.LogWarning("Quá nhiều yêu cầu đặt lại mật khẩu trong 1 giờ: {Email}", email);
                    return false;
                }

                // Xóa các token cũ
                var oldTokens = await _context.PasswordResetTokens
                    .Where(t => t.UserID == user.UserID)
                    .ToListAsync();
                if (oldTokens.Any())
                {
                    _context.PasswordResetTokens.RemoveRange(oldTokens);
                    await _context.SaveChangesAsync();
                    _logger.LogDebug("Đã xóa {Count} token cũ cho user {UserId}", oldTokens.Count, user.UserID);
                }

                var token = GenerateSecureToken();
                var resetToken = new PasswordResetToken
                {
                    UserID = user.UserID,
                    Token = token,
                    CreatedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddMinutes(15),
                    Used = false
                };

                _context.PasswordResetTokens.Add(resetToken);
                await _context.SaveChangesAsync();
                _logger.LogDebug("Đã tạo token mới cho user {UserId}", user.UserID);

                var baseUrl = "http://localhost:5246";
                var resetUrl = $"{baseUrl}/Account/ResetPassword?token={token}";

                try
                {
                    await _emailService.SendPasswordResetEmailAsync(email, resetUrl);
                    _logger.LogInformation("Đã gửi email đặt lại mật khẩu thành công: {Email}", email);
                    return true;
                }
                catch (Exception ex)
                {
                    // Nếu gửi email thất bại, xóa token
                    _context.PasswordResetTokens.Remove(resetToken);
                    await _context.SaveChangesAsync();
                    _logger.LogError(ex, "Lỗi gửi email đặt lại mật khẩu {Email}: {Message}", email, ex.Message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo token đặt lại mật khẩu {Email}: {Message}", email, ex.Message);
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            _logger.LogDebug("Bắt đầu đặt lại mật khẩu với token");
            try
            {
                if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
                {
                    _logger.LogWarning("Mật khẩu mới không hợp lệ");
                    return false;
                }

                var resetToken = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t => t.Token == token && !t.Used && t.ExpiresAt > DateTime.Now);

                if (resetToken == null)
                {
                    _logger.LogWarning("Token không hợp lệ hoặc đã hết hạn");
                    return false;
                }

                var authMethod = await _context.UserAuthMethods
                    .FirstOrDefaultAsync(uam => uam.UserID == resetToken.UserID && uam.AuthType == "Password");

                if (authMethod == null)
                {
                    _logger.LogWarning("Không tìm thấy phương thức xác thực mật khẩu: {UserId}", resetToken.UserID);
                    return false;
                }

                // Kiểm tra mật khẩu mới không trùng với mật khẩu cũ
                if (BCrypt.Net.BCrypt.Verify(newPassword, authMethod.AuthKey))
                {
                    _logger.LogWarning("Mật khẩu mới không được trùng với mật khẩu cũ: {UserId}", resetToken.UserID);
                    return false;
                }

                authMethod.AuthKey = BCrypt.Net.BCrypt.HashPassword(newPassword);
                resetToken.Used = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đặt lại mật khẩu thành công: {UserId}", resetToken.UserID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi đặt lại mật khẩu: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<bool> ValidatePasswordResetTokenAsync(string token)
        {
            try
            {
                var resetToken = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t => t.Token == token && !t.Used && t.ExpiresAt > DateTime.Now);

                if (resetToken == null)
                {
                    _logger.LogWarning("Token không hợp lệ hoặc đã hết hạn");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kiểm tra token: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<bool> InvalidatePasswordResetTokenAsync(string token)
        {
            try
            {
                var resetToken = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t => t.Token == token);

                if (resetToken != null)
                {
                    resetToken.Used = true;
                    await _context.SaveChangesAsync();
                    _logger.LogDebug("Đã vô hiệu hóa token: {Token}", token);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi vô hiệu hóa token: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<bool> IsPasswordResetTokenExpiredAsync(string token)
        {
            try
            {
                var resetToken = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t => t.Token == token);

                if (resetToken == null)
                {
                    return true;
                }

                return resetToken.ExpiresAt <= DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kiểm tra hết hạn token: {Message}", ex.Message);
                return true;
            }
        }

        public async Task<int> GetPasswordResetAttemptsAsync(string email)
        {
            try
            {
                var user = await GetUserByEmailAsync(email);
                if (user == null)
                {
                    return 0;
                }

                return await _context.PasswordResetTokens
                    .CountAsync(t => t.UserID == user.UserID &&
                                   t.CreatedAt >= DateTime.Now.AddHours(-1));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi đếm số lần yêu cầu đặt lại mật khẩu: {Message}", ex.Message);
                return 0;
            }
        }

        public async Task<bool> IsEmailTakenAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Rank)
                    .Where(u => u.Email == email)
                    .Select(u => new User
                    {
                        UserID = u.UserID,
                        Name = u.Name ?? string.Empty,
                        Username = u.Username ?? string.Empty,
                        Email = u.Email ?? string.Empty,
                        ProfileImageURL = u.ProfileImageURL ?? string.Empty,
                        Role = u.Role,
                        Phone = u.Phone,
                        Salary = u.Salary,
                        TotalSpent = u.TotalSpent,
                        OrderCount = u.OrderCount,
                        RankId = u.RankId,
                        Rank = u.Rank,
                        EmailVerified = u.EmailVerified,
                        UserStatus = u.UserStatus,
                        CreatedAt = u.CreatedAt,
                        Gender = u.Gender,
                        DateOfBirth = u.DateOfBirth,
                        AuthMethods = u.AuthMethods
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserByEmailAsync: {Message}", ex.Message);
                return null;
            }
        }

        public async Task<bool> IncrementLoginAttemptsAsync(string email)
        {
            return true;
        }

        public async Task<int> GetLoginAttemptsAsync(string email)
        {
            return 0;
        }

        public async Task ResetLoginAttemptsAsync(string email)
        {
        }

        private string GenerateSecureToken()
        {
            _logger.LogDebug("Tạo token bảo mật");
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        private string GenerateUniqueUsername(string email)
        {
            _logger.LogDebug("Tạo tên đăng nhập cho: {Email}", email);
            var baseUsername = email.Split('@')[0].Replace(".", "");
            var username = baseUsername;
            var counter = 1;
            while (_context.Users.Any(u => u.Username == username))
            {
                username = $"{baseUsername}{counter++}";
            }
            _logger.LogDebug("Đã tạo tên đăng nhập: {Username}", username);
            return username;
        }
        public async Task<bool> UpdateProfileImageAsync(int userId, string imageUrl)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                if (string.IsNullOrEmpty(user.ProfileImageURL))
                {
                    user.ProfileImageURL = imageUrl;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile image for user {UserId}", userId);
                return false;
            }
        }

        public async Task<UserAuthMethod> GetUserAuthMethodAsync(int userId, string provider)
        {
            try
            {
                return await _context.UserAuthMethods
                    .AsNoTracking()
                    .Where(uam => uam.UserID == userId && uam.AuthType == provider)
                    .Select(uam => new UserAuthMethod
                    {
                        ID = uam.ID,
                        UserID = uam.UserID,
                        AuthType = uam.AuthType,
                        AuthKey = uam.AuthKey,
                        CreatedAt = uam.CreatedAt
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong GetUserAuthMethodAsync: {Message}", ex.Message);
                return null;
            }
        }

        public async Task<bool> AddAuthMethodAsync(int userId, string provider, string providerId)
        {
            try
            {
                var existingAuthMethod = await _context.UserAuthMethods
                    .AsNoTracking()
                    .Select(uam => new { uam.UserID, uam.AuthType, uam.AuthKey })
                    .FirstOrDefaultAsync(uam => uam.UserID == userId &&
                                               uam.AuthType == provider &&
                                               uam.AuthKey == providerId);

                if (existingAuthMethod != null)
                {
                    return true;
                }

                var newAuthMethod = new UserAuthMethod
                {
                    UserID = userId,
                    AuthType = provider,
                    AuthKey = providerId,
                    CreatedAt = DateTime.Now
                };

                await _context.UserAuthMethods.AddAsync(newAuthMethod);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong AddAuthMethodAsync: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<bool> CanResetPasswordAsync(string email)
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null) return false;

            // Kiểm tra xem tài khoản có phải là tài khoản ngoài không
            var isExternal = await IsExternalAccountAsync(email);
            if (isExternal) return false;

            // Kiểm tra xem tài khoản có phương thức xác thực bằng mật khẩu không
            var hasPassword = await HasPasswordAuthAsync(email);
            if (!hasPassword) return false;

            return true;
        }

        public async Task<bool> HasPasswordAuthAsync(string email)
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null) return false;

            return await _context.UserAuthMethods
                .AnyAsync(uam => uam.UserID == user.UserID && uam.AuthType == "Password");
        }

        public async Task<bool> IsExternalAccountAsync(string email)
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null) return false;

            var authMethods = await _context.UserAuthMethods
                .Where(uam => uam.UserID == user.UserID)
                .Select(uam => uam.AuthType)
                .ToListAsync();

            return authMethods.Any(type => type == "Google" || type == "Facebook");
        }

        public async Task<bool> RemoveAuthMethodAsync(int userId, string provider)
        {
            try
            {
                var authMethod = await _context.UserAuthMethods
                    .FirstOrDefaultAsync(uam => uam.UserID == userId && uam.AuthType == provider);

                if (authMethod == null)
                {
                    return false;
                }

                _context.UserAuthMethods.Remove(authMethod);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong RemoveAuthMethodAsync: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<List<UserAuthMethod>> GetUserAuthMethodsAsync(int userId)
        {
            try
            {
                return await _context.UserAuthMethods
                    .AsNoTracking()
                    .Where(uam => uam.UserID == userId)
                    .Select(uam => new UserAuthMethod
                    {
                        ID = uam.ID,
                        UserID = uam.UserID,
                        AuthType = uam.AuthType,
                        AuthKey = uam.AuthKey,
                        CreatedAt = uam.CreatedAt
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong GetUserAuthMethodsAsync: {Message}", ex.Message);
                return new List<UserAuthMethod>();
            }
        }

        public async Task<bool> HasAuthMethodAsync(int userId, string provider)
        {
            try
            {
                return await _context.UserAuthMethods
                    .AsNoTracking()
                    .AnyAsync(uam => uam.UserID == userId && uam.AuthType == provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong HasAuthMethodAsync: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<bool> UpdateProfileAsync(string email, string name, string phone, byte? gender, DateTime? dateOfBirth)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null) return false;

                user.Name = name;
                user.Phone = phone;
                user.Gender = gender;
                user.DateOfBirth = dateOfBirth;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Cập nhật thông tin người dùng thành công: {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật thông tin người dùng: {Email}", email);
                return false;
            }
        }

        public async Task<int> GetAddressCountAsync(int userId)
        {
            try
            {
                return await _context.UserAddresses
                    .CountAsync(a => a.UserID == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi đếm số địa chỉ của người dùng {UserId}", userId);
                return 0;
            }
        }

        public async Task<bool> CanAddMoreAddressesAsync(int userId)
        {
            try
            {
                var addressCount = await GetAddressCountAsync(userId);
                return addressCount < 6;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kiểm tra khả năng thêm địa chỉ của người dùng {UserId}", userId);
                return false;
            }
        }

        public async Task<List<UserAddress>> GetUserAddressesAsync(int userId)
        {
            try
            {
                return await _context.UserAddresses
                    .Where(a => a.UserID == userId)
                    .OrderByDescending(a => a.IsPrimary)
                    .ThenByDescending(a => a.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lấy danh sách địa chỉ của người dùng {UserId}", userId);
                return new List<UserAddress>();
            }
        }

        public async Task<UserAddress> GetAddressByIdAsync(int addressId, int userId)
        {
            try
            {
                return await _context.UserAddresses
                    .FirstOrDefaultAsync(a => a.AddressID == addressId && a.UserID == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lấy thông tin địa chỉ {AddressId} của người dùng {UserId}", addressId, userId);
                return null;
            }
        }

        public async Task<bool> AddAddressAsync(UserAddress address)
        {
            try
            {
                if (!await CanAddMoreAddressesAsync(address.UserID))
                {
                    return false;
                }

                address.CreatedAt = DateTime.Now;

                if (address.IsPrimary)
                {
                    var existingPrimaryAddresses = await _context.UserAddresses
                        .Where(a => a.UserID == address.UserID && a.IsPrimary)
                        .ToListAsync();

                    foreach (var existingAddress in existingPrimaryAddresses)
                    {
                        existingAddress.IsPrimary = false;
                    }
                }

                await _context.UserAddresses.AddAsync(address);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi thêm địa chỉ cho người dùng {UserId}", address.UserID);
                return false;
            }
        }

        public async Task<bool> UpdateAddressAsync(UserAddress address)
        {
            try
            {
                var existingAddress = await _context.UserAddresses
                    .FirstOrDefaultAsync(a => a.AddressID == address.AddressID && a.UserID == address.UserID);

                if (existingAddress == null)
                {
                    return false;
                }

                existingAddress.RecipientName = address.RecipientName;
                existingAddress.Phone = address.Phone;
                existingAddress.AddressLine = address.AddressLine;
                existingAddress.City = address.City;
                existingAddress.District = address.District;
                existingAddress.Ward = address.Ward;
                existingAddress.IsPrimary = address.IsPrimary;

                if (address.IsPrimary)
                {
                    var otherPrimaryAddresses = await _context.UserAddresses
                        .Where(a => a.UserID == address.UserID && a.AddressID != address.AddressID && a.IsPrimary)
                        .ToListAsync();

                    foreach (var otherAddress in otherPrimaryAddresses)
                    {
                        otherAddress.IsPrimary = false;
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật địa chỉ {AddressId} của người dùng {UserId}", address.AddressID, address.UserID);
                return false;
            }
        }

        public async Task<bool> DeleteAddressAsync(int addressId, int userId)
        {
            try
            {
                var address = await _context.UserAddresses
                    .FirstOrDefaultAsync(a => a.AddressID == addressId && a.UserID == userId);

                if (address == null)
                {
                    return false;
                }

                if (address.IsPrimary)
                {
                    return false;
                }

                _context.UserAddresses.Remove(address);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xóa địa chỉ {AddressId} của người dùng {UserId}", addressId, userId);
                return false;
            }
        }

        public async Task<bool> SetPrimaryAddressAsync(int addressId, int userId)
        {
            try
            {
                var address = await _context.UserAddresses
                    .FirstOrDefaultAsync(a => a.AddressID == addressId && a.UserID == userId);

                if (address == null)
                {
                    _logger.LogWarning($"Address {addressId} not found for user {userId}");
                    return false;
                }

                // Nếu địa chỉ đã là mặc định thì không cần thay đổi
                if (address.IsPrimary)
                {
                    return true;
                }

                // Cập nhật tất cả địa chỉ của user thành không mặc định
                var userAddresses = await _context.UserAddresses
                    .Where(a => a.UserID == userId)
                    .ToListAsync();

                foreach (var addr in userAddresses)
                {
                    addr.IsPrimary = false;
                }

                // Đặt địa chỉ được chọn làm mặc định
                address.IsPrimary = true;

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Set address {addressId} as primary for user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting primary address {addressId} for user {userId}");
                return false;
            }
        }

        public async Task<bool> DeleteAddress(int addressId)
        {
            var address = await _context.UserAddresses.FindAsync(addressId);
            if (address == null) return false;

            _context.UserAddresses.Remove(address);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<WishlistItem>> GetWishlistItems(string userId)
        {
            return await _context.WishlistItems
                .Include(w => w.Product)
                    .ThenInclude(p => p.ProductImages)
                .Where(w => w.UserID.ToString() == userId)
                .OrderByDescending(w => w.AddedDate)
                .ToListAsync();
        }

        public async Task<bool> AddToWishlist(string userId, int productId)
        {
            var existingItem = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserID.ToString() == userId && w.ProductID == productId);

            if (existingItem != null) return false;

            var wishlistItem = new WishlistItem
            {
                UserID = int.Parse(userId),
                ProductID = productId,
                AddedDate = DateTime.Now
            };

            _context.WishlistItems.Add(wishlistItem);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveFromWishlist(string userId, int productId)
        {
            var wishlistItem = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserID.ToString() == userId && w.ProductID == productId);

            if (wishlistItem == null) return false;

            _context.WishlistItems.Remove(wishlistItem);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsInWishlist(string userId, int productId)
        {
            if (string.IsNullOrEmpty(userId)) return false;

            return await _context.WishlistItems
                .AnyAsync(wi => wi.UserID == int.Parse(userId) && wi.ProductID == productId);
        }

        public async Task<List<UserVoucher>> GetUserVouchersAsync(int userId)
        {
            try
            {
                return await _context.UserVouchers
                    .Include(uv => uv.Voucher)
                    .Where(uv => uv.UserID == userId && !uv.IsUsed && uv.Voucher.IsActive && uv.Voucher.ValidTo > DateTime.Now)
                    .OrderBy(uv => uv.Voucher.ValidTo)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vouchers for user {UserId}", userId);
                return new List<UserVoucher>();
            }
        }

        public async Task<bool> AssignVoucherToUserAsync(int userId, int voucherId)
        {
            try
            {
                // Check if user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found when assigning voucher", userId);
                    return false;
                }

                // Check if voucher exists and is valid
                var voucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.VoucherID == voucherId && v.IsActive && v.ValidTo > DateTime.Now);
                if (voucher == null)
                {
                    _logger.LogWarning("Voucher {VoucherId} not found or not valid", voucherId);
                    return false;
                }

                // Check if voucher has available quantity
                if (voucher.QuantityAvailable.HasValue && voucher.QuantityAvailable <= 0)
                {
                    _logger.LogWarning("Voucher {VoucherId} has no available quantity", voucherId);
                    return false;
                }

                // Create new user voucher - allowing multiple vouchers if previous ones are used
                var userVoucher = new UserVoucher
                {
                    UserID = userId,
                    VoucherID = voucherId,
                    AssignedDate = DateTime.Now,
                    IsUsed = false
                };

                // Decrease voucher quantity if applicable
                if (voucher.QuantityAvailable.HasValue)
                {
                    voucher.QuantityAvailable--;
                }

                await _context.UserVouchers.AddAsync(userVoucher);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Voucher {VoucherId} assigned to user {UserId}", voucherId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning voucher {VoucherId} to user {UserId}", voucherId, userId);
                return false;
            }
        }

        public async Task<bool> MarkVoucherAsUsedAsync(int userVoucherId, int orderId)
        {
            try
            {
                var userVoucher = await _context.UserVouchers.FindAsync(userVoucherId);
                if (userVoucher == null)
                {
                    _logger.LogWarning("UserVoucher {UserVoucherId} not found", userVoucherId);
                    return false;
                }

                if (userVoucher.IsUsed)
                {
                    _logger.LogWarning("UserVoucher {UserVoucherId} is already used", userVoucherId);
                    return false;
                }

                userVoucher.IsUsed = true;
                userVoucher.UsedDate = DateTime.Now;
                userVoucher.OrderID = orderId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("UserVoucher {UserVoucherId} marked as used for order {OrderId}", userVoucherId, orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking UserVoucher {UserVoucherId} as used", userVoucherId);
                return false;
            }
        }

        public async Task<UserVoucher> GetUserVoucherByIdAsync(int userVoucherId)
        {
            try
            {
                return await _context.UserVouchers
                    .Include(uv => uv.Voucher)
                    .FirstOrDefaultAsync(uv => uv.UserVoucherID == userVoucherId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting UserVoucher {UserVoucherId}", userVoucherId);
                return null;
            }
        }

        public async Task<bool> IsVoucherValidForUserAsync(int userId, int voucherId)
        {
            try
            {
                var userVoucher = await _context.UserVouchers
                    .Include(uv => uv.Voucher)
                    .FirstOrDefaultAsync(uv => uv.UserID == userId &&
                                              uv.VoucherID == voucherId &&
                                              !uv.IsUsed &&
                                              uv.Voucher.IsActive &&
                                              uv.Voucher.ValidTo > DateTime.Now);

                return userVoucher != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if voucher {VoucherId} is valid for user {UserId}", voucherId, userId);
                return false;
            }
        }

        public async Task<bool> SendVoucherNotificationAsync(int userId, int voucherId, string notificationMessage)
        {
            try
            {
                // Get the user
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found when sending voucher notification", userId);
                    return false;
                }

                // Get the voucher
                var voucher = await _context.Vouchers.FindAsync(voucherId);
                if (voucher == null)
                {
                    _logger.LogWarning("Voucher {VoucherId} not found when sending notification", voucherId);
                    return false;
                }

                // Check if user already has this voucher
                var existingUserVoucher = await _context.UserVouchers
                    .FirstOrDefaultAsync(uv => uv.UserID == userId && uv.VoucherID == voucherId);

                if (existingUserVoucher != null)
                {
                    _logger.LogWarning("User {UserId} already has voucher {VoucherId}", userId, voucherId);
                    return false;
                }

                // Create claim URL for the voucher
                var baseUrl = "http://localhost:5246"; // Should be configured in appsettings
                var claimUrl = $"{baseUrl}/Account/ClaimVoucher?code={voucher.Code}&userId={userId}";

                // Prepare email content
                string emailSubject = $"Mã giảm giá mới từ CyberTech: {voucher.Code}";
                string emailContent = $@"
                <h2>Xin chào {user.Name},</h2>
                <p>{notificationMessage}</p>
                <div style='margin: 20px 0; padding: 15px; border: 1px dashed #ddd; background-color: #f8f9fa; border-radius: 5px;'>
                    <h3 style='color: #dc3545; margin: 0 0 10px 0;'>Mã giảm giá: {voucher.Code}</h3>
                    <p><strong>Giảm giá:</strong> {(voucher.DiscountType == "PERCENT" ? $"{voucher.DiscountValue}%" : $"{voucher.DiscountValue:N0}đ")}</p>
                    <p><strong>Áp dụng cho:</strong> {(voucher.AppliesTo == "Order" ? "Toàn đơn hàng" : "Sản phẩm")}</p>
                    <p><strong>Có hiệu lực đến:</strong> {voucher.ValidTo:dd/MM/yyyy}</p>
                    <p>{voucher.Description}</p>
                </div>
                <p>Nhấn vào nút bên dưới để thêm mã giảm giá vào tài khoản của bạn:</p>
                <div style='text-align: center; margin: 25px 0;'>
                    <a href='{claimUrl}' style='background-color: #0d6efd; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Nhận mã giảm giá</a>
                </div>
                <p>Hoặc bạn có thể nhập mã <strong>{voucher.Code}</strong> trực tiếp trong kho voucher của bạn.</p>
                <p>Lưu ý: Mã giảm giá chỉ có thể sử dụng một lần duy nhất và có hiệu lực đến {voucher.ValidTo:dd/MM/yyyy}.</p>
                <p>Cảm ơn bạn đã chọn CyberTech!</p>
                ";

                // Send email
                await _emailService.SendEmailAsync(user.Email, emailSubject, emailContent);

                _logger.LogInformation("Voucher notification email sent to user {UserId} for voucher {VoucherId}", userId, voucherId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending voucher notification for voucher {VoucherId} to user {UserId}", voucherId, userId);
                return false;
            }
        }

        // Email verification methods
        public async Task<bool> GenerateEmailVerificationTokenAsync(int userId)
        {
            _logger.LogDebug("Generating email verification token for user {UserId}", userId);
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return false;
                }

                if (user.EmailVerified)
                {
                    _logger.LogInformation("User {UserId} email is already verified", userId);
                    return false;
                }

                // Check if there's an existing valid token
                var existingToken = await _context.UserVerifyTokens
                    .Where(t => t.UserId == userId && !t.IsUsed && t.ExpiresAt > DateTime.Now)
                    .FirstOrDefaultAsync();

                if (existingToken != null)
                {
                    // Use the existing token if it's still valid
                    _logger.LogInformation("Using existing valid token for user {UserId}", userId);

                    var baseUrl = "http://localhost:5246";
                    var verifyUrl = $"{baseUrl}/Account/VerifyEmail?token={existingToken.Token}";

                    try
                    {
                        await _emailService.SendEmailAsync(
                            user.Email,
                            "Xác minh email tài khoản CyberTech",
                            GetEmailVerificationTemplate(user.Name, verifyUrl)
                        );
                        _logger.LogInformation("Verification email sent to {Email}", user.Email);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending verification email to {Email}", user.Email);
                        return false;
                    }
                }

                // Generate a new token
                var token = GenerateSecureToken();
                var verifyToken = new UserVerifyToken
                {
                    UserId = userId,
                    Token = token,
                    CreatedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddHours(1), // Token valid for 1 hour
                    IsUsed = false
                };

                _context.UserVerifyTokens.Add(verifyToken);
                await _context.SaveChangesAsync();
                _logger.LogDebug("Generated new verification token for user {UserId}", userId);

                // Send verification email
                var newBaseUrl = "http://localhost:5246";
                var newVerifyUrl = $"{newBaseUrl}/Account/VerifyEmail?token={token}";

                try
                {
                    await _emailService.SendEmailAsync(
                        user.Email,
                        "Xác minh email tài khoản CyberTech",
                        GetEmailVerificationTemplate(user.Name, newVerifyUrl)
                    );
                    _logger.LogInformation("Verification email sent to {Email}", user.Email);
                    return true;
                }
                catch (Exception ex)
                {
                    // If sending email fails, delete the token
                    _context.UserVerifyTokens.Remove(verifyToken);
                    await _context.SaveChangesAsync();
                    _logger.LogError(ex, "Error sending verification email to {Email}", user.Email);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating verification token for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> VerifyEmailAsync(string token)
        {
            _logger.LogDebug("Verifying email with token");
            try
            {
                var verifyToken = await _context.UserVerifyTokens
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed && t.ExpiresAt > DateTime.Now);

                if (verifyToken == null)
                {
                    _logger.LogWarning("Invalid or expired verification token");
                    return false;
                }

                var user = verifyToken.User;
                if (user == null)
                {
                    _logger.LogWarning("User not found for token");
                    return false;
                }

                user.EmailVerified = true;
                verifyToken.IsUsed = true;
                verifyToken.VerifiedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Email verified for user {UserId}", user.UserID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email");
                return false;
            }
        }

        public async Task<bool> IsEmailVerificationTokenValidAsync(string token)
        {
            try
            {
                var verifyToken = await _context.UserVerifyTokens
                    .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed && t.ExpiresAt > DateTime.Now);

                return verifyToken != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking verification token validity");
                return false;
            }
        }

        public async Task<bool> ResendVerificationEmailAsync(int userId)
        {
            _logger.LogDebug("Resending verification email for user {UserId}", userId);
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return false;
                }

                if (user.EmailVerified)
                {
                    _logger.LogInformation("User {UserId} email is already verified", userId);
                    return false;
                }

                // Delete any existing tokens
                var existingTokens = await _context.UserVerifyTokens
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                if (existingTokens.Any())
                {
                    _context.UserVerifyTokens.RemoveRange(existingTokens);
                    await _context.SaveChangesAsync();
                }

                // Generate a new token
                return await GenerateEmailVerificationTokenAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification email for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> IsEmailVerifiedAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return false;
                }

                return user.EmailVerified;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email verification status for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> MarkEmailAsVerifiedAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return false;
                }

                user.EmailVerified = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Email marked as verified for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking email as verified for user {UserId}", userId);
                return false;
            }
        }

        private string GetEmailVerificationTemplate(string userName, string verifyUrl)
        {
            return $@"
                <h2 style='color: #333; margin-top: 0;'>Xác minh địa chỉ email</h2>
                <p style='color: #666; line-height: 1.6;'>
                    Xin chào {userName},<br><br>
                    Cảm ơn bạn đã đăng ký tài khoản tại CyberTech. Để hoàn tất quá trình đăng ký,
                    vui lòng xác minh địa chỉ email của bạn bằng cách nhấp vào nút bên dưới.
                </p>
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{verifyUrl}' 
                       style='background-color: #007bff; color: white; padding: 12px 24px; 
                              text-decoration: none; border-radius: 4px; display: inline-block;
                              font-weight: bold;'>
                        Xác minh email
                    </a>
                </div>
                <p style='color: #666; font-size: 14px; margin-bottom: 0;'>
                    <strong>Lưu ý quan trọng:</strong><br>
                    - Link xác minh sẽ hết hạn sau 1 giờ<br>
                    - Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này<br>
                    - Vui lòng không chia sẻ link này với người khác
                </p>";
        }
    }
}

