using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberTech.Models
{
    [Table("Users")]
    public class User
    {
        public User()
        {
            AuthMethods = new List<UserAuthMethod>();
            Addresses = new List<UserAddress>();
            PasswordResetTokens = new List<PasswordResetToken>();
            Carts = new List<Cart>();
            Orders = new List<Order>();
            Reviews = new List<Review>();
            UserVouchers = new List<UserVoucher>();
            VoucherTokens = new List<VoucherToken>();
            WishlistItems = new List<WishlistItem>();
            ProductStockNotifications = new List<ProductStockNotification>();
            UserVerifyTokens = new List<UserVerifyToken>();
        }

        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(500)]
        public string? ProfileImageURL { get; set; }

        [Required]
        public string Role { get; set; } = "Customer";

        [StringLength(20)]
        public string? Phone { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Salary { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSpent { get; set; } = 0;

        [Required]
        public int OrderCount { get; set; } = 0;

        public int? RankId { get; set; } = 1;

        [Required]
        public bool EmailVerified { get; set; } = false;

        [Required]
        [StringLength(20)]
        public string UserStatus { get; set; } = "Active";

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 1 = Male, 2 = Female
        public byte? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        // Navigation properties
        [ForeignKey("RankId")]
        public virtual Rank Rank { get; set; }

        public virtual ICollection<UserAuthMethod> AuthMethods { get; set; }
        public virtual ICollection<UserAddress> Addresses { get; set; }
        public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; }
        public virtual ICollection<WishlistItem> WishlistItems { get; set; }
        public virtual ICollection<Cart> Carts { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<UserVoucher> UserVouchers { get; set; }
        public virtual ICollection<VoucherToken> VoucherTokens { get; set; }
        public virtual ICollection<ProductStockNotification> ProductStockNotifications { get; set; }
        public virtual ICollection<UserVerifyToken> UserVerifyTokens { get; set; }
    }
}