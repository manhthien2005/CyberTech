using CyberTech.Data;
using CyberTech.Models;
using CyberTech.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CyberTech.Services
{
    public class RankService : IRankService
    {
        private readonly ApplicationDbContext _context;

        public RankService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<RankViewModel> ranks, object pagination)> GetRanksAsync(int page, int pageSize, string searchTerm, string sortBy)
        {
            var query = _context.Ranks
                .Include(r => r.Users)
                .Select(r => new RankViewModel
                {
                    RankId = r.RankId,
                    RankName = r.RankName,
                    MinTotalSpent = r.MinTotalSpent,
                    DiscountPercent = r.DiscountPercent,
                    PriorityLevel = r.PriorityLevel,
                    Description = r.Description,
                    UserCount = r.Users.Count
                })
                .AsQueryable();

            // Apply search
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(r => r.RankName.ToLower().Contains(searchTerm) ||
                                      (r.Description != null && r.Description.ToLower().Contains(searchTerm)));
            }

            // Apply sorting
            query = sortBy switch
            {
                "name_desc" => query.OrderByDescending(r => r.RankName),
                "name_asc" => query.OrderBy(r => r.RankName),
                "priority_desc" => query.OrderByDescending(r => r.PriorityLevel),
                "priority_asc" => query.OrderBy(r => r.PriorityLevel),
                "discount_desc" => query.OrderByDescending(r => r.DiscountPercent),
                "discount_asc" => query.OrderBy(r => r.DiscountPercent),
                _ => query.OrderBy(r => r.PriorityLevel)
            };

            // Calculate pagination
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var ranks = await query
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

            return (ranks, pagination);
        }

        public async Task<RankViewModel> CreateRankAsync(CreateRankDTO rankDto)
        {
            var rank = new Rank
            {
                RankName = rankDto.RankName,
                MinTotalSpent = rankDto.MinTotalSpent,
                DiscountPercent = rankDto.DiscountPercent,
                PriorityLevel = rankDto.PriorityLevel,
                Description = rankDto.Description
            };

            _context.Ranks.Add(rank);
            await _context.SaveChangesAsync();

            return new RankViewModel
            {
                RankId = rank.RankId,
                RankName = rank.RankName,
                MinTotalSpent = rank.MinTotalSpent,
                DiscountPercent = rank.DiscountPercent,
                PriorityLevel = rank.PriorityLevel,
                Description = rank.Description,
                UserCount = 0
            };
        }

        public async Task<RankViewModel> UpdateRankAsync(UpdateRankDTO rankDto)
        {
            var existingRank = await _context.Ranks
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.RankId == rankDto.RankId);

            if (existingRank == null)
                throw new Exception("Không tìm thấy cấp bậc");

            existingRank.RankName = rankDto.RankName;
            existingRank.MinTotalSpent = rankDto.MinTotalSpent;
            existingRank.DiscountPercent = rankDto.DiscountPercent;
            existingRank.PriorityLevel = rankDto.PriorityLevel;
            existingRank.Description = rankDto.Description;

            await _context.SaveChangesAsync();

            return new RankViewModel
            {
                RankId = existingRank.RankId,
                RankName = existingRank.RankName,
                MinTotalSpent = existingRank.MinTotalSpent,
                DiscountPercent = existingRank.DiscountPercent,
                PriorityLevel = existingRank.PriorityLevel,
                Description = existingRank.Description,
                UserCount = existingRank.Users.Count
            };
        }

        public async Task<bool> DeleteRankAsync(int id)
        {
            var rank = await _context.Ranks
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.RankId == id);

            if (rank == null)
                return false;

            if (rank.Users.Any())
                throw new Exception("Không thể xóa cấp bậc đang được sử dụng");

            _context.Ranks.Remove(rank);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}