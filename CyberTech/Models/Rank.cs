using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberTech.Models
{
    public class Rank
    {
        [Key]
        public int RankId { get; set; }

        [Required]
        [StringLength(50)]
        public string RankName { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinTotalSpent { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? DiscountPercent { get; set; }

        [Required]
        public int PriorityLevel { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        // Navigation property
        public virtual ICollection<User> Users { get; set; }
    }
}