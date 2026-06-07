using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;

namespace Nhom6_QLThietBi_API.Controllers
{
    [ApiController]
    [Route("api/admin/computer-lines")]
    public class AdminComputerLinesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminComputerLinesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetComputerLines()
        {
            var lines = await _context.DongMayTinhs
                .Include(d => d.MayTinhs)
                .OrderBy(d => d.Hang)
                .ThenBy(d => d.TenDong)
                .Select(d => new
                {
                    d.Id,
                    d.TenDong,
                    d.Hang,
                    d.MoTa,
                    SoLuongMay = d.MayTinhs.Count
                })
                .ToListAsync();

            return Ok(lines);
        }
    }
}