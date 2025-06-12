using CyberTech.Models;
using CyberTech.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Configuration;
using CyberTech.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Text.Json;

namespace CyberTech.Controllers
{
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private static readonly string[] AllowedRoles = new[] { "Admin", "Staff", "Support", "Manager", "SuperAdmin" };

        public AdminController(
            IUserService userService,
            ILogger<AdminController> logger,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _userService = userService;
            _logger = logger;
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                if (AllowedRoles.Contains(role))
                {
                    return RedirectToAction("Index", "Dashboard");
                }
            }
            return RedirectToAction("Login");
        }

        [HttpGet("Login")]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                if (AllowedRoles.Contains(role))
                {
                    return RedirectToAction("Index", "Dashboard");
                }
                return RedirectToAction("AccessDenied", "Account");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost("Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe)
        {
            try
            {
                var (success, errorMessage, user) = await _userService.AuthenticateAsync(email, password);

                if (!success || user == null)
                {
                    return Json(new { error = errorMessage ?? "Email hoặc mật khẩu không đúng" });
                }

                // Check if user has allowed role
                if (!AllowedRoles.Contains(user.Role))
                {
                    _logger.LogWarning("Unauthorized access attempt by user {Email} with role {Role}", email, user.Role);
                    return Json(new { error = "Bạn không có quyền truy cập vào trang này" });
                }

                // Check if user is active
                if (user.UserStatus != "Active")
                {
                    _logger.LogWarning("Inactive user attempted to login: {Email}", email);
                    return Json(new { error = "Tài khoản của bạn đã bị vô hiệu hóa" });
                }

                await SignInUserAsync(user, rememberMe);
                _logger.LogInformation("{Role} {Email} logged in successfully", user.Role, email);

                return Json(new { redirectUrl = Url.Action("Index", "Dashboard") });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin login for email: {Email}", email);
                return Json(new { error = "Đã xảy ra lỗi khi đăng nhập. Vui lòng thử lại sau." });
            }
        }


        [HttpGet("ExternalLogin")]
        public async Task<IActionResult> ExternalLogin(string provider, string returnUrl = null)
        {
            try
            {
                if (string.IsNullOrEmpty(provider) || !new[] { "Google", "Facebook" }.Contains(provider))
                {
                    _logger.LogWarning("Invalid provider specified: {Provider}", provider);
                    return RedirectToAction("Login");
                }

                var properties = new AuthenticationProperties
                {
                    RedirectUri = Url.Action("ExternalLoginCallback", "Admin", new { returnUrl }),
                    Items = { { "LoginProvider", provider } }
                };

                return Challenge(properties, provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during external login initiation");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi bắt đầu đăng nhập";
                return RedirectToAction("Login");
            }
        }

        [HttpGet("ExternalLoginCallback")]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            try
            {
                if (remoteError != null)
                {
                    _logger.LogWarning("External login failed with remote error: {RemoteError}", remoteError);
                    TempData["ErrorMessage"] = $"Lỗi từ nhà cung cấp: {remoteError}";
                    return RedirectToAction("Login");
                }

                var info = await HttpContext.AuthenticateAsync();
                if (info == null || !info.Succeeded)
                {
                    _logger.LogError("External authentication failed");
                    TempData["ErrorMessage"] = "Lỗi xác thực với nhà cung cấp";
                    return RedirectToAction("Login");
                }

                var provider = info.Properties?.Items["LoginProvider"];
                if (string.IsNullOrEmpty(provider) || !new[] { "Google", "Facebook" }.Contains(provider))
                {
                    _logger.LogError("No valid provider specified in authentication properties. Provider: {Provider}", provider);
                    TempData["ErrorMessage"] = "Không thể xác định nhà cung cấp đăng nhập";
                    return RedirectToAction("Login");
                }

                var providerId = info.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = info.Principal.FindFirst(ClaimTypes.Email)?.Value;
                var name = info.Principal.FindFirst(ClaimTypes.Name)?.Value;

                string pictureUrl = null;
                if (provider == "Google")
                {
                    pictureUrl = info.Principal.FindFirst("picture")?.Value;
                }
                else if (provider == "Facebook")
                {
                    var pictureClaim = info.Principal.FindFirst("picture");
                    if (pictureClaim != null)
                    {
                        try
                        {
                            var pictureData = JsonDocument.Parse(pictureClaim.Value);
                            pictureUrl = pictureData.RootElement.GetProperty("data").GetProperty("url").GetString();
                        }
                        catch
                        {
                            _logger.LogWarning("Could not parse Facebook picture data for user {Email}", email);
                        }
                    }
                }

                if (string.IsNullOrEmpty(providerId) || string.IsNullOrEmpty(email))
                {
                    _logger.LogError("Missing required claims: ProviderId={ProviderId}, Email={Email}", providerId, email);
                    TempData["ErrorMessage"] = "Không thể lấy thông tin từ nhà cung cấp";
                    return RedirectToAction("Login");
                }

                // Get user and verify role
                var user = await _userService.GetUserByEmailAsync(email);
                if (user == null || !AllowedRoles.Contains(user.Role))
                {
                    _logger.LogWarning("Unauthorized external login attempt for email: {Email}", email);
                    TempData["ErrorMessage"] = "Bạn không có quyền truy cập vào trang quản trị";
                    return RedirectToAction("Login");
                }

                if (user.UserStatus != "Active")
                {
                    _logger.LogWarning("Inactive user attempted to login: {Email}", email);
                    TempData["ErrorMessage"] = "Tài khoản của bạn đã bị vô hiệu hóa";
                    return RedirectToAction("Login");
                }

                // Update or add external login info
                var existingAuthMethod = await _context.UserAuthMethods
                    .FirstOrDefaultAsync(uam => uam.UserID == user.UserID && uam.AuthType == provider);

                if (existingAuthMethod == null)
                {
                    var authMethod = new UserAuthMethod
                    {
                        UserID = user.UserID,
                        AuthType = provider,
                        AuthKey = providerId
                    };
                    _context.UserAuthMethods.Add(authMethod);
                }

                if (!string.IsNullOrEmpty(pictureUrl))
                {
                    user.ProfileImageURL = pictureUrl;
                }

                await _context.SaveChangesAsync();
                await SignInUserAsync(user, true);

                _logger.LogInformation("{Role} {Email} logged in successfully with {Provider}", user.Role, email, provider);
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during external login callback");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình đăng nhập";
                return RedirectToAction("Login");
            }
        }

        private async Task SignInUserAsync(User user, bool isPersistent)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("Username", user.Username),
                new Claim("ProfileImage", user.ProfileImageURL ?? "/images/default-avatar.png"),
                new Claim("Phone", user.Phone ?? ""),
                new Claim("EmailVerified", user.EmailVerified.ToString()),
                new Claim("UserStatus", user.UserStatus),
                new Claim("CreatedAt", user.CreatedAt.ToString("o"))
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent ? DateTime.UtcNow.AddDays(30) : null
            });

            // Store admin/staff information in session
            HttpContext.Session.SetString("UserId", user.UserID.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("AdminFullName", user.Name);
            HttpContext.Session.SetString("AdminRole", user.Role);
            HttpContext.Session.SetString("ProfileImage", user.ProfileImageURL ?? "/images/default-avatar.png");
            HttpContext.Session.SetString("Phone", user.Phone ?? "");
            HttpContext.Session.SetString("EmailVerified", user.EmailVerified.ToString());
            HttpContext.Session.SetString("UserStatus", user.UserStatus);
            HttpContext.Session.SetString("CreatedAt", user.CreatedAt.ToString("o"));
        }
    }
}