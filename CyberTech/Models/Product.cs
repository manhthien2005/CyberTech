using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CyberTech.Models
{
    public class Product
    {
        public Product()
        {
            ProductImages = new List<ProductImage>();
            ProductAttributeValues = new List<ProductAttributeValue>();
            WishlistItems = new List<WishlistItem>();
            CartItems = new List<CartItem>();
            OrderItems = new List<OrderItem>();
            Reviews = new List<Review>();
            VoucherProducts = new List<VoucherProducts>();
            ProductStockNotifications = new List<ProductStockNotification>();
        }

        [Key]
        public int ProductID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? SalePercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalePrice { get; set; }

        [Required]
        public int Stock { get; set; }

        [Required]
        public int SubSubcategoryID { get; set; }

        [ForeignKey("SubSubcategoryID")]
        [JsonIgnore]
        public virtual SubSubcategory SubSubcategory { get; set; }

        [StringLength(100)]
        public string Brand { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Calculate the effective price based on sale options
        public decimal GetEffectivePrice()
        {
            // If SalePrice is directly set, use it
            if (SalePrice.HasValue)
            {
                return SalePrice.Value;
            }
            // If SalePercentage is set, calculate the sale price
            else if (SalePercentage.HasValue)
            {
                return Price * (1 - (SalePercentage.Value / 100));
            }
            // Otherwise use the regular price
            else
            {
                return Price;
            }
        }

        // Check if the product is on sale
        [NotMapped]
        public bool IsOnSale => (SalePrice.HasValue && SalePrice.Value < Price) || SalePercentage.HasValue;

        public virtual ICollection<ProductImage> ProductImages { get; set; }
        public virtual ICollection<ProductAttributeValue> ProductAttributeValues { get; set; }
        public virtual ICollection<WishlistItem> WishlistItems { get; set; }
        public virtual ICollection<CartItem> CartItems { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<VoucherProducts> VoucherProducts { get; set; }
        public virtual ICollection<ProductStockNotification> ProductStockNotifications { get; set; }
    }
}