// using System;
// using System.Collections.Generic;
// using System.ComponentModel.DataAnnotations;
// using System.ComponentModel.DataAnnotations.Schema;

// namespace CyberTech.Models
// {
//     public class Cart
//     {
//         [Key]
//         public int CartID { get; set; }

//         [Required]
//         public int UserID { get; set; }

//         [Required]
//         [Column(TypeName = "decimal(18,2)")]
//         public decimal TotalPrice { get; set; } = 0;

//         // Navigation properties
//         [ForeignKey("UserID")]
//         public virtual User User { get; set; }

//         public virtual ICollection<CartItem> CartItems { get; set; }
//     }

//     public class CartItem
//     {
//         [Key]
//         public int CartItemID { get; set; }

//         [Required]
//         public int CartID { get; set; }

//         [Required]
//         public int ProductID { get; set; }

//         [Required]
//         public int Quantity { get; set; }

//         [Required]
//         [Column(TypeName = "decimal(18,2)")]
//         public decimal Subtotal { get; set; }

//         // Navigation properties
//         [ForeignKey("CartID")]
//         public virtual Cart Cart { get; set; }

//         [ForeignKey("ProductID")]
//         public virtual Product Product { get; set; }
//     }

//     public class Order
//     {
//         [Key]
//         public int OrderID { get; set; }

//         [Required]
//         public int UserID { get; set; }

//         [Required]
//         [Column(TypeName = "decimal(18,2)")]
//         public decimal TotalPrice { get; set; } = 0;

//         [Required]
//         [Column(TypeName = "decimal(18,2)")]
//         public decimal DiscountAmount { get; set; } = 0;

//         [Column(TypeName = "decimal(18,2)")]
//         public decimal FinalPrice { get; set; }

//         [Required]
//         [StringLength(50)]
//         public string Status { get; set; } = "Pending";

//         [Required]
//         public DateTime CreatedAt { get; set; } = DateTime.Now;

//         // Navigation properties
//         [ForeignKey("UserID")]
//         public virtual User User { get; set; }
//         public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

//         public virtual ICollection<Payment> Payments { get; set; }
//     }

//     public class OrderItem
//     {
//         [Key]
//         public int OrderItemID { get; set; }

//         [Required]
//         public int OrderID { get; set; }

//         [Required]
//         public int ProductID { get; set; }

//         [Required]
//         public int Quantity { get; set; }

//         [Column(TypeName = "decimal(18,2)")]
//         public decimal UnitPrice { get; set; }
//         [Column(TypeName = "decimal(18,2)")]
//         public decimal Subtotal { get; set; }

//         [Required]
//         [Column(TypeName = "decimal(18,2)")]
//         public decimal DiscountAmount { get; set; } = 0;

//         [Column(TypeName = "decimal(18,2)")]
//         public decimal FinalSubtotal { get; set; }

//         // Navigation properties
//         [ForeignKey("OrderID")]
//         public virtual Order Order { get; set; }

//         [ForeignKey("ProductID")]
//         public virtual Product Product { get; set; }
//     }

//     public class Product
//     {
//         public Product()
//         {
//             ProductImages = new List<ProductImage>();
//             ProductAttributeValues = new List<ProductAttributeValue>();
//             Wishlists = new List<Wishlist>();
//             CartItems = new List<CartItem>();
//             OrderItems = new List<OrderItem>();
//             Reviews = new List<Review>();
//             VoucherProducts = new List<VoucherProducts>();
//         }

//         [Key]
//         public int ProductID { get; set; }

//         [Required]
//         [StringLength(100)]
//         public string Name { get; set; }

//         public string Description { get; set; }

//         [Required]
//         [Column(TypeName = "decimal(18,2)")]
//         public decimal Price { get; set; }

//         [Column(TypeName = "decimal(5,2)")]
//         public decimal? SalePercentage { get; set; }

//         [Column(TypeName = "decimal(18,2)")]
//         public decimal? SalePrice { get; set; }

//         [Required]
//         public int Stock { get; set; }

//         [Required]
//         public int SubSubcategoryID { get; set; }

//         [ForeignKey("SubSubcategoryID")]
//         public virtual SubSubcategory SubSubcategory { get; set; }

//         [StringLength(100)]
//         public string Brand { get; set; }

//         [Required]
//         [StringLength(20)]
//         public string Status { get; set; } = "Active";

//         [Required]
//         public DateTime CreatedAt { get; set; } = DateTime.Now;

//         [Required]
//         public DateTime UpdatedAt { get; set; } = DateTime.Now;

//         public virtual ICollection<ProductImage> ProductImages { get; set; }
//         public virtual ICollection<ProductAttributeValue> ProductAttributeValues { get; set; }
//         public virtual ICollection<Wishlist> Wishlists { get; set; }
//         public virtual ICollection<CartItem> CartItems { get; set; }
//         public virtual ICollection<OrderItem> OrderItems { get; set; }
//         public virtual ICollection<Review> Reviews { get; set; }
//         public virtual ICollection<VoucherProducts> VoucherProducts { get; set; }
//     }

//     public class ProductImage
//     {
//         [Key]
//         public int ImageID { get; set; }

//         [Required]
//         public int ProductID { get; set; }

//         [Required]
//         [StringLength(255)]
//         public string ImageURL { get; set; }

//         [Required]
//         public bool IsPrimary { get; set; } = false;

//         [Required]
//         public int DisplayOrder { get; set; } = 0;

//         [Required]
//         public DateTime CreatedAt { get; set; } = DateTime.Now;

//         // Navigation property
//         [ForeignKey("ProductID")]
//         public virtual Product Product { get; set; }
//     }

//     public class VoucherProducts
//     {
//         public int VoucherID { get; set; }
//         public int ProductID { get; set; }

//         [ForeignKey("VoucherID")]
//         public virtual Voucher Voucher { get; set; }

//         [ForeignKey("ProductID")]
//         public virtual Product Product { get; set; }
//     }

//     public class Voucher
//     {
//         [Key]
//         public int VoucherID { get; set; }

//         [Required]
//         [StringLength(50)]
//         public string Code { get; set; }

//         public string Description { get; set; }

//         [Required]
//         [StringLength(10)]
//         public string DiscountType { get; set; }

//         [Required]
//         [Column(TypeName = "decimal(18,2)")]
//         [Range(0.01, double.MaxValue, ErrorMessage = "Discount value must be greater than 0")]
//         public decimal DiscountValue { get; set; }

//         public int? QuantityAvailable { get; set; }

//         [Required]
//         public DateTime ValidFrom { get; set; }

//         [Required]
//         public DateTime ValidTo { get; set; }

//         [Required]
//         public bool IsActive { get; set; } = true;

//         [Required]
//         [StringLength(10)]
//         public string AppliesTo { get; set; } = "Order";

//         public virtual ICollection<VoucherProducts> VoucherProducts { get; set; }
//     }
// }