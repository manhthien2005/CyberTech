using CyberTech.Data;
using CyberTech.Models;
using CyberTech.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace CyberTech.Controllers
{
    [Authorize(Roles = "Admin,Staff,Support,Manager,SuperAdmin")]
    [Route("OrderManage")]
    public class OrderManageController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderManageController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("GetOrders")]
        public async Task<IActionResult> GetOrders(int page = 1, int pageSize = 5, string searchTerm = "", string sortBy = "", string status = "", string startDate = "", string endDate = "")
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.User)
                    .AsQueryable();

                // Apply search
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(o => o.OrderID.ToString().Contains(searchTerm) ||
                                          o.User.Name.ToLower().Contains(searchTerm) ||
                                          o.User.Email.ToLower().Contains(searchTerm));
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(o => o.Status == status);
                }

                // Apply date range filter
                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out DateTime startDateTime))
                {
                    query = query.Where(o => o.CreatedAt >= startDateTime);
                }

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out DateTime endDateTime))
                {
                    // Add one day to include the end date fully
                    endDateTime = endDateTime.AddDays(1).AddSeconds(-1);
                    query = query.Where(o => o.CreatedAt <= endDateTime);
                }

                // Apply sorting
                query = sortBy switch
                {
                    "id_asc" => query.OrderBy(o => o.OrderID),
                    "id_desc" => query.OrderByDescending(o => o.OrderID),
                    "date_asc" => query.OrderBy(o => o.CreatedAt),
                    "date_desc" => query.OrderByDescending(o => o.CreatedAt),
                    "total_asc" => query.OrderBy(o => o.FinalPrice),
                    "total_desc" => query.OrderByDescending(o => o.FinalPrice),
                    _ => query.OrderByDescending(o => o.CreatedAt)
                };

                // Calculate pagination
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var orders = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new OrderViewModel
                    {
                        OrderId = o.OrderID,
                        TotalPrice = o.TotalPrice,
                        FinalPrice = o.FinalPrice,
                        Status = o.Status,
                        CreatedAt = o.CreatedAt,
                        TotalDiscountAmount = o.TotalDiscountAmount,
                        ItemCount = o.OrderItems.Count
                    })
                    .ToListAsync();

                var pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalItems,
                    totalPages
                };

                return Json(new { success = true, data = orders, pagination });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải danh sách đơn hàng: " + ex.Message });
            }
        }

        [HttpGet("GetOrder/{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .Include(o => o.UserAddress)
                    .Include(o => o.User)
                    .Where(o => o.OrderID == id)
                    .Select(o => new OrderDetailViewModel
                    {
                        OrderId = o.OrderID,
                        TotalPrice = o.TotalPrice,
                        FinalPrice = o.FinalPrice,
                        Status = o.Status,
                        CreatedAt = o.CreatedAt,
                        TotalDiscountAmount = o.TotalDiscountAmount,
                        RankDiscountAmount = o.RankDiscountAmount,
                        VoucherDiscountAmount = o.VoucherDiscountAmount,
                        ProductDiscountAmount = o.ProductDiscountAmount,
                        RecipientName = o.UserAddress != null ? o.UserAddress.RecipientName : o.User.Name,
                        RecipientPhone = o.UserAddress != null ? o.UserAddress.Phone : o.User.Phone,
                        ShippingAddress = o.UserAddress != null ? $"{o.UserAddress.AddressLine}, {o.UserAddress.Ward}, {o.UserAddress.District}, {o.UserAddress.City}" : "N/A",
                        OrderItems = o.OrderItems.Select(oi => new OrderItemViewModel
                        {
                            OrderItemId = oi.OrderItemID,
                            ProductId = oi.ProductID,
                            ProductName = oi.Product.Name,
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPrice,
                            Subtotal = oi.Subtotal,
                            DiscountAmount = oi.DiscountAmount,
                            FinalSubtotal = oi.FinalSubtotal
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (order == null)
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

                return Json(new { success = true, data = order });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải chi tiết đơn hàng: " + ex.Message });
            }
        }

        [HttpPost("UpdateOrder")]
        public async Task<IActionResult> UpdateOrder([FromBody] UpdateOrderRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                var order = await _context.Orders.FindAsync(request.OrderId);
                if (order == null)
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

                order.Status = request.Status;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật trạng thái đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật trạng thái đơn hàng: " + ex.Message });
            }
        }

        [HttpPost("DeleteOrder")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

                order.Status = "Cancelled"; // Mark as cancelled instead of deleting
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Hủy đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi hủy đơn hàng: " + ex.Message });
            }
        }
    }

    public class UpdateOrderRequest
    {
        public int OrderId { get; set; }
        public string Status { get; set; }
    }
}