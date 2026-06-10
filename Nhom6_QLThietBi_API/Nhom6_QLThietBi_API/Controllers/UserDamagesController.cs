using Microsoft.AspNetCore.Mvc;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;
using Nhom6_QLThietBi_API.DTOs;
using System;
using System.Threading.Tasks;

namespace Nhom6_QLThietBi_API.Controllers
{
    [Route("api/user/[controller]")]
    [ApiController]
    public class UserDamagesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UserDamagesController(AppDbContext context) => _context = context;

        [HttpPost("report")]
        public async Task<IActionResult> ReportDamage([FromBody] ReportDamageRequestDto req)
        {
            var bc = new BaoCaoHuHong
            {
                ChiTietDonThueId = req.ChiTietDonThueId,
                NguoiBaoCaoId = req.NguoiBaoCaoId,
                MoTa = req.MoTa,
                HinhAnhUrl = req.HinhAnhUrl,
                TrangThai = "cho_xu_ly",
                NgayBaoCao = DateTime.Now
            };
            _context.BaoCaoHuHongs.Add(bc);
            await _context.SaveChangesAsync();
            return Ok("Đã gửi báo cáo hư hỏng");
        }
    }
}