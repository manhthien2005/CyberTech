using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CyberTech.Models.DTOs
{
    public class CreateVoucherDTO
    {
        [Required(ErrorMessage = "Mã voucher là bắt buộc")]
        [StringLength(50, ErrorMessage = "Mã voucher không được vượt quá 50 ký tự")]
        public string Code { get; set; }

        [StringLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Loại giảm giá là bắt buộc")]
        [StringLength(10, ErrorMessage = "Loại giảm giá không hợp lệ")]
        public string DiscountType { get; set; }

        [Required(ErrorMessage = "Giá trị giảm giá là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0")]
        public decimal DiscountValue { get; set; }

        public int? QuantityAvailable { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        public DateTime ValidFrom { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        public DateTime ValidTo { get; set; }

        [Required(ErrorMessage = "Trạng thái hoạt động là bắt buộc")]
        public bool IsActive { get; set; }

        [Required(ErrorMessage = "Áp dụng cho là bắt buộc")]
        [StringLength(10, ErrorMessage = "Áp dụng cho không hợp lệ")]
        public string AppliesTo { get; set; }

        [Required(ErrorMessage = "Phạm vi áp dụng là bắt buộc")]
        public bool IsSystemWide { get; set; }

        public List<int> ProductIds { get; set; } = new List<int>();
    }

    public class UpdateVoucherDTO : CreateVoucherDTO
    {
        [Required(ErrorMessage = "ID voucher là bắt buộc")]
        public int VoucherId { get; set; }
    }

    public class VoucherViewModel
    {
        public int VoucherId { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public int? QuantityAvailable { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsActive { get; set; }
        public string AppliesTo { get; set; }
        public bool IsSystemWide { get; set; }
        public int UserCount { get; set; }
        public int ProductCount { get; set; }
    }
}