using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CyberTech.Models
{
    public class ProductAttribute
    {
        [Key]
        public int AttributeID { get; set; }

        [Required]
        [StringLength(100)]
        public string AttributeName { get; set; }

        [Required]
        [StringLength(50)]
        public string AttributeType { get; set; }

        // Navigation property
        public virtual ICollection<AttributeValue> AttributeValues { get; set; }
    }
}