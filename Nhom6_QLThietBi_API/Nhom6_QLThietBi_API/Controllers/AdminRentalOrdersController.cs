using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;

namespace Nhom6_QLThietBi_API.Controllers
{
    [ApiController]
    [Route("api/admin/rental-orders")]
    public class AdminRentalOrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminRentalOrdersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetRentalOrders()
        {
            var orders = await _context.DonThues
                .Include(d => d.DonVi)
                .Include(d => d.ChiTietDonThues)
                    .ThenInclude(ct => ct.MayTinh)
                        .ThenInclude(mt => mt!.DongMayTinh)
                .OrderByDescending(d => d.NgayTao)
                .Select(d => new
                {
                    d.Id,
                    d.MaDonThue,
                    TenDonVi = d.DonVi == null ? null : d.DonVi.TenDonVi,
                    d.NgayBatDau,
                    d.NgayKetThucDuKien,
                    d.NgayKetThucThucTe,
                    d.MucDichSuDung,
                    d.TrangThai,
                    d.TongTienThue,
                    d.TongTienDatCoc,
                    d.TongTienDenBu,
                    d.GhiChu,
                    Devices = d.ChiTietDonThues.Select(ct => new
                    {
                        ct.Id,
                        ct.MayTinhId,
                        MaTaiSan = ct.MayTinh == null ? null : ct.MayTinh.MaTaiSan,
                        TenMay = ct.MayTinh == null || ct.MayTinh.DongMayTinh == null
                            ? null
                            : ct.MayTinh.DongMayTinh.Hang + " " + ct.MayTinh.DongMayTinh.TenDong,
                        ct.GiaThueNgay,
                        ct.SoNgayThue,
                        ct.ThanhTien,
                        ct.TrangThai
                    })
                })
                .ToListAsync();

            return Ok(orders);
        }
    }
}