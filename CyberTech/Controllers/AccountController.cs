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
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BCrypt.Net;

namespace CyberTech.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IRecaptchaService _recaptchaService;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AccountController(
            IUserService userService,
            IRecaptchaService recaptchaService,
            ILogger<AccountController> logger,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _userService = userService;
            _recaptchaService = recaptchaService;
            _logger = logger;
            _configuration = configuration;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["RecaptchaSiteKey"] = _configuration["Recaptcha:SiteKey"];
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string recaptchaResponse, string returnUrl = null)
        {
            try
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var (success, errorMessage, user) = await _userService.AuthenticateAsync(email, password);
                    if (!success) return Json(new { success = false, errorMessage = errorMessage ?? "Email hoặc mật khẩu không đúng" });

                    await SignInUserAsync(user, rememberMe);
                    return Json(new { success = true, returnUrl = returnUrl ?? Url.Action("Index", "Home") });
                }

                var (successNonAjax, errorMessageNonAjax, userNonAjax) = await _userService.AuthenticateAsync(email, password);
                if (!successNonAjax)
                {
                    ModelState.AddModelError("", errorMessageNonAjax ?? "Email hoặc mật khẩu không đúng");
                    return View();
                }

                await SignInUserAsync(userNonAjax, rememberMe);
                return Redirect(returnUrl ?? Url.Action("Index", "Home"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", email);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, errorMessage = "Đã xảy ra lỗi khi đăng nhập. Vui lòng thử lại sau." });
                ModelState.AddModelError("", "Đã xảy ra lỗi khi đăng nhập. Vui lòng thử lại sau.");
                return View();
            }
        }

        [HttpGet]
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
                    RedirectUri = Url.Action("ExternalLoginCallback", "Account", new { returnUrl }),
                    Items = { { "LoginProvider", provider } }
                };

                return Challenge(properties, provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during external login initiation");
                ModelState.AddModelError("", "Đã xảy ra lỗi khi bắt đầu đăng nhập");
                return View("Login");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            try
            {
                if (remoteError != null)
                {
                    _logger.LogWarning("External login failed with remote error: {RemoteError}", remoteError);
                    ModelState.AddModelError("", $"Lỗi từ nhà cung cấp: {remoteError}");
                    return RedirectToAction("Login");
                }

                var info = await HttpContext.AuthenticateAsync();
                if (info == null || !info.Succeeded)
                {
                    _logger.LogError("External authentication failed");
                    ModelState.AddModelError("", "Lỗi xác thực với nhà cung cấp");
                    return RedirectToAction("Login");
                }

                var provider = info.Properties?.Items["LoginProvider"];
                if (string.IsNullOrEmpty(provider) || !new[] { "Google", "Facebook" }.Contains(provider))
                {
                    _logger.LogError("No valid provider specified in authentication properties. Provider: {Provider}", provider);
                    ModelState.AddModelError("", "Không thể xác định nhà cung cấp đăng nhập");
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

                _logger.LogInformation("External login info: Provider={Provider}, Email={Email}, Name={Name}, Picture={Picture}", provider, email, name, pictureUrl);

                if (string.IsNullOrEmpty(providerId) || string.IsNullOrEmpty(email))
                {
                    _logger.LogError("Missing required claims: ProviderId={ProviderId}, Email={Email}", providerId, email);
                    ModelState.AddModelError("", "Không thể lấy thông tin từ nhà cung cấp");
                    return RedirectToAction("Login");
                }

                var existingUser = await _userService.GetUserByEmailAsync(email);
                if (existingUser != null)
                {
                    if (existingUser.UserStatus != "Active")
                    {
                        _logger.LogWarning("User attempted to login with external provider but account is {Status}: {Email}", existingUser.UserStatus, email);
                        ModelState.AddModelError("", $"Tài khoản của bạn hiện đang {(existingUser.UserStatus == "Suspended" ? "bị khóa" : "không hoạt động")}");
                        TempData["ErrorMessage"] = $"Tài khoản của bạn hiện đang {(existingUser.UserStatus == "Suspended" ? "bị khóa" : "không hoạt động")}";
                        return RedirectToAction("Login");
                    }

                    var existingAuthMethod = await _userService.GetUserAuthMethodAsync(existingUser.UserID, provider);
                    if (existingAuthMethod == null)
                    {
                        _logger.LogInformation("User {Email} exists but doesn't have {Provider} auth method. Adding it now.", email, provider);
                        var authMethod = new UserAuthMethod
                        {
                            UserID = existingUser.UserID,
                            AuthType = provider,
                            AuthKey = providerId
                        };
                        _context.UserAuthMethods.Add(authMethod);
                        await _context.SaveChangesAsync();
                    }
                    else if (!string.IsNullOrEmpty(pictureUrl))
                    {
                        await _context.SaveChangesAsync();
                    }
                }

                var (success, errorMessage, user) = await _userService.ExternalLoginAsync(provider, providerId, email, name ?? "Unknown");
                if (!success || user == null)
                {
                    _logger.LogWarning("External login processing failed: {ErrorMessage}", errorMessage);
                    ModelState.AddModelError("", errorMessage ?? "Đăng nhập ngoài không thành công");
                    TempData["ErrorMessage"] = errorMessage ?? "Đăng nhập ngoài không thành công";
                    return RedirectToAction("Login");
                }

                if (!string.IsNullOrEmpty(pictureUrl))
                {
                    try
                    {
                        user.ProfileImageURL = pictureUrl;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Updated profile picture from {Provider} for user {Email}", provider, email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating profile picture for user {Email}", email);
                    }
                }

                await SignInUserAsync(user, true);
                _logger.LogInformation("User {Email} successfully logged in with {Provider}", email, provider);
                return Redirect(returnUrl ?? Url.Action("Index", "Home"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during external login callback: {Message}", ex.Message);
                ModelState.AddModelError("", "Đã xảy ra lỗi trong quá trình đăng nhập");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình đăng nhập: " + ex.Message;
                return RedirectToAction("Login");
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string name, string username, string email, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, errorMessage = "Vui lòng điền đầy đủ thông tin" });
                    ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin");
                    return View();
                }

                if (await _userService.IsEmailTakenAsync(email))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, errorMessage = "Email đã được sử dụng" });
                    ModelState.AddModelError("Email", "Email đã được sử dụng");
                    return View();
                }

                if (await _userService.IsUsernameTakenAsync(username))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, errorMessage = "Tên người dùng đã được sử dụng" });
                    ModelState.AddModelError("Username", "Tên người dùng đã được sử dụng");
                    return View();
                }

                var existingUser = await _userService.GetUserByEmailAsync(email);
                if (existingUser != null && await _userService.IsExternalAccountAsync(email))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, errorMessage = "Email này đã được sử dụng bởi tài khoản Google hoặc Facebook" });
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản Google hoặc Facebook");
                    return View();
                }

                var success = await _userService.RegisterAsync(name, username, email, password);
                if (!success)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, errorMessage = "Đăng ký không thành công. Vui lòng thử lại sau." });
                    ModelState.AddModelError("", "Đăng ký không thành công. Vui lòng thử lại sau.");
                    return View();
                }

                // Get the newly registered user
                var user = await _userService.GetUserByEmailAsync(email);
                if (user != null)
                {
                    // Send verification email for password-based accounts
                    var authMethod = await _userService.GetUserAuthMethodAsync(user.UserID, "Password");
                    if (authMethod != null)
                    {
                        await _userService.GenerateEmailVerificationTokenAsync(user.UserID);
                    }
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    _logger.LogInformation("Đăng ký thành công. Vui lòng kiểm tra email để xác minh tài khoản.");
                    return Json(new { success = true, message = "Đăng ký thành công. Vui lòng kiểm tra email để xác minh tài khoản." });
                }

                _logger.LogInformation("Đăng ký thành công. Vui lòng kiểm tra email để xác minh tài khoản.");
                TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng kiểm tra email để xác minh tài khoản.";
                return RedirectToAction("RegistrationSuccess");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Register action for email: {Email}", email);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, errorMessage = "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại sau." });
                ModelState.AddModelError("", "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại sau.");
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            Response.Cookies.Delete("UserId");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Email không được để trống" });
                    ModelState.AddModelError("", "Email không được để trống");
                    return View();
                }

                var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
                if (!emailRegex.IsMatch(email))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Email không hợp lệ" });
                    ModelState.AddModelError("", "Email không hợp lệ");
                    return View();
                }

                if (!await _userService.CanResetPasswordAsync(email))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Tài khoản này không thể đặt lại mật khẩu" });
                    ModelState.AddModelError("", "Tài khoản này không thể đặt lại mật khẩu");
                    return View();
                }

                var success = await _userService.GeneratePasswordResetTokenAsync(email);
                if (!success)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Không thể gửi email đặt lại mật khẩu. Vui lòng thử lại sau." });
                    ModelState.AddModelError("", "Không thể gửi email đặt lại mật khẩu. Vui lòng thử lại sau.");
                    return View();
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = true, message = "Vui lòng kiểm tra email của bạn để đặt lại mật khẩu" });
                TempData["SuccessMessage"] = "Vui lòng kiểm tra email của bạn để đặt lại mật khẩu";
                return RedirectToAction("ResetPasswordConfirmation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong ForgotPassword action cho email: {Email}", email);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
                ModelState.AddModelError("", "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau.");
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Token không được cung cấp");
                return RedirectToAction("Login");
            }

            if (await _userService.IsPasswordResetTokenExpiredAsync(token))
            {
                _logger.LogWarning("Token đã hết hạn");
                TempData["ErrorMessage"] = "Link đặt lại mật khẩu đã hết hạn. Vui lòng yêu cầu link mới.";
                return RedirectToAction("ForgotPassword");
            }

            if (!await _userService.ValidatePasswordResetTokenAsync(token))
            {
                _logger.LogWarning("Token không hợp lệ");
                TempData["ErrorMessage"] = "Link đặt lại mật khẩu không hợp lệ. Vui lòng yêu cầu link mới.";
                return RedirectToAction("ForgotPassword");
            }

            ViewData["Token"] = token;
            return View();
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string newPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Token không được cung cấp");
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Token không hợp lệ" });
                    TempData["ErrorMessage"] = "Token không hợp lệ";
                    return RedirectToAction("ForgotPassword");
                }

                if (string.IsNullOrEmpty(newPassword))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Mật khẩu mới không được để trống" });
                    ModelState.AddModelError("", "Mật khẩu mới không được để trống");
                    return View();
                }

                if (newPassword.Length < 6)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Mật khẩu phải có ít nhất 6 ký tự" });
                    ModelState.AddModelError("", "Mật khẩu phải có ít nhất 6 ký tự");
                    return View();
                }

                if (!await _userService.ValidatePasswordResetTokenAsync(token))
                {
                    _logger.LogWarning("Token không hợp lệ hoặc không tồn tại: {Token}", token);
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Link đặt lại mật khẩu không hợp lệ" });
                    TempData["ErrorMessage"] = "Link đặt lại mật khẩu không hợp lệ. Vui lòng yêu cầu link mới.";
                    return RedirectToAction("ForgotPassword");
                }

                if (await _userService.IsPasswordResetTokenExpiredAsync(token))
                {
                    _logger.LogWarning("Token đã hết hạn: {Token}", token);
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Link đặt lại mật khẩu đã hết hạn" });
                    TempData["ErrorMessage"] = "Link đặt lại mật khẩu đã hết hạn. Vui lòng yêu cầu link mới.";
                    return RedirectToAction("ForgotPassword");
                }

                var success = await _userService.ResetPasswordAsync(token, newPassword);
                if (!success)
                {
                    _logger.LogError("Không thể đặt lại mật khẩu cho token: {Token}", token);
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Không thể đặt lại mật khẩu. Vui lòng thử lại sau." });
                    ModelState.AddModelError("", "Không thể đặt lại mật khẩu. Vui lòng thử lại sau.");
                    return View();
                }

                await _userService.InvalidatePasswordResetTokenAsync(token);
                _logger.LogInformation("Đặt lại mật khẩu thành công cho token: {Token}", token);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = true, message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại." });
                TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong ResetPassword action cho token: {Token}", token);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Đã xảy ra lỗi khi đặt lại mật khẩu. Vui lòng thử lại sau." });
                ModelState.AddModelError("", "Đã xảy ra lỗi khi đặt lại mật khẩu. Vui lòng thử lại sau.");
                return View();
            }
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();


        private async Task SetCommonViewBagProperties(User user)
        {
            ViewBag.UserProfileImage = user.ProfileImageURL ?? "/images/default-avatar.png";

            // Get current rank
            var currentRank = user.Rank?.RankName ?? "Thành viên";
            ViewBag.UserRank = currentRank;

            // Get active voucher count
            var activeVoucherCount = await _context.UserVouchers
                .CountAsync(uv => uv.UserID == user.UserID &&
                           !uv.IsUsed &&
                           uv.Voucher.IsActive &&
                           uv.Voucher.ValidTo > DateTime.Now);

            ViewBag.VoucherCount = activeVoucherCount;

            // Get max rank ID (5)
            const int maxRankId = 5;

            // Check if user has reached max rank
            if (user.RankId == maxRankId)
            {
                ViewBag.NextRank = null;
                ViewBag.IsMaxRank = true;
                ViewBag.MaxRankMessage = "Chúc mừng! Bạn đã đạt cấp độ cao nhất";
                return;
            }

            // Get next rank based on total spent
            var nextRank = await _context.Ranks
                .Where(r => r.MinTotalSpent > user.TotalSpent && r.RankId <= maxRankId)
                .OrderBy(r => r.MinTotalSpent)
                .FirstOrDefaultAsync();

            ViewBag.NextRank = nextRank;
            ViewBag.IsMaxRank = false;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            if (emailClaim == null)
            {
                _logger.LogError("Email claim not found in user claims");
                return Unauthorized("Invalid user identifier.");
            }

            _logger.LogInformation("Email claim found: {Email}", emailClaim.Value);

            var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
            if (user == null) return NotFound();

            // Ensure user has a rank
            if (user.RankId == null)
            {
                user.RankId = 1;
                await _context.SaveChangesAsync();
            }

            await SetCommonViewBagProperties(user);
            return View(user);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string name, string phone, string gender, string dateOfBirth)
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogError("Email claim not found in user claims");
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                if (string.IsNullOrEmpty(name))
                {
                    return Json(new { success = false, message = "Tên không được để trống" });
                }

                DateTime? dob = null;
                if (!string.IsNullOrEmpty(dateOfBirth))
                {
                    if (!DateTime.TryParse(dateOfBirth, out var parsedDob))
                    {
                        return Json(new { success = false, message = "Ngày sinh không hợp lệ" });
                    }
                    dob = parsedDob;
                }

                byte? genderValue = null;
                if (!string.IsNullOrEmpty(gender))
                {
                    if (!byte.TryParse(gender, out var parsedGender) || (parsedGender != 1 && parsedGender != 2))
                    {
                        return Json(new { success = false, message = "Giới tính không hợp lệ" });
                    }
                    genderValue = parsedGender;
                }

                var success = await _userService.UpdateProfileAsync(emailClaim.Value, name, phone, genderValue, dob);
                if (!success)
                {
                    return Json(new { success = false, message = "Không thể cập nhật thông tin" });
                }

                // Cập nhật session và claims
                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user != null)
                {
                    // Cập nhật session
                    HttpContext.Session.SetString("Name", user.Name);
                    HttpContext.Session.SetString("Phone", user.Phone ?? "");
                    HttpContext.Session.SetString("Gender", user.Gender?.ToString() ?? "");
                    HttpContext.Session.SetString("DateOfBirth", user.DateOfBirth?.ToString("o") ?? "");

                    // Cập nhật claims
                    var identity = User.Identity as ClaimsIdentity;
                    if (identity != null)
                    {
                        // Xóa claims cũ
                        var nameClaim = identity.FindFirst(ClaimTypes.Name);
                        var phoneClaim = identity.FindFirst("Phone");
                        var genderClaim = identity.FindFirst("Gender");
                        var dobClaim = identity.FindFirst("DateOfBirth");

                        if (nameClaim != null) identity.RemoveClaim(nameClaim);
                        if (phoneClaim != null) identity.RemoveClaim(phoneClaim);
                        if (genderClaim != null) identity.RemoveClaim(genderClaim);
                        if (dobClaim != null) identity.RemoveClaim(dobClaim);

                        // Thêm claims mới
                        identity.AddClaim(new Claim(ClaimTypes.Name, user.Name));
                        identity.AddClaim(new Claim("Phone", user.Phone ?? ""));
                        identity.AddClaim(new Claim("Gender", user.Gender?.ToString() ?? ""));
                        identity.AddClaim(new Claim("DateOfBirth", user.DateOfBirth?.ToString("o") ?? ""));

                        // Cập nhật cookie authentication
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(identity));
                    }
                }

                return Json(new { success = true, message = "Cập nhật thông tin thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật thông tin người dùng");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật thông tin" });
            }
        }

        [HttpGet]
        [Authorize]
        [Route("Account/Orders")]
        public async Task<IActionResult> Orders(int page = 1, string search = null, string status = null, string date = null, string paymentStatus = null)
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogError("Email claim not found in user claims");
                    return Unauthorized("Invalid user identifier.");
                }

                _logger.LogInformation("Email claim found: {Email}", emailClaim.Value);

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null) return NotFound();

                // Ensure user has a rank
                if (user.RankId == null)
                {
                    user.RankId = 1;
                    await _context.SaveChangesAsync();
                }

                await SetCommonViewBagProperties(user);

                const int pageSize = 3;
                var query = _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                            .ThenInclude(p => p.ProductImages)
                    .Include(o => o.Payments)
                    .Where(o => o.UserID == user.UserID);

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(o => o.OrderID.ToString().Contains(search));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(o => o.Status == status);
                }

                if (!string.IsNullOrEmpty(paymentStatus))
                {
                    query = query.Where(o => o.Payments.Any(p => p.PaymentStatus == paymentStatus));
                }

                if (!string.IsNullOrEmpty(date))
                {
                    if (DateTime.TryParse(date, out DateTime filterDate))
                    {
                        query = query.Where(o => o.CreatedAt.Date == filterDate.Date);
                    }
                }

                // Get total count before pagination
                var totalOrders = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);

                // Ensure page is within valid range
                page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

                // Apply pagination
                var orders = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalOrders = totalOrders;
                ViewBag.SearchTerm = search;
                ViewBag.StatusFilter = status;
                ViewBag.DateFilter = date;
                ViewBag.PaymentStatusFilter = paymentStatus;

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders for user {Email}", User.Identity.Name);
                return View(new List<Order>());
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> OrderDetails(int id)
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogError("Email claim not found in user claims");
                    return Unauthorized("Invalid user identifier.");
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null) return NotFound();

                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.Payments)
                    .Include(o => o.UserAddress)
                    .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == user.UserID);

                if (order == null) return NotFound();

                var payment = order.Payments?.FirstOrDefault();
                var result = new
                {
                    orderId = order.OrderID,
                    createdAt = order.CreatedAt,
                    status = order.Status,
                    totalPrice = order.TotalPrice,
                    productDiscountAmount = order.ProductDiscountAmount,
                    rankDiscountAmount = order.RankDiscountAmount,
                    voucherDiscountAmount = order.VoucherDiscountAmount,
                    totalDiscountAmount = order.TotalDiscountAmount,
                    finalPrice = order.FinalPrice,
                    paymentMethod = payment?.PaymentMethod,
                    paymentStatus = payment?.PaymentStatus,
                    paymentDate = payment?.PaymentDate,
                    address = order.UserAddress != null ? new
                    {
                        recipientName = order.UserAddress.RecipientName,
                        addressLine = order.UserAddress.AddressLine,
                        city = order.UserAddress.City,
                        district = order.UserAddress.District,
                        ward = order.UserAddress.Ward,
                        phone = order.UserAddress.Phone
                    } : null,
                    orderItems = order.OrderItems.Select(oi => new
                    {
                        orderItemId = oi.OrderItemID,
                        quantity = oi.Quantity,
                        unitPrice = oi.UnitPrice,
                        finalSubtotal = oi.FinalSubtotal,
                        product = new
                        {
                            name = oi.Product.Name,
                            imageUrl = oi.Product.ProductImages.FirstOrDefault()?.ImageURL ?? "/images/no-image.png"
                        }
                    })
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details for order {OrderId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải thông tin đơn hàng" });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogError("Email claim not found in user claims");
                    return Unauthorized("Invalid user identifier.");
                }

                // Tải user với AsTracking() để đảm bảo Entity Framework theo dõi thay đổi
                var user = await _context.Users
                    .AsTracking()
                    .FirstOrDefaultAsync(u => u.Email == emailClaim.Value);

                if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng" });

                // Tải order với AsTracking() để đảm bảo Entity Framework theo dõi thay đổi
                var order = await _context.Orders
                    .AsTracking()
                    .Include(o => o.Payments)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == user.UserID);

                if (order == null) return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

                var payment = order.Payments?.FirstOrDefault();
                var hasSuccessfulPayment = false;

                if (payment != null)
                {
                    // Với cả VNPay và COD, cần xử lý hủy đơn hàng và cập nhật TotalSpent, OrderCount
                    // Bất kể trạng thái thanh toán là "Pending" hay "Completed"
                    hasSuccessfulPayment = payment.PaymentStatus == "Pending" || payment.PaymentStatus == "Completed";

                    _logger.LogInformation("Payment method: {PaymentMethod}, Payment status: {PaymentStatus}, hasSuccessfulPayment: {HasSuccessfulPayment}",
                        payment.PaymentMethod, payment.PaymentStatus, hasSuccessfulPayment);
                }

                var timeSinceOrder = DateTime.Now - order.CreatedAt;

                // Hủy được trong 1 giờ kể từ khi đặt hàng với trạng thái chờ xử lý hoặc đang xử lý
                if ((order.Status != "Pending" && order.Status != "Processing") || timeSinceOrder.TotalHours > 1)
                {
                    return Json(new
                    {
                        success = false,
                        message = timeSinceOrder.TotalHours > 1
                            ? "Không thể hủy đơn hàng sau 1 giờ kể từ khi đặt hàng"
                            : "Chỉ có thể hủy đơn hàng đang ở trạng thái chờ xử lý hoặc đang xử lý"
                    });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Update order status
                    order.Status = "Cancelled";

                    // If payment was pending or completed, mark it as failed
                    var pendingOrCompletedPayment = order.Payments?.FirstOrDefault(p => p.PaymentStatus == "Pending" || p.PaymentStatus == "Completed");
                    if (pendingOrCompletedPayment != null)
                    {
                        pendingOrCompletedPayment.PaymentStatus = "Failed";
                        pendingOrCompletedPayment.PaymentDate = DateTime.Now;
                        _logger.LogInformation("Payment {PaymentId} for order {OrderId} marked as Failed", pendingOrCompletedPayment.PaymentID, order.OrderID);
                    }

                    // Restore product quantities
                    foreach (var orderItem in order.OrderItems)
                    {
                        var product = orderItem.Product;
                        if (product != null)
                        {
                            product.Stock += orderItem.Quantity;
                        }
                    }

                    // Return vouchers used in this order
                    var usedVouchers = await _context.UserVouchers
                        .Include(uv => uv.Voucher)
                        .Where(uv => uv.OrderID == order.OrderID && uv.IsUsed)
                        .ToListAsync();

                    foreach (var userVoucher in usedVouchers)
                    {
                        // Reset voucher status
                        userVoucher.IsUsed = false;
                        userVoucher.UsedDate = null;
                        userVoucher.OrderID = null;

                        // If voucher has a quantity limit, increment it
                        if (userVoucher.Voucher.QuantityAvailable.HasValue)
                        {
                            userVoucher.Voucher.QuantityAvailable++;
                        }
                    }

                    _logger.LogInformation("[Before] User {UserId} TotalSpent: {TotalSpent}, OrderCount: {OrderCount}", user.UserID, user.TotalSpent, user.OrderCount);
                    _logger.LogDebug("hasSuccessfulPayment: {hasSuccessfulPayment}", hasSuccessfulPayment);

                    if (hasSuccessfulPayment)
                    {
                        // Ensure TotalSpent doesn't go negative
                        user.TotalSpent = Math.Max(0, user.TotalSpent - order.FinalPrice);
                        user.OrderCount = Math.Max(0, user.OrderCount - 1);

                        // Log sau khi thay đổi giá trị
                        _logger.LogInformation("[After] User {UserId} TotalSpent: {TotalSpent}, OrderCount: {OrderCount}", user.UserID, user.TotalSpent, user.OrderCount);

                        // Recalculate user's rank based on new TotalSpent
                        var newRank = await _context.Ranks
                            .Where(r => r.MinTotalSpent <= user.TotalSpent)
                            .OrderByDescending(r => r.MinTotalSpent)
                            .FirstOrDefaultAsync();

                        if (newRank != null && user.RankId != newRank.RankId)
                        {
                            user.RankId = newRank.RankId;
                            _logger.LogInformation("User {UserId} rank updated to {RankId} after order cancellation", user.UserID, newRank.RankId);
                        }

                        // Đảm bảo thay đổi được lưu vào cơ sở dữ liệu
                        _context.Users.Update(user);
                        _logger.LogInformation("Updating user in database: UserId={UserId}, TotalSpent={TotalSpent}, OrderCount={OrderCount}",
                            user.UserID, user.TotalSpent, user.OrderCount);
                    }

                    try
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Changes saved to database successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving changes to database");
                        throw;
                    }

                    await transaction.CommitAsync();
                    _logger.LogInformation("Transaction committed successfully");

                    _logger.LogInformation("Successfully cancelled order {OrderId} and restored product quantities", order.OrderID);
                    return Json(new { success = true, message = "Hủy đơn hàng thành công" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error cancelling order {OrderId}", id);
                    return Json(new { success = false, message = "Có lỗi xảy ra khi hủy đơn hàng" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi hủy đơn hàng" });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Addresses()
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogError("Email claim not found in user claims");
                    return Unauthorized("Invalid user identifier.");
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null) return NotFound();

                await SetCommonViewBagProperties(user);

                var addresses = await _userService.GetUserAddressesAsync(user.UserID);
                var addressCount = await _userService.GetAddressCountAsync(user.UserID);
                var canAddMore = await _userService.CanAddMoreAddressesAsync(user.UserID);

                ViewBag.AddressCount = addressCount;
                ViewBag.CanAddMore = canAddMore;

                return View(addresses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading addresses for user {Email}", User.Identity.Name);
                return View(new List<UserAddress>());
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(UserAddress address)
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogError("Email claim not found in user claims");
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    _logger.LogError("User not found for email: {Email}", emailClaim.Value);
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                if (!await _userService.CanAddMoreAddressesAsync(user.UserID))
                {
                    _logger.LogWarning("User {Email} has reached address limit", emailClaim.Value);
                    return Json(new { success = false, message = "Bạn đã đạt đến giới hạn 6 địa chỉ" });
                }

                address.UserID = user.UserID;
                var success = await _userService.AddAddressAsync(address);

                if (!success)
                {
                    _logger.LogError("Failed to add address for user {Email}", emailClaim.Value);
                    return Json(new { success = false, message = "Không thể thêm địa chỉ" });
                }

                return Json(new { success = true, message = "Thêm địa chỉ thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm địa chỉ" });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddress(UserAddress address)
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogError("Email claim not found in user claims");
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    _logger.LogError("User not found for email: {Email}", emailClaim.Value);
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                var existingAddress = await _userService.GetAddressByIdAsync(address.AddressID, user.UserID);
                if (existingAddress == null)
                {
                    _logger.LogWarning("Address {AddressId} not found for user {Email}", address.AddressID, emailClaim.Value);
                    return Json(new { success = false, message = "Không tìm thấy địa chỉ" });
                }

                address.UserID = user.UserID;
                var success = await _userService.UpdateAddressAsync(address);

                if (!success)
                {
                    _logger.LogError("Failed to update address {AddressId} for user {Email}", address.AddressID, emailClaim.Value);
                    return Json(new { success = false, message = "Không thể cập nhật địa chỉ" });
                }

                return Json(new { success = true, message = "Cập nhật địa chỉ thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật địa chỉ" });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogError("Email claim not found in user claims");
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    _logger.LogError("User not found for email: {Email}", emailClaim.Value);
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                var address = await _userService.GetAddressByIdAsync(id, user.UserID);
                if (address == null)
                {
                    _logger.LogWarning("Address {AddressId} not found for user {Email}", id, emailClaim.Value);
                    return Json(new { success = false, message = "Không tìm thấy địa chỉ" });
                }

                if (address.IsPrimary)
                {
                    _logger.LogWarning("Cannot delete primary address {AddressId} for user {Email}", id, emailClaim.Value);
                    return Json(new { success = false, message = "Không thể xóa địa chỉ mặc định" });
                }

                var success = await _userService.DeleteAddressAsync(id, user.UserID);

                if (!success)
                {
                    _logger.LogError("Failed to delete address {AddressId} for user {Email}", id, emailClaim.Value);
                    return Json(new { success = false, message = "Không thể xóa địa chỉ" });
                }

                _logger.LogInformation("Successfully deleted address {AddressId} for user {Email}", id, emailClaim.Value);
                return Json(new { success = true, message = "Xóa địa chỉ thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa địa chỉ" });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimaryAddress(int id)
        {
            try
            {
                var emailClaim = User.FindFirst(ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogWarning("User email claim not found");
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for email: {emailClaim.Value}");
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                var address = await _userService.GetAddressByIdAsync(id, user.UserID);
                if (address == null)
                {
                    _logger.LogWarning($"Address {id} not found for user {user.UserID}");
                    return Json(new { success = false, message = "Không tìm thấy địa chỉ" });
                }

                if (address.IsPrimary)
                {
                    return Json(new { success = true, message = "Địa chỉ này đã là mặc định" });
                }

                var result = await _userService.SetPrimaryAddressAsync(id, user.UserID);
                if (result)
                {
                    _logger.LogInformation($"Set address {id} as primary for user {user.UserID}");
                    return Json(new { success = true, message = "Đặt địa chỉ mặc định thành công" });
                }
                else
                {
                    _logger.LogWarning($"Failed to set address {id} as primary for user {user.UserID}");
                    return Json(new { success = false, message = "Không thể đặt địa chỉ mặc định" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting primary address {id}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi đặt địa chỉ mặc định" });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            if (emailClaim == null)
            {
                _logger.LogError("Email claim not found in user claims");
                return Unauthorized("Invalid user identifier.");
            }

            _logger.LogInformation("Email claim found: {Email}", emailClaim.Value);

            var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
            if (!await _userService.HasPasswordAuthAsync(user.Email))
                return Json(new { success = false, message = "Tài khoản Google/Facebook không thể đổi mật khẩu" });

            var authMethod = await _context.UserAuthMethods
                .FirstOrDefaultAsync(uam => uam.UserID == user.UserID && uam.AuthType == "Password");

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, authMethod.AuthKey))
                return Json(new { success = false, message = "Mật khẩu hiện tại không đúng" });

            authMethod.AuthKey = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đổi mật khẩu thành công" });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Wishlist(int page = 1)
        {
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            if (emailClaim == null)
            {
                _logger.LogError("Email claim not found in user claims");
                return Unauthorized("Invalid user identifier.");
            }

            var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
            if (user == null) return NotFound();

            // Ensure user has a rank
            if (user.RankId == null)
            {
                user.RankId = 1;
                await _context.SaveChangesAsync();
            }

            await SetCommonViewBagProperties(user);

            const int pageSize = 6;

            // Lấy danh sách các items trong wishlist trực tiếp từ user
            var query = _context.WishlistItems
                .Include(wi => wi.Product)
                    .ThenInclude(p => p.ProductImages)
                .Where(wi => wi.UserID == user.UserID);

            // Get total count before pagination
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Ensure page is within valid range
            page = Math.Max(1, Math.Min(page, totalPages));

            // Apply pagination
            var wishlistItems = await query
                .OrderByDescending(w => w.AddedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(wishlistItems);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> IsInWishlist(int productId)
        {
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            if (emailClaim == null)
            {
                _logger.LogError("Email claim not found in user claims");
                return Json(new { success = false, message = "Invalid user identifier." });
            }

            var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            var isInWishlist = await _userService.IsInWishlist(user.UserID.ToString(), productId);
            return Json(isInWishlist);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleWishlist(int productId)
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogError("Email claim not found in user claims");
                    return Json(new { success = false, message = "Invalid user identifier." });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                var isInWishlist = await _userService.IsInWishlist(user.UserID.ToString(), productId);
                if (isInWishlist)
                {
                    var success = await _userService.RemoveFromWishlist(user.UserID.ToString(), productId);
                    if (success)
                    {
                        return Json(new { success = true, message = "Removed from wishlist." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Failed to remove from wishlist." });
                    }
                }
                else
                {
                    var success = await _userService.AddToWishlist(user.UserID.ToString(), productId);
                    if (success)
                    {
                        return Json(new { success = true, message = "Added to wishlist." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Failed to add to wishlist." });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling wishlist for product {ProductId}", productId);
                return Json(new { success = false, message = "An error occurred while updating wishlist." });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetActiveVoucherCount()
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var activeVoucherCount = await _context.UserVouchers
                    .CountAsync(uv => uv.UserID == user.UserID &&
                           !uv.IsUsed &&
                           uv.Voucher.IsActive &&
                           uv.Voucher.ValidTo > DateTime.Now);

                return Json(new { success = true, count = activeVoucherCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active voucher count");
                return Json(new { success = false, message = "Error getting voucher count" });
            }
        }

        private async Task SignInUserAsync(User user, bool isPersistent)
        {
            // Get user's auth methods
            var authMethods = await _context.UserAuthMethods
                .Where(uam => uam.UserID == user.UserID)
                .ToListAsync();

            // Check if user has external auth methods
            var hasExternalAuth = authMethods.Any(uam => uam.AuthType == "Google" || uam.AuthType == "Facebook");

            // If user has external auth and no profile image, try to get it from the auth method
            if (hasExternalAuth && string.IsNullOrEmpty(user.ProfileImageURL))
            {
                var externalAuth = authMethods.FirstOrDefault(uam => uam.AuthType == "Google" || uam.AuthType == "Facebook");
                if (externalAuth != null)
                {
                    await _context.SaveChangesAsync();
                }
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("Username", user.Username),
                new Claim("ProfileImage", user.ProfileImageURL ?? "/images/default-avatar.png"),
                new Claim("Phone", user.Phone ?? ""),
                new Claim("TotalSpent", user.TotalSpent.ToString()),
                new Claim("OrderCount", user.OrderCount.ToString()),
                new Claim("RankId", user.RankId?.ToString() ?? "1"),
                new Claim("EmailVerified", user.EmailVerified.ToString()),
                new Claim("UserStatus", user.UserStatus),
                new Claim("CreatedAt", user.CreatedAt.ToString("o")),
                new Claim("Gender", user.Gender?.ToString() ?? ""),
                new Claim("DateOfBirth", user.DateOfBirth?.ToString("o") ?? "")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent ? DateTime.UtcNow.AddDays(30) : null
            });

            // Store all user information in session
            HttpContext.Session.SetString("UserId", user.UserID.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("Name", user.Name);
            HttpContext.Session.SetString("Role", user.Role.ToString());
            HttpContext.Session.SetString("ProfileImage", user.ProfileImageURL ?? "/images/default-avatar.png");
            HttpContext.Session.SetString("Phone", user.Phone ?? "");
            HttpContext.Session.SetString("TotalSpent", user.TotalSpent.ToString());
            HttpContext.Session.SetString("OrderCount", user.OrderCount.ToString());
            HttpContext.Session.SetString("RankId", user.RankId?.ToString() ?? "1");
            HttpContext.Session.SetString("EmailVerified", user.EmailVerified.ToString());
            HttpContext.Session.SetString("UserStatus", user.UserStatus);
            HttpContext.Session.SetString("CreatedAt", user.CreatedAt.ToString("o"));
            HttpContext.Session.SetString("Gender", user.Gender?.ToString() ?? "");
            HttpContext.Session.SetString("DateOfBirth", user.DateOfBirth?.ToString("o") ?? "");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ClaimVoucher(string code, int userId)
        {
            try
            {
                // Verify the current user is the same as the userId in the link
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogError("Email claim not found in user claims");
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng";
                    return RedirectToAction("Vouchers");
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng";
                    return RedirectToAction("Vouchers");
                }

                // Check if the user in the link matches the logged-in user
                if (user.UserID != userId)
                {
                    _logger.LogWarning("User {LoggedInUserId} attempted to claim a voucher for user {TargetUserId}", user.UserID, userId);
                    TempData["ErrorMessage"] = "Bạn không có quyền nhận voucher này";
                    return RedirectToAction("Vouchers");
                }

                // Find the voucher by code
                var voucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.Code == code && v.IsActive && v.ValidTo > DateTime.Now);

                if (voucher == null)
                {
                    TempData["ErrorMessage"] = "Mã giảm giá không hợp lệ hoặc đã hết hạn";
                    return RedirectToAction("Vouchers");
                }

                // Check if voucher is system-wide - only system-wide vouchers can be claimed by users
                if (!voucher.IsSystemWide)
                {
                    TempData["ErrorMessage"] = "Mã giảm giá này không thể được sử dụng trực tiếp";
                    return RedirectToAction("Vouchers");
                }

                // Check if voucher has available quantity
                if (voucher.QuantityAvailable.HasValue && voucher.QuantityAvailable <= 0)
                {
                    TempData["ErrorMessage"] = "Mã giảm giá đã hết lượt sử dụng";
                    return RedirectToAction("Vouchers");
                }

                // Check if user already has this voucher
                var existingUserVoucher = await _context.UserVouchers
                    .FirstOrDefaultAsync(uv => uv.UserID == user.UserID && uv.VoucherID == voucher.VoucherID);

                if (existingUserVoucher != null)
                {
                    if (existingUserVoucher.IsUsed)
                    {
                        TempData["ErrorMessage"] = "Bạn đã sử dụng mã giảm giá này";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Bạn đã có mã giảm giá này trong kho voucher";
                    }
                    return RedirectToAction("Vouchers");
                }

                // Assign voucher to user
                var userVoucher = new UserVoucher
                {
                    UserID = user.UserID,
                    VoucherID = voucher.VoucherID,
                    AssignedDate = DateTime.Now,
                    IsUsed = false
                };

                _context.UserVouchers.Add(userVoucher);

                // Decrease available quantity if applicable
                if (voucher.QuantityAvailable.HasValue)
                {
                    voucher.QuantityAvailable--;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã thêm mã giảm giá vào kho voucher thành công";
                return RedirectToAction("Vouchers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error claiming voucher {VoucherCode}", code);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi nhận mã giảm giá";
                return RedirectToAction("Vouchers");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Vouchers(int page = 1, string tab = "active")
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogError("Email claim not found in user claims");
                    return Unauthorized("Invalid user identifier.");
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null) return NotFound();

                // Ensure user has a rank
                if (user.RankId == null)
                {
                    user.RankId = 1;
                    await _context.SaveChangesAsync();
                }

                await SetCommonViewBagProperties(user);

                const int pageSize = 6; // Number of vouchers per page

                // Get total counts for badges
                var activeCount = await _context.UserVouchers
                    .CountAsync(uv => uv.UserID == user.UserID &&
                           !uv.IsUsed &&
                           uv.Voucher.IsActive &&
                           uv.Voucher.ValidTo > DateTime.Now);

                var expiredCount = await _context.UserVouchers
                    .CountAsync(uv => uv.UserID == user.UserID &&
                           (!uv.IsUsed && (uv.Voucher.ValidTo <= DateTime.Now || !uv.Voucher.IsActive)));

                var usedCount = await _context.UserVouchers
                    .CountAsync(uv => uv.UserID == user.UserID && uv.IsUsed);

                ViewBag.ActiveCount = activeCount;
                ViewBag.ExpiredCount = expiredCount;
                ViewBag.UsedCount = usedCount;

                // Get paginated data based on selected tab
                IQueryable<UserVoucher> query;
                int totalItems;

                switch (tab.ToLower())
                {
                    case "expired":
                        query = _context.UserVouchers
                            .Include(uv => uv.Voucher)
                            .Where(uv => uv.UserID == user.UserID &&
                                   (!uv.IsUsed && (uv.Voucher.ValidTo <= DateTime.Now || !uv.Voucher.IsActive)))
                            .OrderByDescending(uv => uv.Voucher.ValidTo);
                        totalItems = expiredCount;
                        break;

                    case "used":
                        query = _context.UserVouchers
                            .Include(uv => uv.Voucher)
                            .Include(uv => uv.Order)
                            .Where(uv => uv.UserID == user.UserID && uv.IsUsed)
                            .OrderByDescending(uv => uv.UsedDate);
                        totalItems = usedCount;
                        break;

                    default: // "active"
                        query = _context.UserVouchers
                            .Include(uv => uv.Voucher)
                            .Where(uv => uv.UserID == user.UserID &&
                                   !uv.IsUsed &&
                                   uv.Voucher.IsActive &&
                                   uv.Voucher.ValidTo > DateTime.Now)
                            .OrderByDescending(uv => uv.Voucher.ValidTo);
                        totalItems = activeCount;
                        break;
                }

                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Set view data
                ViewBag.CurrentTab = tab;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;
                ViewBag.PageSize = pageSize;
                ViewBag.StartItem = totalItems == 0 ? 0 : ((page - 1) * pageSize) + 1;
                ViewBag.EndItem = Math.Min(page * pageSize, totalItems);

                switch (tab.ToLower())
                {
                    case "expired":
                        ViewBag.ExpiredVouchers = items;
                        ViewBag.ActiveVouchers = new List<UserVoucher>();
                        ViewBag.UsedVouchers = new List<UserVoucher>();
                        break;
                    case "used":
                        ViewBag.UsedVouchers = items;
                        ViewBag.ActiveVouchers = new List<UserVoucher>();
                        ViewBag.ExpiredVouchers = new List<UserVoucher>();
                        break;
                    default:
                        ViewBag.ActiveVouchers = items;
                        ViewBag.ExpiredVouchers = new List<UserVoucher>();
                        ViewBag.UsedVouchers = new List<UserVoucher>();
                        break;
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vouchers for user {Email}", User.Identity.Name);
                return View(new List<UserVoucher>());
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyVoucherCode(string voucherCode)
        {
            try
            {
                if (string.IsNullOrEmpty(voucherCode))
                {
                    return Json(new { success = false, message = "Vui lòng nhập mã giảm giá" });
                }

                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogError("Email claim not found in user claims");
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                // Find the voucher by code
                var voucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.Code == voucherCode && v.IsActive && v.ValidTo > DateTime.Now);

                if (voucher == null)
                {
                    return Json(new { success = false, message = "Mã giảm giá không hợp lệ hoặc đã hết hạn" });
                }

                // Check if voucher is system-wide - only system-wide vouchers can be claimed by users
                if (!voucher.IsSystemWide)
                {
                    return Json(new { success = false, message = "Mã giảm giá này không thể được sử dụng trực tiếp" });
                }

                // Check if voucher has available quantity
                if (voucher.QuantityAvailable.HasValue && voucher.QuantityAvailable <= 0)
                {
                    return Json(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng" });
                }

                // Check if user already has this voucher
                var existingUserVoucher = await _context.UserVouchers
                    .FirstOrDefaultAsync(uv => uv.UserID == user.UserID && uv.VoucherID == voucher.VoucherID);

                if (existingUserVoucher != null)
                {
                    if (existingUserVoucher.IsUsed)
                    {
                        return Json(new { success = false, message = "Bạn đã sử dụng mã giảm giá này" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Bạn đã có mã giảm giá này trong kho voucher" });
                    }
                }

                // Assign voucher to user
                var userVoucher = new UserVoucher
                {
                    UserID = user.UserID,
                    VoucherID = voucher.VoucherID,
                    AssignedDate = DateTime.Now,
                    IsUsed = false
                };

                _context.UserVouchers.Add(userVoucher);

                // Decrease available quantity if applicable
                if (voucher.QuantityAvailable.HasValue)
                {
                    voucher.QuantityAvailable--;
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Đã thêm mã giảm giá vào kho voucher thành công",
                    voucher = new
                    {
                        code = voucher.Code,
                        discountType = voucher.DiscountType,
                        discountValue = voucher.DiscountValue,
                        validTo = voucher.ValidTo.ToString("dd/MM/yyyy")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying voucher code {VoucherCode}", voucherCode);
                return Json(new { success = false, message = "Có lỗi xảy ra khi áp dụng mã giảm giá" });
            }
        }

        [HttpGet]
        public IActionResult RegistrationSuccess()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Token không hợp lệ";
                return RedirectToAction("Login");
            }

            var isValid = await _userService.IsEmailVerificationTokenValidAsync(token);
            if (!isValid)
            {
                TempData["ErrorMessage"] = "Link xác minh không hợp lệ hoặc đã hết hạn";
                return RedirectToAction("Login");
            }

            var success = await _userService.VerifyEmailAsync(token);
            if (!success)
            {
                TempData["ErrorMessage"] = "Không thể xác minh email. Vui lòng thử lại sau.";
                return RedirectToAction("Login");
            }

            TempData["SuccessMessage"] = "Email đã được xác minh thành công. Bạn có thể đăng nhập ngay bây giờ.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerificationEmail()
        {
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            if (emailClaim == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
            }

            var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
            }

            if (user.EmailVerified)
            {
                return Json(new { success = false, message = "Email đã được xác minh" });
            }

            // Check if user has password auth method
            var hasPasswordAuth = await _userService.HasPasswordAuthAsync(user.Email);
            if (!hasPasswordAuth)
            {
                return Json(new { success = false, message = "Tài khoản này không cần xác minh email" });
            }

            var success = await _userService.ResendVerificationEmailAsync(user.UserID);
            if (!success)
            {
                return Json(new { success = false, message = "Không thể gửi email xác minh. Vui lòng thử lại sau." });
            }

            return Json(new { success = true, message = "Email xác minh đã được gửi lại. Vui lòng kiểm tra hộp thư của bạn." });
        }
    }
}