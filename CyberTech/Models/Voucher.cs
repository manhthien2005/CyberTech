using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;

namespace CyberTech.Models
{
    public class Voucher
    {
        [Key]
        public int VoucherID { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        public string Description { get; set; }

        [Required]
        [StringLength(10)]
        public string DiscountType { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Discount value must be greater than 0")]
        public decimal DiscountValue { get; set; }

        public int? QuantityAvailable { get; set; }

        [Required]
        public DateTime ValidFrom { get; set; }

        [Required]
        public DateTime ValidTo { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        [StringLength(10)]
        public string AppliesTo { get; set; } = "Order";

        // Flag to indicate if this is a system-wide voucher or a user-specific voucher
        [Required]
        public bool IsSystemWide { get; set; } = false;

        public virtual ICollection<VoucherProducts> VoucherProducts { get; set; }
        public virtual ICollection<UserVoucher> UserVouchers { get; set; }

        public bool IsValid()
        {
            // Check if voucher is active
            if (!IsActive)
                return false;

            // Check if voucher is within valid date range
            if (ValidFrom > DateTime.Now || ValidTo < DateTime.Now)
                return false;

            // Check if voucher has available quantity
            if (QuantityAvailable.HasValue && QuantityAvailable <= 0)
                return false;

            return true;
        }

        /// <summary>
        /// Checks if this voucher applies to a specific product
        /// </summary>
        /// <param name="productId">The product ID to check</param>
        /// <returns>True if the voucher applies to the product, false otherwise</returns>
        public bool AppliesToProduct(int productId)
        {
            // If voucher applies to the entire order, it applies to all products
            if (AppliesTo == "Order")
                return true;

            // If voucher applies to specific products, check if the product is in the list
            if (AppliesTo == "Product" && VoucherProducts != null)
                return VoucherProducts.Any(vp => vp.ProductID == productId);

            return false;
        }
    }
}