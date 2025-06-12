using CyberTech.Data;
using CyberTech.Models;
using CyberTech.Models.DTOs;
using CyberTech.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CyberTech.Services
{
    public class VoucherService : IVoucherService
    {
        private readonly ApplicationDbContext _context;

        public VoucherService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<VoucherViewModel> vouchers, object pagination)> GetVouchersAsync(int page, int pageSize, string searchTerm, string sortBy, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Vouchers
                .Include(v => v.UserVouchers)
                .Include(v => v.VoucherProducts)
                .Select(v => new VoucherViewModel
                {
                    VoucherId = v.VoucherID,
                    Code = v.Code,
                    Description = v.Description,
                    DiscountType = v.DiscountType,
                    DiscountValue = v.DiscountValue,
                    QuantityAvailable = v.QuantityAvailable,
                    ValidFrom = v.ValidFrom,
                    ValidTo = v.ValidTo,
                    IsActive = v.IsActive,
                    AppliesTo = v.AppliesTo,
                    IsSystemWide = v.IsSystemWide,
                    ProductCount = v.VoucherProducts.Count
                });

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(v => v.Code.ToLower().Contains(searchTerm) || v.Description.ToLower().Contains(searchTerm));
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(v => v.ValidFrom <= endDate.Value && v.ValidTo >= startDate.Value);
            }
            else if (startDate.HasValue)
            {
                query = query.Where(v => v.ValidTo >= startDate.Value);
            }
            else if (endDate.HasValue)
            {
                query = query.Where(v => v.ValidFrom <= endDate.Value);
            }

            switch (sortBy.ToLower())
            {
                case "validto_asc":
                    query = query.OrderBy(v => v.ValidTo);
                    break;
                case "validto_desc":
                    query = query.OrderByDescending(v => v.ValidTo);
                    break;
                case "code_asc":
                    query = query.OrderBy(v => v.Code);
                    break;
                case "code_desc":
                    query = query.OrderByDescending(v => v.Code);
                    break;
                case "value_asc":
                    query = query.OrderBy(v => v.DiscountValue);
                    break;
                case "value_desc":
                    query = query.OrderByDescending(v => v.DiscountValue);
                    break;
                default:
                    query = query.OrderByDescending(v => v.ValidTo);
                    break;
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var vouchers = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pagination = new
            {
                currentPage = page,
                pageSize,
                totalItems,
                totalPages
            };

            return (vouchers, pagination);
        }

        public async Task<VoucherViewModel> CreateVoucherAsync(CreateVoucherDTO voucherDto)
        {
            // Check for duplicate code
            if (await _context.Vouchers.AnyAsync(v => v.Code == voucherDto.Code))
            {
                throw new Exception("Mã voucher đã tồn tại. Vui lòng chọn mã khác.");
            }

            var voucher = new Voucher
            {
                Code = voucherDto.Code,
                Description = voucherDto.Description,
                DiscountType = voucherDto.DiscountType,
                DiscountValue = voucherDto.DiscountValue,
                QuantityAvailable = voucherDto.QuantityAvailable,
                ValidFrom = voucherDto.ValidFrom,
                ValidTo = voucherDto.ValidTo,
                IsActive = voucherDto.IsActive,
                AppliesTo = voucherDto.AppliesTo,
                IsSystemWide = voucherDto.IsSystemWide
            };

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            if (voucherDto.AppliesTo == "Product" && voucherDto.ProductIds.Any())
            {
                var voucherProducts = voucherDto.ProductIds.Select(pid => new VoucherProducts
                {
                    VoucherID = voucher.VoucherID,
                    ProductID = pid
                }).ToList();

                _context.VoucherProducts.AddRange(voucherProducts);
                await _context.SaveChangesAsync();
            }

            return new VoucherViewModel
            {
                VoucherId = voucher.VoucherID,
                Code = voucher.Code,
                Description = voucher.Description,
                DiscountType = voucher.DiscountType,
                DiscountValue = voucher.DiscountValue,
                QuantityAvailable = voucher.QuantityAvailable,
                ValidFrom = voucher.ValidFrom,
                ValidTo = voucher.ValidTo,
                IsActive = voucher.IsActive,
                AppliesTo = voucher.AppliesTo,
                IsSystemWide = voucher.IsSystemWide,
                UserCount = 0,
                ProductCount = voucherDto.AppliesTo == "Product" ? voucherDto.ProductIds.Count : 0
            };
        }

        public async Task<VoucherViewModel> UpdateVoucherAsync(UpdateVoucherDTO voucherDto)
        {
            var existingVoucher = await _context.Vouchers
                .Include(v => v.UserVouchers)
                .Include(v => v.VoucherProducts)
                .FirstOrDefaultAsync(v => v.VoucherID == voucherDto.VoucherId);

            if (existingVoucher == null)
                throw new Exception("Không tìm thấy voucher");

            // Check for duplicate code, excluding the current voucher
            if (await _context.Vouchers.AnyAsync(v => v.Code == voucherDto.Code && v.VoucherID != voucherDto.VoucherId))
            {
                throw new Exception("Mã voucher đã tồn tại. Vui lòng chọn mã khác.");
            }

            existingVoucher.Code = voucherDto.Code;
            existingVoucher.Description = voucherDto.Description;
            existingVoucher.DiscountType = voucherDto.DiscountType;
            existingVoucher.DiscountValue = voucherDto.DiscountValue;
            existingVoucher.QuantityAvailable = voucherDto.QuantityAvailable;
            existingVoucher.ValidFrom = voucherDto.ValidFrom;
            existingVoucher.ValidTo = voucherDto.ValidTo;
            existingVoucher.IsActive = voucherDto.IsActive;
            existingVoucher.AppliesTo = voucherDto.AppliesTo;
            existingVoucher.IsSystemWide = voucherDto.IsSystemWide;

            if (voucherDto.AppliesTo == "Product")
            {
                var existingProducts = await _context.VoucherProducts
                    .Where(vp => vp.VoucherID == voucherDto.VoucherId)
                    .ToListAsync();

                _context.VoucherProducts.RemoveRange(existingProducts);

                if (voucherDto.ProductIds.Any())
                {
                    var newVoucherProducts = voucherDto.ProductIds.Select(pid => new VoucherProducts
                    {
                        VoucherID = voucherDto.VoucherId,
                        ProductID = pid
                    }).ToList();

                    _context.VoucherProducts.AddRange(newVoucherProducts);
                }
            }

            await _context.SaveChangesAsync();

            return new VoucherViewModel
            {
                VoucherId = existingVoucher.VoucherID,
                Code = existingVoucher.Code,
                Description = existingVoucher.Description,
                DiscountType = existingVoucher.DiscountType,
                DiscountValue = existingVoucher.DiscountValue,
                QuantityAvailable = existingVoucher.QuantityAvailable,
                ValidFrom = existingVoucher.ValidFrom,
                ValidTo = existingVoucher.ValidTo,
                IsActive = existingVoucher.IsActive,
                AppliesTo = existingVoucher.AppliesTo,
                IsSystemWide = existingVoucher.IsSystemWide,
                UserCount = existingVoucher.UserVouchers.Count,
                ProductCount = existingVoucher.AppliesTo == "Product" ? voucherDto.ProductIds.Count : 0
            };
        }

        public async Task<bool> DeleteVoucherAsync(int id)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.UserVouchers)
                .FirstOrDefaultAsync(v => v.VoucherID == id);

            if (voucher == null)
                return false;

            if (voucher.UserVouchers.Any())
                throw new Exception("Không thể xóa voucher đang được sử dụng");

            var voucherProducts = await _context.VoucherProducts
                .Where(vp => vp.VoucherID == id)
                .ToListAsync();

            if (voucherProducts.Any())
            {
                _context.VoucherProducts.RemoveRange(voucherProducts);
            }

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}