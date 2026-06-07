using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;

namespace Nhom6_QLThietBi_API.Controllers
{
    [ApiController]
    [Route("api/admin/damage-levels")]
    public class AdminDamageLevelsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminDamageLevelsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDamageLevels()
        {
            var levels = await _context.MucDoHuHongs
                .OrderBy(x => x.PhanTramDenBu)
                .Select(x => new
                {
                    x.Id,
                    x.TenMucDo,
                    x.MoTa,
                    x.PhanTramDenBu
                })
                .ToListAsync();

            return Ok(levels);
        }
    }
}