using CyberTech.Models;
using CyberTech.Models.DTOs;
using CyberTech.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using CyberTech.Data;
using System;
using Microsoft.AspNetCore.Authorization;

namespace CyberTech.Controllers
{
    [Authorize(Roles = "Admin,Staff,Support,Manager,SuperAdmin")]
    [Route("RankManage")]
    public class RankManageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRankService _rankService;

        public RankManageController(ApplicationDbContext context, IRankService rankService)
        {
            _context = context;
            _rankService = rankService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("GetRanks")]
        public async Task<IActionResult> GetRanks(int page = 1, int pageSize = 5, string searchTerm = "", string sortBy = "")
        {
            try
            {
                var result = await _rankService.GetRanksAsync(page, pageSize, searchTerm, sortBy);
                return Json(new { success = true, data = result.ranks, pagination = result.pagination });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải danh sách cấp bậc" });
            }
        }

        [HttpPost("CreateRank")]
        public async Task<IActionResult> CreateRank([FromBody] CreateRankDTO rankDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
                }

                var result = await _rankService.CreateRankAsync(rankDto);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tạo cấp bậc mới" });
            }
        }

        [HttpPost("UpdateRank")]
        public async Task<IActionResult> UpdateRank([FromBody] UpdateRankDTO rankDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
                }

                var result = await _rankService.UpdateRankAsync(rankDto);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("DeleteRank")]
        public async Task<IActionResult> DeleteRank(int id)
        {
            try
            {
                var result = await _rankService.DeleteRankAsync(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}