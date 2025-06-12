using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberTech.Models
{
    public class ProductStockNotification
    {
        [Key]
        public int NotificationID { get; set; }

        public int ProductID { get; set; }

        public int UserID { get; set; }

        public bool IsNotified { get; set; } = false;

        public DateTime? NotificationSentAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }

        [ForeignKey("UserID")]
        public virtual User User { get; set; }
    }
}