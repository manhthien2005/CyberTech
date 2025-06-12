using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CyberTech.Data;
using CyberTech.ViewModels;
using CyberTech.Models;
using Microsoft.AspNetCore.Authorization;

namespace CyberTechShop.Controllers
{
    [Authorize(Roles = "Admin,Staff,Support,Manager,SuperAdmin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var dashboardData = new DashboardViewModel();

            // Tính toán các thống kê chính
            await CalculateMainStatistics(dashboardData);
            
            // Dữ liệu biểu đồ 7 ngày gần đây (mặc định)
            await CalculateRevenueChart(dashboardData, "7days");
            
            // Đơn hàng gần đây
            await LoadRecentOrders(dashboardData);
            
            // Sản phẩm bán chạy
            await LoadTopProducts(dashboardData);

            return View(dashboardData);
        }

        [HttpGet]
        public async Task<IActionResult> GetRevenueData(string period = "7days")
        {
            var chartData = new List<ChartDataPoint>();
            await CalculateRevenueChartByPeriod(chartData, period);
            return Json(chartData);
        }

        private async Task CalculateMainStatistics(DashboardViewModel model)
        {
            var now = DateTime.Now;
            var thisMonth = new DateTime(now.Year, now.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);
            var today = now.Date;

            // Tổng doanh thu (tất cả đơn hàng đã hoàn thành)
            model.TotalRevenue = await _context.Orders
                .Where(o => o.Status == "Completed" || o.Status == "Delivered")
                .SumAsync(o => o.FinalPrice);

            // Tổng số đơn hàng
            model.TotalOrders = await _context.Orders.CountAsync();

            // Tổng số sản phẩm
            model.TotalProducts = await _context.Products
                .Where(p => p.Status == "Active")
                .CountAsync();

            // Tổng số khách hàng
            model.TotalCustomers = await _context.Users.CountAsync();

            // Đơn hàng hôm nay
            model.TodayOrders = await _context.Orders
                .Where(o => o.CreatedAt.Date == today)
                .CountAsync();

            // Sản phẩm mới tháng này
            model.NewProducts = await _context.Products
                .Where(p => p.CreatedAt >= thisMonth)
                .CountAsync();

            // Khách hàng mới tháng này
            model.NewCustomers = await _context.Users
                .Where(u => u.CreatedAt >= thisMonth)
                .CountAsync();

            // Tính tăng trưởng theo tháng
            var thisMonthRevenue = await _context.Orders
                .Where(o => (o.Status == "Completed" || o.Status == "Delivered") 
                           && o.CreatedAt >= thisMonth)
                .SumAsync(o => o.FinalPrice);

            var lastMonthRevenue = await _context.Orders
                .Where(o => (o.Status == "Completed" || o.Status == "Delivered") 
                           && o.CreatedAt >= lastMonth && o.CreatedAt < thisMonth)
                .SumAsync(o => o.FinalPrice);

            if (lastMonthRevenue > 0)
            {
                model.MonthlyGrowth = Math.Round(((thisMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100, 1);
            }
            else
            {
                model.MonthlyGrowth = thisMonthRevenue > 0 ? 100 : 0;
            }
        }

        private async Task CalculateRevenueChart(DashboardViewModel model, string period)
        {
            await CalculateRevenueChartByPeriod(model.RevenueChart, period);
        }

        private async Task CalculateRevenueChartByPeriod(List<ChartDataPoint> chartData, string period)
        {
            var dates = new List<DateTime>();
            var dateFormat = "dd/MM";
            
            switch (period)
            {
                case "7days":
                    for (int i = 6; i >= 0; i--)
                    {
                        dates.Add(DateTime.Now.Date.AddDays(-i));
                    }
                    break;
                    
                case "30days":
                    for (int i = 29; i >= 0; i--)
                    {
                        dates.Add(DateTime.Now.Date.AddDays(-i));
                    }
                    break;
                    
                case "3months":
                    for (int i = 11; i >= 0; i--)
                    {
                        var date = DateTime.Now.Date.AddDays(-i * 7); // Theo tuần
                        dates.Add(date);
                    }
                    break;
                    
                case "6months":
                    for (int i = 5; i >= 0; i--)
                    {
                        var date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-i);
                        dates.Add(date);
                    }
                    dateFormat = "MM/yyyy";
                    break;
            }

            foreach (var date in dates)
            {
                decimal periodRevenue;
                
                if (period == "3months")
                {
                    // Tính theo tuần
                    var weekEnd = date.AddDays(6);
                    periodRevenue = await _context.Orders
                        .Where(o => (o.Status == "Completed" || o.Status == "Delivered") 
                                   && o.CreatedAt.Date >= date && o.CreatedAt.Date <= weekEnd)
                        .SumAsync(o => o.FinalPrice);
                }
                else if (period == "6months")
                {
                    // Tính theo tháng
                    var monthEnd = date.AddMonths(1).AddDays(-1);
                    periodRevenue = await _context.Orders
                        .Where(o => (o.Status == "Completed" || o.Status == "Delivered") 
                                   && o.CreatedAt.Date >= date && o.CreatedAt.Date <= monthEnd)
                        .SumAsync(o => o.FinalPrice);
                }
                else
                {
                    // Tính theo ngày
                    periodRevenue = await _context.Orders
                        .Where(o => (o.Status == "Completed" || o.Status == "Delivered") 
                                   && o.CreatedAt.Date == date)
                        .SumAsync(o => o.FinalPrice);
                }

                chartData.Add(new ChartDataPoint
                {
                    Date = date.ToString(dateFormat),
                    Revenue = Math.Round(periodRevenue / 1000000, 1) // Chuyển sang triệu VNĐ
                });
            }
        }

        private async Task LoadRecentOrders(DashboardViewModel model)
        {
            // Lấy dữ liệu thô từ database với thông tin payment
            var recentOrdersData = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new
                {
                    o.OrderID,
                    UserName = o.User.Name,
                    ProductName = o.OrderItems.FirstOrDefault().Product.Name,
                    o.FinalPrice,
                    ShipStatus = o.Status, // Trạng thái giao hàng từ Order
                    PaymentStatus = o.Payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault().PaymentStatus ?? "Chưa thanh toán",
                    o.CreatedAt
                })
                .ToListAsync();

            // Sau đó transform dữ liệu
            model.RecentOrders = recentOrdersData.Select(o => new RecentOrderViewModel
            {
                OrderCode = $"#CT{o.OrderID:000}",
                CustomerName = o.UserName ?? "Khách hàng",
                ProductName = o.ProductName ?? "Nhiều sản phẩm",
                TotalValue = o.FinalPrice,
                ShipStatus = GetShipStatusInVietnamese(o.ShipStatus),
                PaymentStatus = GetPaymentStatusInVietnamese(o.PaymentStatus),
                CreatedDate = o.CreatedAt
            }).ToList();
        }

        private async Task LoadTopProducts(DashboardViewModel model)
        {
            var topProducts = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.Status == "Completed" || oi.Order.Status == "Delivered")
                .GroupBy(oi => new { oi.ProductID, oi.Product.Name, oi.Product.Brand })
                .Select(g => new TopProductViewModel
                {
                    ProductName = g.Key.Name,
                    Brand = g.Key.Brand ?? "Không rõ",
                    SoldQuantity = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.UnitPrice * x.Quantity)
                })
                .OrderByDescending(p => p.SoldQuantity)
                .Take(5)
                .ToListAsync();

            model.TopProducts = topProducts;
        }

        private static string GetShipStatusInVietnamese(string status)
        {
            return status switch
            {
                "Pending" => "Chờ xác nhận",
                "Processing" => "Đang chuẩn bị",
                "Shipped" => "Đang giao",
                "Delivered" => "Đã giao",
                "Completed" => "Hoàn thành",
                "Cancelled" => "Đã hủy",
                "Refunded" => "Đã hoàn trả",
                _ => status
            };
        }

        private static string GetPaymentStatusInVietnamese(string status)
        {
            return status switch
            {
                "Pending" => "Chờ thanh toán",
                "Processing" => "Đang xử lý",
                "Completed" => "Đã thanh toán",
                "Failed" => "Thất bại",
                "Cancelled" => "Đã hủy",
                "Refunded" => "Đã hoàn tiền",
                "Chưa thanh toán" => "Chưa thanh toán",
                _ => status
            };
        }
    }
}
