using System.ComponentModel.DataAnnotations;

namespace CyberTech.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public decimal MonthlyGrowth { get; set; }
        public int TodayOrders { get; set; }
        public int NewProducts { get; set; }
        public int NewCustomers { get; set; }
        
        // Dữ liệu cho biểu đồ 7 ngày
        public List<ChartDataPoint> RevenueChart { get; set; } = new List<ChartDataPoint>();
        
        // Đơn hàng gần đây
        public List<RecentOrderViewModel> RecentOrders { get; set; } = new List<RecentOrderViewModel>();
        
        // Sản phẩm bán chạy
        public List<TopProductViewModel> TopProducts { get; set; } = new List<TopProductViewModel>();
    }
    
    public class ChartDataPoint
    {
        public string Date { get; set; }
        public decimal Revenue { get; set; }
    }
    
    public class RecentOrderViewModel
    {
        public string OrderCode { get; set; }
        public string CustomerName { get; set; }
        public string ProductName { get; set; }
        public decimal TotalValue { get; set; }
        public string ShipStatus { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    
    public class TopProductViewModel
    {
        public string ProductName { get; set; }
        public string Brand { get; set; }
        public int SoldQuantity { get; set; }
        public decimal Revenue { get; set; }
    }
} 