using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;

namespace Nhom6_QLThietBi_API.Controllers
{
    [Authorize(Roles = "admin,nhan_vien")]
    [ApiController]
    [Route("api/admin/dashboard")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminDashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var totalDevices = await _context.MayTinhs.CountAsync();
            var availableDevices = await _context.MayTinhs.CountAsync(m => m.TinhTrang == "san_sang");
            var rentedDevices = await _context.MayTinhs.CountAsync(m => m.TinhTrang == "dang_thue");
            var maintenanceDevices = await _context.MayTinhs.CountAsync(m => m.TinhTrang == "bao_tri");
            var brokenDevices = await _context.MayTinhs.CountAsync(m => m.TinhTrang == "hong");

            var deviceStatus = await _context.MayTinhs
                .GroupBy(m => m.TinhTrang)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToListAsync();

            var monthlyRevenue = await _context.HoaDons
                .GroupBy(h => new { h.NgayLap.Year, h.NgayLap.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    year = g.Key.Year,
                    month = g.Key.Month,
                    revenue = g.Sum(h => h.TongThanhToan)
                })
                .ToListAsync();

            var upcomingOrders = await _context.DonThues
                .Where(d => d.TrangThai == "dang_thue" || d.TrangThai == "da_duyet")
                .OrderBy(d => d.NgayKetThucDuKien)
                .Take(5)
                .Select(d => new
                {
                    id = d.Id,
                    type = "rental_due",
                    title = d.MaDonThue,
                    dueDate = d.NgayKetThucDuKien,
                    status = d.TrangThai
                })
                .ToListAsync();

            var maintenanceReminders = await _context.MayTinhs
                .Include(m => m.DongMayTinh)
                .Where(m => m.TinhTrang == "bao_tri" || m.TinhTrang == "hong")
                .OrderBy(m => m.Id)
                .Take(5)
                .Select(m => new
                {
                    id = m.Id,
                    type = "device_attention",
                    title = m.MaTaiSan,
                    deviceName = m.DongMayTinh == null
                        ? m.MaTaiSan
                        : m.DongMayTinh.Hang + " " + m.DongMayTinh.TenDong,
                    status = m.TinhTrang
                })
                .ToListAsync();

            return Ok(new
            {
                overview = new
                {
                    totalDevices,
                    availableDevices,
                    rentedDevices,
                    maintenanceDevices,
                    brokenDevices
                },
                deviceStatus,
                monthlyRevenue,
                reminders = new { upcomingOrders, maintenanceReminders }
            });
        }
    }
}
