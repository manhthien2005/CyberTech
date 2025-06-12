using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CyberTech.Models
{
    public class SubSubcategory
    {
        [Key]
        public int SubSubcategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public int SubcategoryID { get; set; }

        // Navigation properties
        [ForeignKey("SubcategoryID")]
        [JsonIgnore]
        public virtual Subcategory Subcategory { get; set; }

        [JsonIgnore]
        public virtual ICollection<Product> Products { get; set; }
    }
}