using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberTech.Models
{
    public class UserVoucher
    {
        [Key]
        public int UserVoucherID { get; set; }

        public int UserID { get; set; }
        [ForeignKey("UserID")]
        public User User { get; set; }

        public int VoucherID { get; set; }
        [ForeignKey("VoucherID")]
        public Voucher Voucher { get; set; }

        public DateTime AssignedDate { get; set; }
        public DateTime? UsedDate { get; set; }
        public bool IsUsed { get; set; }
        public int? OrderID { get; set; }
        [ForeignKey("OrderID")]
        public Order Order { get; set; }
        public bool IsValid()
        {
            // Check if voucher has been used
            if (IsUsed)
                return false;

            // Check if voucher exists and is active
            if (Voucher == null || !Voucher.IsActive)
                return false;

            // Check if voucher is within valid date range
            if (Voucher.ValidFrom > DateTime.Now || Voucher.ValidTo < DateTime.Now)
                return false;

            // Check if voucher has available quantity
            if (Voucher.QuantityAvailable.HasValue && Voucher.QuantityAvailable <= 0)
                return false;

            return true;
        }
    }
}