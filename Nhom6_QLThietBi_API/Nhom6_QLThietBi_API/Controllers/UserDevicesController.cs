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
                .Where(m => m.TinhTrang == "san_sang")
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
                    MachineValue = m.GiaTriMay,
                    DepositRate = m.TiLeDatCoc,
                    TienDatCocDuKien =
                        m.GiaTriMay * (m.TiLeDatCoc / 100),
                    ImageUrl = m.AnhMayTinhs.FirstOrDefault(a => a.LaAnhDaiDien) != null
                        ? m.AnhMayTinhs.FirstOrDefault(a => a.LaAnhDaiDien)!.DuongDanAnh : "assets/images/Lap1.jpg"
                }).ToListAsync();
            return Ok(data);
        }

        [HttpGet("my-assets/{userId}")]
        public async Task<IActionResult> GetMyAssets(int userId)
        {
            var assets = await _context.ChiTietDonThues
                .AsNoTracking()
                .Include(x => x.DonThue)
                .Include(x => x.MayTinh)!.ThenInclude(x => x!.DongMayTinh)
                .Where(x => x.DonThue != null &&
                            x.DonThue.NguoiTaoId == userId &&
                            (x.DonThue.TrangThai == "dang_thue" ||
                             x.DonThue.TrangThai == "qua_han" ||
                             x.DonThue.TrangThai == "yeu_cau_tra") &&
                            x.TrangThai != "da_tra" && x.TrangThai != "huy")
                .Select(x => new
                {
                    chiTietId = x.Id,
                    mayTinhId = x.MayTinhId,
                    maTaiSan = x.MayTinh == null ? null : x.MayTinh.MaTaiSan,
                    name = x.MayTinh == null || x.MayTinh.DongMayTinh == null
                        ? null
                        : x.MayTinh.DongMayTinh.Hang + " " + x.MayTinh.DongMayTinh.TenDong,
                    status = x.TrangThai,
                    maDonThue = x.DonThue!.MaDonThue
                })
                .ToListAsync();
            return Ok(assets);
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
                Name = m.DongMayTinh == null
                    ? m.MaTaiSan
                    : m.DongMayTinh.Hang + " " + m.DongMayTinh.TenDong,
                Hang = m.DongMayTinh == null ? string.Empty : m.DongMayTinh.Hang,
                MaTaiSan = m.MaTaiSan,
                SerialNumber = m.SerialNumber,
                LoaiMay = m.LoaiMay,
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
                GhiChu = m.GhiChu,
                ImageUrl = m.AnhMayTinhs.FirstOrDefault(a => a.LaAnhDaiDien) != null
                    ? m.AnhMayTinhs.FirstOrDefault(a => a.LaAnhDaiDien)!.DuongDanAnh
                    : m.AnhMayTinhs.OrderBy(a => a.NgayTao)
                        .Select(a => a.DuongDanAnh)
                        .FirstOrDefault() ?? "assets/images/Lap1.jpg",
                ImageUrls = m.AnhMayTinhs
                    .OrderByDescending(a => a.LaAnhDaiDien)
                    .ThenBy(a => a.NgayTao)
                    .Select(a => a.DuongDanAnh)
                    .ToList()
            };
            return Ok(dto);
        }
    }
}
