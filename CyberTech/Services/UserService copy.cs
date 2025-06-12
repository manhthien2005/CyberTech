// using BCrypt.Net;
// using CyberTech.Data;
// using CyberTech.Models;
// using Microsoft.EntityFrameworkCore;
// using System.Security.Cryptography;
// using Microsoft.Extensions.Logging;

// namespace CyberTech.Services
// {
//     public class UserService : IUserService
//     {
//         private readonly ApplicationDbContext _context;
//         private readonly IEmailService _emailService;
//         private readonly ILogger<UserService> _logger;

//         public UserService(ApplicationDbContext context, IEmailService emailService, ILogger<UserService> logger)
//         {
//             _context = context;
//             _emailService = emailService;
//             _logger = logger;
//             _logger.LogInformation("Khởi tạo UserService");
//         }

//         public async Task<bool> RegisterAsync(string name, string username, string email, string password)
//         {
//             _logger.LogDebug("Bắt đầu đăng ký: {Email}", email);
//             try
//             {
//                 if (await IsEmailTakenAsync(email))
//                 {
//                     _logger.LogWarning("Email đã tồn tại: {Email}", email);
//                     return false;
//                 }

//                 if (await IsUsernameTakenAsync(username))
//                 {
//                     _logger.LogWarning("Tên đăng nhập đã tồn tại: {Username}", username);
//                     return false;
//                 }

//                 var existingUser = await GetUserByEmailAsync(email);
//                 if (existingUser != null)
//                 {
//                     var isExternal = await IsExternalAccountAsync(email);
//                     if (isExternal)
//                     {
//                         _logger.LogWarning("Email đã được sử dụng bởi tài khoản ngoài: {Email}", email);
//                         return false;
//                     }
//                 }

//                 var user = new User
//                 {
//                     Name = name,
//                     Username = username,
//                     Email = email,
//                     Role = UserRole.Customer,
//                     UserStatus = UserStatus.Active,
//                     CreatedAt = DateTime.Now,
//                     EmailVerified = true
//                 };

//                 var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
//                 var authMethod = new UserAuthMethod
//                 {
//                     User = user,
//                     AuthType = AuthType.Password,
//                     AuthKey = hashedPassword,
//                     CreatedAt = DateTime.Now
//                 };

//                 await _context.Users.AddAsync(user);
//                 await _context.UserAuthMethods.AddAsync(authMethod);
//                 await _context.SaveChangesAsync();

//                 _logger.LogInformation("Đăng ký thành công: {Email}", email);
//                 return true;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Lỗi đăng ký {Email}: {Message}", email, ex.Message);
//                 return false;
//             }
//         }

//         public async Task<(bool Success, string ErrorMessage, User User)> AuthenticateAsync(string email, string password)
//         {
//             _logger.LogDebug("Bắt đầu đăng nhập: {Email}", email);
//             try
//             {
//                 var user = await GetUserByEmailAsync(email);
//                 if (user == null)
//                 {
//                     _logger.LogWarning("Tài khoản không tồn tại: {Email}", email);
//                     return (false, "Tài khoản không tồn tại", null);
//                 }

//                 if (user.UserStatus != UserStatus.Active)
//                 {
//                     _logger.LogWarning("Tài khoản không hoạt động: {Email}", email);
//                     return (false, "Tài khoản bị khóa hoặc bị đình chỉ", null);
//                 }

//                 var isExternal = await IsExternalAccountAsync(email);
//                 if (isExternal)
//                 {
//                     _logger.LogWarning("Tài khoản ngoài không thể đăng nhập bằng mật khẩu: {Email}", email);
//                     return (false, "Tài khoản này chỉ có thể đăng nhập bằng Google hoặc Facebook", null);
//                 }

//                 var hasPassword = await HasPasswordAuthAsync(email);
//                 if (!hasPassword)
//                 {
//                     _logger.LogWarning("Tài khoản không có phương thức xác thực bằng mật khẩu: {Email}", email);
//                     return (false, "Tài khoản này không có phương thức xác thực bằng mật khẩu", null);
//                 }

//                 var authMethod = await _context.UserAuthMethods
//                     .AsNoTracking()
//                     .Select(uam => new
//                     {
//                         uam.UserID,
//                         uam.AuthType,
//                         uam.AuthKey
//                     })
//                     .Where(uam => uam.UserID == user.UserID &&
//                                  uam.AuthType == AuthType.Password &&
//                                  uam.AuthKey != null)
//                     .FirstOrDefaultAsync();

//                 if (authMethod == null)
//                 {
//                     _logger.LogWarning("Không tìm thấy phương thức xác thực mật khẩu: {Email}", email);
//                     return (false, "Không tìm thấy phương thức xác thực mật khẩu", null);
//                 }

//                 if (!BCrypt.Net.BCrypt.Verify(password, authMethod.AuthKey))
//                 {
//                     _logger.LogWarning("Mật khẩu không đúng: {Email}", email);
//                     return (false, "Mật khẩu không đúng", null);
//                 }

//                 user.LastLoginAt = DateTime.Now;
//                 await _context.SaveChangesAsync();

//                 _logger.LogInformation("Đăng nhập thành công: {Email}", email);
//                 return (true, null, user);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Lỗi đăng nhập {Email}: {Message}", email, ex.Message);
//                 return (false, "Đã xảy ra lỗi khi đăng nhập", null);
//             }
//         }

//         public async Task<(bool Success, string ErrorMessage, User User)> ExternalLoginAsync(string provider, string providerId, string email, string name)
//         {
//             _logger.LogDebug("Bắt đầu đăng nhập ngoài với {Provider} cho {Email}", provider, email);
//             try
//             {
//                 var authType = Enum.Parse<AuthType>(provider);
//                 // Kiểm tra xem người dùng đã tồn tại với provider này chưa
//                 var existingUser = await GetUserByProviderAsync(provider, providerId);
//                 if (existingUser != null)
//                 {
//                     _logger.LogInformation("Người dùng đã tồn tại với {Provider}: {Email}", provider, email);
//                     existingUser.LastLoginAt = DateTime.Now;
//                     await _context.SaveChangesAsync();
//                     return (true, null, existingUser);
//                 }

//                 // Nếu không tìm thấy theo provider, kiểm tra theo email
//                 var userByEmail = await GetUserByEmailAsync(email);
//                 if (userByEmail != null)
//                 {
//                     _logger.LogInformation("Tìm thấy người dùng theo email: {Email}", email);

//                     // Kiểm tra xem người dùng đã có phương thức xác thực này chưa
//                     var existingAuthMethod = await GetUserAuthMethodAsync(userByEmail.UserID, provider);
//                     if (existingAuthMethod == null)
//                     {
//                         // Thêm phương thức xác thực mới
//                         var success = await AddAuthMethodAsync(userByEmail.UserID, provider, providerId);
//                         if (!success)
//                         {
//                             _logger.LogWarning("Không thể thêm phương thức xác thực {Provider} cho {Email}", provider, email);
//                             return (false, "Không thể thêm phương thức xác thực", null);
//                         }
//                     }

//                     userByEmail.LastLoginAt = DateTime.Now;
//                     await _context.SaveChangesAsync();
//                     return (true, null, userByEmail);
//                 }

//                 // Tạo người dùng mới
//                 _logger.LogInformation("Tạo người dùng mới cho {Email}", email);
//                 var username = GenerateUniqueUsername(email);
//                 var newUser = new User
//                 {
//                     Name = name,
//                     Username = username,
//                     Email = email,
//                     Role = UserRole.Customer,
//                     UserStatus = UserStatus.Active,
//                     CreatedAt = DateTime.Now,
//                     EmailVerified = true,
//                     LastLoginAt = DateTime.Now
//                 };

//                 var newAuthMethod = new UserAuthMethod
//                 {
//                     User = newUser,
//                     AuthType = authType,
//                     AuthKey = providerId,
//                     CreatedAt = DateTime.Now
//                 };

//                 await _context.Users.AddAsync(newUser);
//                 await _context.UserAuthMethods.AddAsync(newAuthMethod);
//                 await _context.SaveChangesAsync();

//                 _logger.LogInformation("Đã tạo người dùng mới và phương thức xác thực {Provider} cho {Email}", provider, email);
//                 return (true, null, newUser);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Lỗi trong ExternalLoginAsync: {Message}", ex.Message);
//                 return (false, "Đã xảy ra lỗi khi xử lý đăng nhập ngoài", null);
//             }
//         }

//         public async Task<User> GetUserByProviderAsync(string provider, string providerId)
//         {
//             try
//             {
//                 var authType = Enum.Parse<AuthType>(provider);
//                 var authMethod = await _context.UserAuthMethods
//                     .Include(uam => uam.User)
//                     .Where(uam => uam.AuthType == authType && uam.AuthKey == providerId)
//                     .Select(uam => new
//                     {
//                         User = new User
//                         {
//                             UserID = uam.User.UserID,
//                             Name = uam.User.Name ?? string.Empty,
//                             Username = uam.User.Username ?? string.Empty,
//                             Email = uam.User.Email ?? string.Empty,
//                             ProfileImageURL = uam.User.ProfileImageURL,
//                             Role = uam.User.Role,
//                             Phone = uam.User.Phone,
//                             Salary = uam.User.Salary,
//                             TotalSpent = uam.User.TotalSpent,
//                             OrderCount = uam.User.OrderCount,
//                             RankId = uam.User.RankId,
//                             EmailVerified = uam.User.EmailVerified,
//                             UserStatus = uam.User.UserStatus,
//                             CreatedAt = uam.User.CreatedAt,
//                             LastLoginAt = uam.User.LastLoginAt
//                         }
//                     })
//                     .FirstOrDefaultAsync();

//                 return authMethod?.User;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Lỗi trong GetUserByProviderAsync: {Message}", ex.Message);
//                 return null;
//             }
//         }

//         public async Task<bool> GeneratePasswordResetTokenAsync(string email)
//         {
//             _logger.LogDebug("Bắt đầu tạo token đặt lại mật khẩu: {Email}", email);
//             try
//             {
//                 var user = await GetUserByEmailAsync(email);
//                 if (user == null)
//                 {
//                     _logger.LogWarning("Không tìm thấy tài khoản: {Email}", email);
//                     return false;
//                 }

//                 if (!await CanResetPasswordAsync(email))
//                 {
//                     _logger.LogWarning("Tài khoản không thể đặt lại mật khẩu: {Email}", email);
//                     return false;
//                 }

//                 // Kiểm tra số lần yêu cầu trong 1 giờ
//                 var attempts = await GetPasswordResetAttemptsAsync(email);
//                 if (attempts >= 3)
//                 {
//                     _logger.LogWarning("Quá nhiều yêu cầu đặt lại mật khẩu trong 1 giờ: {Email}", email);
//                     return false;
//                 }

//                 // Xóa các token cũ
//                 var oldTokens = await _context.PasswordResetTokens
//                     .Where(t => t.UserID == user.UserID)
//                     .ToListAsync();
//                 if (oldTokens.Any())
//                 {
//                     _context.PasswordResetTokens.RemoveRange(oldTokens);
//                     await _context.SaveChangesAsync();
//                     _logger.LogDebug("Đã xóa {Count} token cũ cho user {UserId}", oldTokens.Count, user.UserID);
//                 }

//                 var token = GenerateSecureToken();
//                 var resetToken = new PasswordResetToken
//                 {
//                     UserID = user.UserID,
//                     Token = token,
//                     CreatedAt = DateTime.Now,
//                     ExpiresAt = DateTime.Now.AddMinutes(15),
//                     Used = false
//                 };

//                 _context.PasswordResetTokens.Add(resetToken);
//                 await _context.SaveChangesAsync();
//                 _logger.LogDebug("Đã tạo token mới cho user {UserId}", user.UserID);

//                 var baseUrl = "http://localhost:5246";
//                 var resetUrl = $"{baseUrl}/Account/ResetPassword?token={token}";

//                 try
//                 {
//                     await _emailService.SendPasswordResetEmailAsync(email, resetUrl);
//                     _logger.LogInformation("Đã gửi email đặt lại mật khẩu thành công: {Email}", email);
//                     return true;
//                 }
//                 catch (Exception ex)
//                 {
//                     // Nếu gửi email thất bại, xóa token
//                     _context.PasswordResetTokens.Remove(resetToken);
//                     await _context.SaveChangesAsync();
//                     _logger.LogError(ex, "Lỗi gửi email đặt lại mật khẩu {Email}: {Message}", email, ex.Message);
//                     return false;
//                 }
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Lỗi tạo token đặt lại mật khẩu {Email}: {Message}", email, ex.Message);
//                 return false;
//             }
//         }

//         public async Task<bool> ResetPasswordAsync(string token, string newPassword)
//         {
//             _logger.LogDebug("Bắt đầu đặt lại mật khẩu với token");
//             try
//             {
//                 if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
//                 {
//                     _logger.LogWarning("Mật khẩu mới không hợp lệ");
//                     return false;
//                 }

//                 var resetToken = await _context.PasswordResetTokens
//                     .FirstOrDefaultAsync(t => t.Token == token && !t.Used && t.ExpiresAt > DateTime.Now);

//                 if (resetToken == null)
//                 {
//                     _logger.LogWarning("Token không hợp lệ hoặc đã hết hạn");
//                     return false;
//                 }

//                 var authMethod = await _context.UserAuthMethods
//                     .FirstOrDefaultAsync(uam => uam.UserID == resetToken.UserID && uam.AuthType == AuthType.Password);

//                 if (authMethod == null)
//                 {
//                     _logger.LogWarning("Không tìm thấy phương thức xác thực mật khẩu: {UserId}", resetToken.UserID);
//                     return false;
//                 }

//                 // Kiểm tra mật khẩu mới không trùng với mật khẩu cũ
//                 if (BCrypt.Net.BCrypt.Verify(newPassword, authMethod.AuthKey))
//                 {
//                     _logger.LogWarning("Mật khẩu mới không được trùng với mật khẩu cũ: {UserId}", resetToken.UserID);
//                     return false;
//                 }

//                 authMethod.AuthKey = BCrypt.Net.BCrypt.HashPassword(newPassword);
//                 resetToken.Used = true;
//                 await _context.SaveChangesAsync();

//                 _logger.LogInformation("Đặt lại mật khẩu thành công: {UserId}", resetToken.UserID);
//                 return true;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Lỗi đặt lại mật khẩu: {Message}", ex.Message);
//                 return false;
//             }
//         }

//         public async Task<bool> ValidatePasswordResetTokenAsync(string token)
//         {
//             try
//             {
//                 var resetToken = await _context.PasswordResetTokens
//                     .FirstOrDefaultAsync(t => t.Token == token && !t.Used && t.ExpiresAt > DateTime.Now);

//                 if (resetToken == null)
//                 {
//                     _logger.LogWarning("Token không hợp lệ hoặc đã hết hạn");
//                     return false;
//                 }

//                 return true;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Lỗi kiểm tra token: {Message}", ex.Message);
//                 return false;
//             }
//         }

//         public async Task<bool> InvalidatePasswordResetTokenAsync(string token)
//         {
//             try
//             {
//                 var resetToken = await _context.PasswordResetTokens
//                     .FirstOrDefaultAsync(t => t.Token == token);

//                 if (resetToken != null)
//                 {
//                     resetToken.Used = true;
//                     await _context.SaveChangesAsync();
//                     _logger.LogDebug("Đã vô hiệu hóa token: {Token}", token);
//                     return true;
//                 }

//                 return false;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Lỗi vô hiệu hóa token: {Message}", ex.Message);
//                 return false;
//             }
//         }

//         public async Task<bool> IsPasswordResetTokenExpiredAsync(string token)
//         {
//             try
//             {
//                 var resetToken = await _context.PasswordResetTokens
//                     .FirstOrDefaultAsync(t => t.Token == token);

//                 if (resetToken == null)
//                 {
//                     return true;
//                 }

//                 return resetToken.ExpiresAt <= DateTime.Now;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Lỗi kiểm tra hết hạn token: {Message}", ex.Message);
//                 return true;
//             }
//         }

//         public async Task<int> GetPasswordResetAttemptsAsync(string email)
//         {
//             try
//             {
//                 var user = await GetUserByEmailAsync(email);
//                 if (user == null)
//                 {
//                     return 0;
//                 }

//                 return await _context.PasswordResetTokens
//                     .CountAsync(t => t.UserID == user.UserID &&
//                                    t.CreatedAt >= DateTime.Now.AddHours(-1));
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Lỗi đếm số lần yêu cầu đặt lại mật khẩu: {Message}", ex.Message);
//                 return 0;
//             }
//         }

//         public async Task<bool> IsEmailTakenAsync(string email)
//         {
//             return await _context.Users.AnyAsync(u => u.Email == email);
//         }

//         public async Task<bool> IsUsernameTakenAsync(string username)
//         {
//             return await _context.Users.AnyAsync(u => u.Username == username);
//         }

//         public async Task<User> GetUserByEmailAsync(string email)
//         {
//             try
//             {
//                 _logger.LogInformation("Getting user by email: {Email}", email);

//                 var user = await _context.Users
//                     .AsNoTracking()
//                     .Include(u => u.Rank)
//                     .FirstOrDefaultAsync(u => u.Email == email);

//                 if (user == null)
//                 {
//                     _logger.LogWarning("User not found for email: {Email}", email);
//                     return null;
//                 }

//                 _logger.LogInformation("Found user with ID: {UserId}, Name: {Name}, Email: {Email}",
//                     user.UserID, user.Name, user.Email);

//                 return user;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error in GetUserByEmailAsync: {Message}", ex.Message);
//                 return null;
//             }
//         }

//         public async Task<bool> IncrementLoginAttemptsAsync(string email)
//         {
//             return true;
//         }

//         public async Task<int> GetLoginAttemptsAsync(string email)
//         {
//             return 0;
//         }

//         public async Task ResetLoginAttemptsAsync(string email)
//         {
//         }

//         private string GenerateSecureToken()
//         {
//             _logger.LogDebug("Tạo token bảo mật");
//             var bytes = new byte[32];
//             using (var rng = RandomNumberGenerator.Create())
//             {
//                 rng.GetBytes(bytes);
//             }
//             return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
//         }

//         private string GenerateUniqueUsername(string email)
//         {
//             _logger.LogDebug("Tạo tên đăng nhập cho: {Email}", email);
//             var baseUsername = email.Split('@')[0].Replace(".", "");
//             var username = baseUsername;
//             var counter = 1;
//             while (_context.Users.Any(u => u.Username == username))
//             {
//                 username = $"{baseUsername}{counter++}";
//             }
//             _logger.LogDebug("Đã tạo tên đăng nhập: {Username}", username);
//             return username;
//         }
//         public async Task<bool> UpdateProfileImageAsync(int userId, string imageUrl)
//         {
//             try
//             {
//                 var user = await _context.Users.FindAsync(userId);
//                 if (user == null) return false;

//                 if (string.IsNullOrEmpty(user.ProfileImageURL))
//                 {
//                     user.ProfileImageURL = imageUrl;
//                     await _context.SaveChangesAsync();
//                     return true;
//                 }
//                 return false;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error updating profile image for user {UserId}", userId);
//                 return false;
//             }
//         }

//         public async Task<UserAuthMethod> GetUserAuthMethodAsync(int userId, string provider)
//         {
//             try
//             {
//                 var authType = Enum.Parse<AuthType>(provider);
//                 return await _context.UserAuthMethods
//                     .AsNoTracking()
//                     .Where(uam => uam.UserID == userId && uam.AuthType == authType)
//                     .Select(uam => new UserAuthMethod
//                     {
//                         ID = uam.ID,
//                         UserID = uam.UserID,
//                         AuthType = uam.AuthType,
//                         AuthKey = uam.AuthKey,
//                         CreatedAt = uam.CreatedAt
//                     })
//                     .FirstOrDefaultAsync();
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Lỗi trong GetUserAuthMethodAsync: {Message}", ex.Message);
//                 return null;
//             }
//         }

//         public async Task<bool> AddAuthMethodAsync(int userId, string provider, string providerId)
//         {
//             try
//             {
//                 var authType = Enum.Parse<AuthType>(provider);
//                 var existingAuthMethod = await _context.UserAuthMethods
//                     .AsNoTracking()
//                     .Select(uam => new { uam.UserID, uam.AuthType, uam.AuthKey })
//                     .FirstOrDefaultAsync(uam => uam.UserID == userId &&
//                                                uam.AuthType == authType &&
//                                                uam.AuthKey == providerId);

//                 if (existingAuthMethod != null)
//                 {
//                     return true;
//                 }

//                 var newAuthMethod = new UserAuthMethod
//                 {
//                     UserID = userId,
//                     AuthType = authType,
//                     AuthKey = providerId,
//                     CreatedAt = DateTime.Now
//                 };

//                 await _context.UserAuthMethods.AddAsync(newAuthMethod);
//                 await _context.SaveChangesAsync();
//                 return true;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Lỗi trong AddAuthMethodAsync: {Message}", ex.Message);
//                 return false;
//             }
//         }

//         public async Task<bool> CanResetPasswordAsync(string email)
//         {
//             var user = await GetUserByEmailAsync(email);
//             if (user == null) return false;

//             // Kiểm tra xem tài khoản có phải là tài khoản ngoài không
//             var isExternal = await IsExternalAccountAsync(email);
//             if (isExternal) return false;

//             // Kiểm tra xem tài khoản có phương thức xác thực bằng mật khẩu không
//             var hasPassword = await HasPasswordAuthAsync(email);
//             if (!hasPassword) return false;

//             return true;
//         }

//         public async Task<bool> HasPasswordAuthAsync(string email)
//         {
//             var user = await GetUserByEmailAsync(email);
//             if (user == null) return false;

//             return await _context.UserAuthMethods
//                 .AnyAsync(uam => uam.UserID == user.UserID && uam.AuthType == AuthType.Password);
//         }

//         public async Task<bool> IsExternalAccountAsync(string email)
//         {
//             var user = await GetUserByEmailAsync(email);
//             if (user == null) return false;

//             var authMethods = await _context.UserAuthMethods
//                 .Where(uam => uam.UserID == user.UserID)
//                 .Select(uam => uam.AuthType)
//                 .ToListAsync();

//             return authMethods.Any(type => type == AuthType.Google || type == AuthType.Facebook);
//         }
//     }
// }

