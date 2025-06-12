using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberTech.Models
{
    public class PasswordResetToken
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [StringLength(256)]
        public string Token { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public bool Used { get; set; } = false;

        // Navigation property
        [ForeignKey("UserID")]
        public virtual User User { get; set; }
    }
}