using System;
using System.Text;
using System.Linq;
using CyberTech.Models;

namespace CyberTech.Services
{
    public class EmailTemplateService
    {
        public (string subject, string content) GetOrderConfirmationTemplate(Order order)
        {
            string subject = $"Xác nhận đơn hàng #{order.OrderID} - CyberTech";

            var content = new StringBuilder();
            content.AppendLine($@"
                <h2 style='color: #333; margin-top: 0;'>Xác nhận đơn hàng</h2>
                <p style='color: #666; line-height: 1.6;'>
                    Xin chào {(string.IsNullOrEmpty(order.User?.Name) ? "Quý khách" : order.User.Name)},<br><br>
                    Cảm ơn bạn đã đặt hàng tại CyberTech. Đơn hàng của bạn đã được xác nhận.
                </p>

                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 4px; margin: 20px 0;'>
                    <h3 style='color: #007bff; margin-top: 0;'>Chi tiết đơn hàng #{order.OrderID}</h3>
                    
                    <!-- Thông tin sản phẩm -->
                    <table style='width: 100%; border-collapse: collapse; margin: 15px 0;'>
                        <tr style='background-color: #e9ecef;'>
                            <th style='padding: 8px; text-align: left; border: 1px solid #dee2e6;'>Sản phẩm</th>
                            <th style='padding: 8px; text-align: center; border: 1px solid #dee2e6;'>Số lượng</th>
                            <th style='padding: 8px; text-align: right; border: 1px solid #dee2e6;'>Đơn giá</th>
                            <th style='padding: 8px; text-align: right; border: 1px solid #dee2e6;'>Giảm giá</th>
                            <th style='padding: 8px; text-align: right; border: 1px solid #dee2e6;'>Thành tiền</th>
                        </tr>");

            foreach (var item in order.OrderItems)
            {
                content.AppendLine($@"
                        <tr>
                            <td style='padding: 8px; border: 1px solid #dee2e6;'>{item.Product?.Name ?? "Sản phẩm"}</td>
                            <td style='padding: 8px; text-align: center; border: 1px solid #dee2e6;'>{item.Quantity}</td>
                            <td style='padding: 8px; text-align: right; border: 1px solid #dee2e6;'>{item.UnitPrice:N0}đ</td>
                            <td style='padding: 8px; text-align: right; border: 1px solid #dee2e6;'>{item.DiscountAmount:N0}đ</td>
                            <td style='padding: 8px; text-align: right; border: 1px solid #dee2e6;'>{item.FinalSubtotal:N0}đ</td>
                        </tr>");
            }

            content.AppendLine($@"
                    </table>

                    <!-- Tổng cộng -->
                    <div style='margin-top: 15px; border-top: 2px solid #dee2e6; padding-top: 15px;'>
                        <p style='margin: 5px 0; text-align: right;'><strong>Tổng tiền hàng:</strong> {order.TotalPrice:N0}đ</p>
                        <p style='margin: 5px 0; text-align: right;'><strong>Giảm giá sản phẩm:</strong> {order.ProductDiscountAmount:N0}đ</p>
                        <p style='margin: 5px 0; text-align: right;'><strong>Giảm giá thành viên:</strong> {order.RankDiscountAmount:N0}đ</p>
                        <p style='margin: 5px 0; text-align: right;'><strong>Giảm giá voucher:</strong> {order.VoucherDiscountAmount:N0}đ</p>
                        <p style='margin: 5px 0; text-align: right; color: #007bff; font-size: 18px;'><strong>Tổng thanh toán:</strong> {order.FinalPrice:N0}đ</p>
                    </div>

                    <!-- Thông tin giao hàng -->
                    <div style='margin-top: 20px; padding-top: 15px; border-top: 2px solid #dee2e6;'>
                        <h4 style='color: #333; margin-top: 0;'>Thông tin giao hàng</h4>");

            if (order.UserAddress != null)
            {
                content.AppendLine($@"
                        <p style='margin: 5px 0;'><strong>Người nhận:</strong> {order.UserAddress.RecipientName}</p>
                        <p style='margin: 5px 0;'><strong>Số điện thoại:</strong> {order.UserAddress.Phone}</p>
                        <p style='margin: 5px 0;'><strong>Địa chỉ:</strong> {order.UserAddress.AddressLine}</p>
                        <p style='margin: 5px 0;'><strong>Phường/Xã:</strong> {order.UserAddress.Ward}</p>
                        <p style='margin: 5px 0;'><strong>Quận/Huyện:</strong> {order.UserAddress.District}</p>
                        <p style='margin: 5px 0;'><strong>Tỉnh/Thành phố:</strong> {order.UserAddress.City}</p>");
            }

            content.AppendLine($@"
                    </div>

                    <!-- Thông tin đơn hàng -->
                    <div style='margin-top: 20px; padding-top: 15px; border-top: 2px solid #dee2e6;'>
                        <p style='margin: 5px 0;'><strong>Trạng thái đơn hàng:</strong> {order.Status}</p>
                        <p style='margin: 5px 0;'><strong>Phương thức thanh toán:</strong> {order.Payments?.FirstOrDefault()?.PaymentMethod ?? "Chưa thanh toán"}</p>
                        <p style='margin: 5px 0;'><strong>Ngày đặt hàng:</strong> {order.CreatedAt:dd/MM/yyyy HH:mm}</p>
                    </div>
                </div>

                <p style='color: #666;'>
                    Bạn có thể theo dõi trạng thái đơn hàng trong phần 'Đơn hàng của tôi' trên website.
                </p>
                <p style='color: #666;'>
                    Nếu bạn có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi qua email support@cybertech.com
                </p>");

            return (subject, content.ToString());
        }

        public (string subject, string content) GetFirstOrderVoucherTemplate(string userName, string voucherCode, string claimUrl)
        {
            string subject = "Chào mừng bạn đến với CyberTech - Tặng voucher cho đơn hàng đầu tiên!";

            var content = new StringBuilder();
            content.AppendLine($@"
                <h2 style='color: #333; margin-top: 0;'>Chào mừng bạn đến với CyberTech!</h2>
                <p style='color: #666; line-height: 1.6;'>
                    Xin chào {userName},<br><br>
                    Cảm ơn bạn đã tin tưởng và đặt hàng đầu tiên tại CyberTech. Để tri ân sự ủng hộ của bạn, 
                    chúng tôi xin gửi tặng bạn một voucher giảm giá đặc biệt.
                </p>
                <div style='background-color: #f0f0f0; border: 2px dashed #007bff; padding: 15px; text-align: center; margin: 20px 0;'>
                    <h3 style='color: #007bff; margin-top: 0;'>VOUCHER GIẢM GIÁ</h3>
                    <p style='font-size: 18px; font-weight: bold;'>{voucherCode}</p>
                    <p>Giảm 50.000đ cho đơn hàng tiếp theo của bạn</p>
                </div>
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{claimUrl}' 
                       style='background-color: #007bff; color: white; padding: 12px 24px; 
                              text-decoration: none; border-radius: 4px; display: inline-block;
                              font-weight: bold;'>
                        Nhận Voucher Ngay
                    </a>
                </div>
                <p style='color: #666; font-size: 14px;'>
                    <strong>Lưu ý:</strong><br>
                    - Voucher có hiệu lực trong 7 ngày kể từ ngày nhận<br>
                    - Mỗi voucher chỉ được sử dụng một lần<br>
                    - Không áp dụng đồng thời với các chương trình khuyến mãi khác
                </p>");

            return (subject, content.ToString());
        }

        public (string subject, string content) GetPremiumVoucherTemplate(string userName, string voucherCode, string claimUrl)
        {
            string subject = "Quà tặng đặc biệt dành cho khách hàng thân thiết!";

            var content = new StringBuilder();
            content.AppendLine($@"
                <h2 style='color: #333; margin-top: 0;'>Quà tặng đặc biệt dành cho khách hàng thân thiết!</h2>
                <p style='color: #666; line-height: 1.6;'>
                    Xin chào {userName},<br><br>
                    Cảm ơn bạn đã tin tưởng và đặt hàng tại CyberTech. Với giá trị đơn hàng trên 1.000.000đ, 
                    chúng tôi xin gửi tặng bạn một voucher giảm giá đặc biệt cho lần mua hàng tiếp theo.
                </p>
                <div style='background-color: #f0f0f0; border: 2px dashed #007bff; padding: 15px; text-align: center; margin: 20px 0;'>
                    <h3 style='color: #007bff; margin-top: 0;'>VOUCHER GIẢM GIÁ</h3>
                    <p style='font-size: 18px; font-weight: bold;'>{voucherCode}</p>
                    <p>Giảm 10% cho đơn hàng tiếp theo của bạn</p>
                </div>
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{claimUrl}' 
                       style='background-color: #007bff; color: white; padding: 12px 24px; 
                              text-decoration: none; border-radius: 4px; display: inline-block;
                              font-weight: bold;'>
                        Nhận Voucher Ngay
                    </a>
                </div>
                <p style='color: #666; font-size: 14px;'>
                    <strong>Lưu ý:</strong><br>
                    - Voucher có hiệu lực trong 14 ngày kể từ ngày nhận<br>
                    - Mỗi voucher chỉ được sử dụng một lần<br>
                    - Không áp dụng đồng thời với các chương trình khuyến mãi khác
                </p>");

            return (subject, content.ToString());
        }
    }
}