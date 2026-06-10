using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;
using Nhom6_QLThietBi_API.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nhom6_QLThietBi_API.Controllers
{
    [Route("api/user/[controller]")]
    [ApiController]
    public class UserInvoicesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UserInvoicesController(AppDbContext context) => _context = context;

        [HttpGet("my-invoices/{userId}")]
        public async Task<IActionResult> GetMyInvoices(int userId)
        {
            var data = await _context.HoaDons
                .Include(h => h.DonThue)
                .Where(h => h.DonThue.NguoiTaoId == userId)
                .Select(h => new InvoiceDto
                {
                    Id = h.Id,
                    DonThueId = h.DonThueId,
                    MaDonThue = h.DonThue.MaDonThue,
                    MaHoaDon = h.MaHoaDon,
                    TienThue = h.TienThue,
                    TienDatCoc = h.TienDatCoc,
                    TienDenBu = h.TienDenBu,
                    TongThanhToan = h.TongThanhToan,
                    TrangThai = h.TrangThai,
                    NgayLap = h.NgayLap
                }).ToListAsync();
            return Ok(data);
        }

        [HttpPost("payment")]
        public async Task<IActionResult> PayInvoice([FromBody] PaymentRequestDto req)
        {
            var hd = await _context.HoaDons.FindAsync(req.HoaDonId);
            if (hd == null) return NotFound("Không thấy hóa đơn");

            var tt = new ThanhToan
            {
                HoaDonId = req.HoaDonId,
                SoTien = req.SoTien,
                PhuongThuc = req.PhuongThuc,
                MaGiaoDich = req.MaGiaoDich,
                GhiChu = req.GhiChu,
                NgayThanhToan = DateTime.Now
            };

            _context.ThanhToans.Add(tt);
            hd.TrangThai = "da_thanh_toan";

            await _context.SaveChangesAsync();
            return Ok("Ghi nhận thanh toán thành công");
        }

        [HttpGet("contract/{donThueId}")]
        public async Task<IActionResult> GetContract(int donThueId)
        {
            var hd = await _context.HopDongs
                .FirstOrDefaultAsync(x => x.DonThueId == donThueId);
            if (hd == null) return NotFound("Chưa có hợp đồng cho đơn thuê này.");
            return Ok(hd);
        }
    }
}