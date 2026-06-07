using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;

namespace Nhom6_QLThietBi_API.Controllers
{
    [ApiController]
    [Route("api/admin/invoices")]
    public class AdminInvoicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminInvoicesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoices()
        {
            var invoices = await _context.HoaDons
                .Include(h => h.DonThue)
                    .ThenInclude(d => d!.DonVi)
                .OrderByDescending(h => h.NgayLap)
                .Select(h => new
                {
                    h.Id,
                    h.DonThueId,
                    h.MaHoaDon,
                    MaDonThue = h.DonThue == null ? null : h.DonThue.MaDonThue,
                    TenDonVi = h.DonThue == null || h.DonThue.DonVi == null
                        ? null
                        : h.DonThue.DonVi.TenDonVi,
                    h.TienThue,
                    h.TienDatCoc,
                    h.TienDenBu,
                    h.TongThanhToan,
                    h.TrangThai,
                    h.NgayLap
                })
                .ToListAsync();

            return Ok(invoices);
        }
    }
}