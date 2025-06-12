using CyberTech.Models;
using CyberTech.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CyberTech.Services
{
    public interface IRankService
    {
        Task<(IEnumerable<RankViewModel> ranks, object pagination)> GetRanksAsync(int page, int pageSize, string searchTerm, string sortBy);
        Task<RankViewModel> CreateRankAsync(CreateRankDTO rankDto);
        Task<RankViewModel> UpdateRankAsync(UpdateRankDTO rankDto);
        Task<bool> DeleteRankAsync(int id);
    }
}