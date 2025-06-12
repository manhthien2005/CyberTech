using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberTech.Models
{
    public class CategoryAttributes
    {
        [Key]
        public int CategoryAttributeID { get; set; }

        [Required]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string AttributeName { get; set; }

        [ForeignKey("CategoryID")]
        public virtual Category Category { get; set; }
    }
}