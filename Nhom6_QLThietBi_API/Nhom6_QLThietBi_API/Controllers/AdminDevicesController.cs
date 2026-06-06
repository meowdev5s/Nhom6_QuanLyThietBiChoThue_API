using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;

namespace Nhom6_QLThietBi_API.Controllers
{
    [ApiController]
    [Route("api/admin/devices")]
    public class AdminDevicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminDevicesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDevices()
        {
            var devices = await _context.MayTinhs
                .Include(m => m.DongMayTinh)
                .OrderBy(m => m.Id)
                .Select(m => new
                {
                    m.Id,
                    m.MaTaiSan,
                    m.SerialNumber,
                    TenDong = m.DongMayTinh == null ? null : m.DongMayTinh.TenDong,
                    Hang = m.DongMayTinh == null ? null : m.DongMayTinh.Hang,
                    m.LoaiMay,
                    m.Cpu,
                    m.Ram,
                    OCung = m.OCung,
                    m.Gpu,
                    m.ManHinh,
                    m.HeDieuHanh,
                    m.GiaTriMay,
                    m.GiaThueNgay,
                    m.TiLeDatCoc,
                    m.TinhTrang,
                    m.NgayNhap,
                    m.GhiChu
                })
                .ToListAsync();

            return Ok(devices);
        }
    }
}
