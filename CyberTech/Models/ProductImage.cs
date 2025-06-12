using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberTech.Models
{
    public class ProductImage
    {
        [Key]
        public int ImageID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required]
        [StringLength(255)]
        public string ImageURL { get; set; }

        [Required]
        public bool IsPrimary { get; set; } = false;

        [Required]
        public int DisplayOrder { get; set; } = 0;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}