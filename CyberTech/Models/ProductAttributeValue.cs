using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberTech.Models
{
    public class ProductAttributeValue
    {
        [Required]
        public int ProductID { get; set; }

        [Required]
        public int ValueID { get; set; }

        // Navigation properties
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
        
        [ForeignKey("ValueID")]
        public virtual AttributeValue AttributeValue { get; set; }
    }
}