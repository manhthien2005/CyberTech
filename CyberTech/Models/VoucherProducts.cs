using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberTech.Models
{
    public class VoucherProducts
    {
        public int VoucherID { get; set; }
        public int ProductID { get; set; }

        [ForeignKey("VoucherID")]
        public virtual Voucher Voucher { get; set; }

        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}