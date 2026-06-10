using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.DTOs;
using Nhom6_QLThietBi_API.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Nhom6_QLThietBi_API.Controllers
{
    [Route("api/user/[controller]")]
    [ApiController]
    public class UserDevicesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UserDevicesController(AppDbContext context) => _context = context;

        [HttpGet("catalog")]
        public async Task<IActionResult> GetCatalog()
        {
            var data = await _context.MayTinhs
                .Include(m => m.DongMayTinh)
                .Include(m => m.AnhMayTinhs)
                .Where(m => m.TinhTrang == "san_ready" || m.TinhTrang == "san_sang")
                .Select(m => new DeviceCatalogDto
                {
                    Id = m.Id,
                    Name = m.DongMayTinh.Hang + " " + m.DongMayTinh.TenDong,
                    Cpu = m.Cpu,
                    Ram = m.Ram,
                    Ssd = m.OCung,
                    Gpu = m.Gpu,
                    Display = m.ManHinh,
                    Price = m.GiaThueNgay,
                    ImageUrl = m.AnhMayTinhs.FirstOrDefault(a => a.LaAnhDaiDien) != null
                        ? m.AnhMayTinhs.FirstOrDefault(a => a.LaAnhDaiDien)!.DuongDanAnh : "assets/images/Lap1.jpg"
                }).ToListAsync();
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var m = await _context.MayTinhs
                .Include(m => m.DongMayTinh)
                .Include(m => m.AnhMayTinhs)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (m == null) return NotFound("Không tìm thấy thiết bị");

            var dto = new DeviceDetailDto
            {
                Id = m.Id,
                Name = m.DongMayTinh.TenDong,
                Hang = m.DongMayTinh.Hang,
                Cpu = m.Cpu,
                Ram = m.Ram,
                Ssd = m.OCung,
                Gpu = m.Gpu,
                Display = m.ManHinh,
                HeDieuHanh = m.HeDieuHanh,
                GiaThueNgay = m.GiaThueNgay,
                GiaTriMay = m.GiaTriMay,
                TiLeDatCoc = m.TiLeDatCoc,
                TienDatCocDuKien = m.GiaTriMay * (m.TiLeDatCoc / 100),
                TinhTrang = m.TinhTrang,
                ImageUrl = m.AnhMayTinhs.FirstOrDefault(a => a.LaAnhDaiDien) != null
                    ? m.AnhMayTinhs.FirstOrDefault(a => a.LaAnhDaiDien)!.DuongDanAnh : "assets/images/Lap1.jpg"
            };
            return Ok(dto);
        }
    }
}