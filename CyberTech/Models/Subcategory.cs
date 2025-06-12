using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CyberTech.Models
{
    public class Subcategory
    {
        [Key]
        public int SubcategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public int CategoryID { get; set; }

        // Navigation properties
        [ForeignKey("CategoryID")]
        [JsonIgnore]
        public virtual Category Category { get; set; }

        [JsonIgnore]
        public virtual ICollection<SubSubcategory> SubSubcategories { get; set; }
    }
}