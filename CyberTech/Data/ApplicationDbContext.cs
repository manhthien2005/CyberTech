using Microsoft.EntityFrameworkCore;
using CyberTech.Models;

namespace CyberTech.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserAuthMethod> UserAuthMethods { get; set; }
        public DbSet<Rank> Ranks { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<CategoryAttributes> CategoryAttributes { get; set; }
        public DbSet<Subcategory> Subcategories { get; set; }
        public DbSet<SubSubcategory> SubSubcategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductAttribute> ProductAttributes { get; set; }
        public DbSet<AttributeValue> AttributeValues { get; set; }
        public DbSet<ProductAttributeValue> ProductAttributeValues { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<VoucherProducts> VoucherProducts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<UserVoucher> UserVouchers { get; set; }
        public DbSet<VoucherToken> VoucherTokens { get; set; }
        public DbSet<ProductStockNotification> ProductStockNotifications { get; set; }
        public DbSet<UserVerifyToken> UserVerifyTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<UserAuthMethod>().ToTable("UserAuthMethods");
            modelBuilder.Entity<Rank>().ToTable("Ranks");
            modelBuilder.Entity<UserAddress>().ToTable("UserAddresses");
            modelBuilder.Entity<PasswordResetToken>().ToTable("PasswordResetTokens");
            modelBuilder.Entity<Category>().ToTable("Category");
            modelBuilder.Entity<CategoryAttributes>().ToTable("CategoryAttributes");
            modelBuilder.Entity<Subcategory>().ToTable("Subcategory");
            modelBuilder.Entity<SubSubcategory>().ToTable("SubSubcategory");
            modelBuilder.Entity<Product>().ToTable("Products");
            modelBuilder.Entity<ProductImage>().ToTable("ProductImages");
            modelBuilder.Entity<ProductAttribute>().ToTable("ProductAttribute");
            modelBuilder.Entity<AttributeValue>().ToTable("AttributeValue");
            modelBuilder.Entity<ProductAttributeValue>().ToTable("ProductAttributeValue");
            modelBuilder.Entity<Cart>().ToTable("Carts");
            modelBuilder.Entity<CartItem>().ToTable("CartItems");
            modelBuilder.Entity<Voucher>().ToTable("Vouchers");
            modelBuilder.Entity<VoucherProducts>().ToTable("VoucherProducts");
            modelBuilder.Entity<Order>().ToTable("Orders");
            modelBuilder.Entity<OrderItem>().ToTable("OrderItems");
            modelBuilder.Entity<Payment>().ToTable("Payments");
            modelBuilder.Entity<Review>().ToTable("Reviews");
            modelBuilder.Entity<WishlistItem>().ToTable("WishlistItems");
            modelBuilder.Entity<UserVoucher>().ToTable("UserVouchers");
            modelBuilder.Entity<VoucherToken>().ToTable("VoucherTokens");
            modelBuilder.Entity<ProductStockNotification>().ToTable("ProductStockNotifications");
            modelBuilder.Entity<UserVerifyToken>().ToTable("UserVerifyTokens");


            // Performance Indexes
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Name)
                .HasDatabaseName("IX_Products_Name");

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Price)
                .HasDatabaseName("IX_Products_Price");

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Stock)
                .HasDatabaseName("IX_Products_Stock");

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SubSubcategoryID)
                .HasDatabaseName("IX_Products_SubSubcategoryID");

            modelBuilder.Entity<Product>()
                .HasIndex(p => new { p.SubSubcategoryID, p.Stock, p.Name })
                .HasDatabaseName("IX_Products_Composite_Search");

            modelBuilder.Entity<ProductImage>()
                .HasIndex(pi => new { pi.ProductID, pi.IsPrimary })
                .HasDatabaseName("IX_ProductImages_ProductID_IsPrimary");

            // Cấu hình mối quan hệ giữa User và WishlistItem
            modelBuilder.Entity<User>()
                .HasMany(u => u.WishlistItems)
                .WithOne(wi => wi.User)
                .HasForeignKey(wi => wi.UserID);

            // Cấu hình mối quan hệ giữa WishlistItem và Product
            modelBuilder.Entity<WishlistItem>()
                .HasOne(wi => wi.Product)
                .WithMany(p => p.WishlistItems)
                .HasForeignKey(wi => wi.ProductID);

            // Cấu hình unique constraint cho WishlistItem
            modelBuilder.Entity<WishlistItem>()
                .HasIndex(wi => new { wi.UserID, wi.ProductID })
                .IsUnique();

            // Cấu hình unique constraint cho ProductStockNotification
            modelBuilder.Entity<ProductStockNotification>()
                .HasIndex(psn => new { psn.UserID, psn.ProductID })
                .IsUnique();

            modelBuilder.Entity<VoucherProducts>()
                .HasKey(vp => new { vp.VoucherID, vp.ProductID });

            modelBuilder.Entity<VoucherProducts>()
                .HasOne(vp => vp.Voucher)
                .WithMany(v => v.VoucherProducts)
                .HasForeignKey(vp => vp.VoucherID);

            modelBuilder.Entity<VoucherProducts>()
                .HasOne(vp => vp.Product)
                .WithMany(p => p.VoucherProducts)
                .HasForeignKey(vp => vp.ProductID);

            modelBuilder.Entity<ProductAttributeValue>()
                .HasKey(pav => new { pav.ProductID, pav.ValueID });

            modelBuilder.Entity<ProductAttributeValue>()
                .HasOne(pav => pav.AttributeValue)
                .WithMany(av => av.ProductAttributeValues)
                .HasForeignKey(pav => pav.ValueID);

            modelBuilder.Entity<Order>()
                .HasKey(o => o.OrderID);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserID);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderID);

            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasDefaultValue("Pending");

            modelBuilder.Entity<OrderItem>()
                .HasKey(oi => oi.OrderItemID);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductID);
        }
    }
}