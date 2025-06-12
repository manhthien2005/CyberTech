using CyberTech.Data;
using CyberTech.Models;
using CyberTech.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace CyberTech.Controllers
{
    [Authorize(Roles = "Admin,Staff,SuperAdmin")]
    [Route("EmployeeManage")]
    public class EmployeeManageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;

        public EmployeeManageController(ApplicationDbContext context, IUserService userService, IEmailService emailService)
        {
            _context = context;
            _userService = userService;
            _emailService = emailService;
        }

        private bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return true; // Phone is optional
            var phoneRegex = new Regex(@"^(0|\+84)([0-9]{9,10})$");
            return phoneRegex.IsMatch(phone.Trim());
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("GetEmployees")]
        public async Task<IActionResult> GetEmployees(int page = 1, int pageSize = 10, string searchTerm = "", string sortBy = "", string status = "")
        {
            try
            {
                var query = _context.Users
                    .Where(u => u.Role == "Support" || u.Role == "Manager" || u.Role == "SuperAdmin")
                    .AsQueryable();

                // Apply search
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(u => u.Name.ToLower().Contains(searchTerm) ||
                                          u.Email.ToLower().Contains(searchTerm) ||
                                          u.Username.ToLower().Contains(searchTerm) ||
                                          (u.Phone != null && u.Phone.Contains(searchTerm)));
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    if (status == "Active" || status == "Inactive" || status == "Suspended")
                    {
                        query = query.Where(u => u.UserStatus == status);
                    }
                }

                // Apply sorting
                query = sortBy switch
                {
                    "name_asc" => query.OrderBy(u => u.Name),
                    "name_desc" => query.OrderByDescending(u => u.Name),
                    "email_asc" => query.OrderBy(u => u.Email),
                    "email_desc" => query.OrderByDescending(u => u.Email),
                    "date_asc" => query.OrderBy(u => u.CreatedAt),
                    "date_desc" => query.OrderByDescending(u => u.CreatedAt),
                    "role_asc" => query.OrderBy(u => u.Role),
                    "role_desc" => query.OrderByDescending(u => u.Role),
                    _ => query.OrderByDescending(u => u.CreatedAt)
                };

                // Calculate pagination
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var employees = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new EmployeeViewModel
                    {
                        UserId = u.UserID,
                        Name = u.Name,
                        Username = u.Username,
                        Email = u.Email,
                        Phone = u.Phone,
                        ProfileImageURL = u.ProfileImageURL,
                        Role = u.Role,
                        Salary = u.Salary,
                        Status = u.UserStatus,
                        CreatedAt = u.CreatedAt,
                        Gender = u.Gender,
                        DateOfBirth = u.DateOfBirth,
                        AuthMethods = u.AuthMethods.Select(am => am.AuthType).ToList()
                    })
                    .ToListAsync();

                var pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalItems,
                    totalPages
                };

                return Json(new { success = true, data = employees, pagination });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải danh sách nhân viên: " + ex.Message });
            }
        }

        [HttpGet("GetEmployee/{id}")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            try
            {
                var employee = await _context.Users
                    .Include(u => u.AuthMethods)
                    .Where(u => u.UserID == id && (u.Role == "Support" || u.Role == "Manager" || u.Role == "SuperAdmin"))
                    .Select(u => new EmployeeViewModel
                    {
                        UserId = u.UserID,
                        Name = u.Name,
                        Username = u.Username,
                        Email = u.Email,
                        Phone = u.Phone,
                        ProfileImageURL = u.ProfileImageURL,
                        Role = u.Role,
                        Salary = u.Salary,
                        Status = u.UserStatus,
                        CreatedAt = u.CreatedAt,
                        Gender = u.Gender,
                        DateOfBirth = u.DateOfBirth,
                        AuthMethods = u.AuthMethods.Select(am => am.AuthType).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (employee == null)
                    return Json(new { success = false, message = "Không tìm thấy nhân viên" });

                return Json(new { success = true, data = employee });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải thông tin nhân viên: " + ex.Message });
            }
        }

        [HttpPost("UpdateEmployee")]
        public async Task<IActionResult> UpdateEmployee([FromBody] UpdateEmployeeRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
                }

                if (!string.IsNullOrEmpty(request.Phone) && !IsValidPhoneNumber(request.Phone))
                {
                    return Json(new { success = false, message = "Số điện thoại không hợp lệ" });
                }

                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null || !(user.Role == "Support" || user.Role == "Manager" || user.Role == "SuperAdmin"))
                    return Json(new { success = false, message = "Không tìm thấy nhân viên" });

                // Prevent self-disabling
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (request.UserId == currentUserId && request.Status != "Active")
                {
                    return Json(new { success = false, message = "Không thể vô hiệu hóa hoặc tạm khóa tài khoản của chính bạn" });
                }

                user.Name = request.Name;
                user.Phone = request.Phone?.Trim();
                user.Gender = request.Gender;
                user.DateOfBirth = request.DateOfBirth;
                user.UserStatus = request.Status;
                user.Role = request.Role;
                user.Salary = request.Salary;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật thông tin nhân viên thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật thông tin nhân viên: " + ex.Message });
            }
        }

        [HttpPost("CreateEmployee")]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
                }

                if (!string.IsNullOrEmpty(request.Phone) && !IsValidPhoneNumber(request.Phone))
                {
                    return Json(new { success = false, message = "Số điện thoại không hợp lệ" });
                }

                var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
                if (emailExists)
                    return Json(new { success = false, message = "Email đã tồn tại trong hệ thống" });

                var usernameExists = await _context.Users.AnyAsync(u => u.Username == request.Username);
                if (usernameExists)
                    return Json(new { success = false, message = "Tên đăng nhập đã tồn tại trong hệ thống" });

                var result = await _userService.RegisterAsync(
                    request.Name,
                    request.Username,
                    request.Email,
                    request.Password
                );

                if (result)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
                    if (user != null)
                    {
                        user.Phone = request.Phone?.Trim();
                        user.Role = request.Role;
                        user.Salary = request.Salary;
                        await _context.SaveChangesAsync();
                    }
                    return Json(new { success = true, message = "Tạo nhân viên mới thành công" });
                }
                else
                    return Json(new { success = false, message = "Không thể tạo nhân viên mới" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi tạo nhân viên mới: " + ex.Message });
            }
        }
    }

    // DTOs for Employee Management
    public class EmployeeViewModel
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string ProfileImageURL { get; set; }
        public string Role { get; set; }
        public decimal? Salary { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public byte? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public List<string> AuthMethods { get; set; } = new List<string>();
    }

    public class UpdateEmployeeRequest
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public byte? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Status { get; set; }
        public string Role { get; set; }
        public decimal? Salary { get; set; }
    }

    public class CreateEmployeeRequest
    {
        public string Name { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public decimal? Salary { get; set; }
    }
}