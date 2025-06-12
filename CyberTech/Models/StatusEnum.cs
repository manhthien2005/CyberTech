
namespace CyberTech.Models
{
    public enum UserRole
    {
        Customer,
        Support,
        Manager,
        SuperAdmin
    }

    public enum UserStatus
    {
        Active,
        Inactive,
        Suspended
    }

    public enum AuthType
    {
        Password,
        Google,
        Facebook
    }

    public enum DiscountType
    {
        PERCENT,
        FIXED
    }

    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled
    }

    public enum PaymentMethod
    {
        COD,
        VNPay,
        Momo
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }

    public enum ShippingMethod
    {
        Standard,
        Express
    }

    public enum ShippingStatus
    {
        Pending,
        Shipped,
        InTransit,
        Delivered
    }
}