using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberTech.Models
{
    public class AttributeValue
    {
        [Key]
        public int ValueID { get; set; }

        [Required]
        public int AttributeID { get; set; }

        [Required]
        [StringLength(255)]
        public string ValueName { get; set; }

        // Navigation property
        [ForeignKey("AttributeID")]
        public virtual ProductAttribute ProductAttribute { get; set; }

        // Navigation property for many-to-many relationship
        public virtual ICollection<ProductAttributeValue> ProductAttributeValues { get; set; }
    }
}