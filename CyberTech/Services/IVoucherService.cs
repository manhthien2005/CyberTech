using CyberTech.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CyberTech.Services
{
    public interface IVoucherService
    {
        Task<(IEnumerable<VoucherViewModel> vouchers, object pagination)> GetVouchersAsync(int page, int pageSize, string searchTerm, string sortBy, DateTime? startDate = null, DateTime? endDate = null);
        Task<VoucherViewModel> CreateVoucherAsync(CreateVoucherDTO voucherDto);
        Task<VoucherViewModel> UpdateVoucherAsync(UpdateVoucherDTO voucherDto);
        Task<bool> DeleteVoucherAsync(int id);
    }
}