using System.ComponentModel.DataAnnotations;

namespace CyberTech.Models.DTOs
{
    public class CreateRankDTO
    {
        [Required(ErrorMessage = "Tên cấp bậc là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên cấp bậc không được vượt quá 50 ký tự")]
        public string RankName { get; set; }

        [Required(ErrorMessage = "Chi tiêu tối thiểu là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Chi tiêu tối thiểu phải lớn hơn hoặc bằng 0")]
        public decimal MinTotalSpent { get; set; }

        [Required(ErrorMessage = "Phần trăm giảm giá là bắt buộc")]
        [Range(0, 100, ErrorMessage = "Phần trăm giảm giá phải từ 0 đến 100")]
        public decimal? DiscountPercent { get; set; }

        [Required(ErrorMessage = "Mức ưu tiên là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Mức ưu tiên phải lớn hơn 0")]
        public int PriorityLevel { get; set; }

        [StringLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự")]
        public string Description { get; set; }
    }

    public class UpdateRankDTO : CreateRankDTO
    {
        [Required(ErrorMessage = "ID cấp bậc là bắt buộc")]
        public int RankId { get; set; }
    }

    public class RankViewModel
    {
        public int RankId { get; set; }
        public string RankName { get; set; }
        public decimal MinTotalSpent { get; set; }
        public decimal? DiscountPercent { get; set; }
        public int PriorityLevel { get; set; }
        public string Description { get; set; }
        public int UserCount { get; set; }
    }
}