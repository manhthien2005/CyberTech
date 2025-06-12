using CyberTech.Models;
using System.Collections.Generic;

namespace CyberTech.ViewModels
{
    public class CartViewModel
    {
        public Cart Cart { get; set; }
        public List<CartItem> CartItems { get; set; }
        public List<UserAddress> UserAddresses { get; set; }
        public Voucher AppliedVoucher { get; set; }

        // Add rank discount information
        public decimal RankDiscountPercent { get; set; }
        public decimal RankDiscountAmount { get; set; }
        public string RankName { get; set; }

        // Add product discount information
        public decimal ProductDiscountAmount { get; set; }

        // Add voucher discount information
        public decimal VoucherDiscountAmount { get; set; }

        public decimal Subtotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal FinalTotal { get; set; }
    }
}