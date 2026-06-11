using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.DTOs;
using Nhom6_QLThietBi_API.Models;
using Nhom6_QLThietBi_API.Services;

namespace Nhom6_QLThietBi_API.Controllers
{
    [Route("api/user/[controller]")]
    [ApiController]
    public class UserInvoicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserInvoicesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("my-invoices/{userId}")]
        public async Task<IActionResult> GetMyInvoices(int userId)
        {
            var data = await _context.HoaDons
                .Include(h => h.DonThue)
                .Where(h => h.DonThue != null && h.DonThue.NguoiTaoId == userId)
                .Select(h => new InvoiceDto
                {
                    Id = h.Id,
                    DonThueId = h.DonThueId,
                    MaDonThue = h.DonThue!.MaDonThue,
                    MaHoaDon = h.MaHoaDon,
                    TienThue = h.TienThue,
                    TienDatCoc = h.TienDatCoc,
                    TienDenBu = h.TienDenBu,
                    TongThanhToan = h.TongThanhToan,
                    TrangThai = h.TrangThai,
                    NgayLap = h.NgayLap
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpPost("payment")]
        public async Task<IActionResult> PayInvoice([FromBody] PaymentRequestDto request)
        {
            var allowedMethods = new[] { "tien_mat", "chuyen_khoan", "vi_dien_tu" };
            if (request.HoaDonId <= 0)
                return BadRequest(new { message = "Hóa đơn không hợp lệ." });
            if (request.SoTien <= 0)
                return BadRequest(new { message = "Số tiền thanh toán phải lớn hơn 0." });
            if (!allowedMethods.Contains(request.PhuongThuc))
                return BadRequest(new { message = "Phương thức thanh toán không hợp lệ." });

            var invoice = await _context.HoaDons
                .Include(x => x.ThanhToans)
                .Include(x => x.DonThue)
                .FirstOrDefaultAsync(x => x.Id == request.HoaDonId);

            if (invoice == null)
                return NotFound(new { message = "Không tìm thấy hóa đơn." });
            if (invoice.TrangThai == "huy")
                return Conflict(new { message = "Hóa đơn đã bị hủy." });

            var paidAmount = invoice.ThanhToans.Sum(x => x.SoTien);
            var remaining = invoice.TongThanhToan - paidAmount;
            if (remaining <= 0 || invoice.TrangThai == "da_thanh_toan")
                return Conflict(new { message = "Hóa đơn đã được thanh toán đủ." });
            if (request.SoTien > remaining)
            {
                return BadRequest(new
                {
                    message = "Số tiền thanh toán vượt quá số tiền còn lại.",
                    remaining
                });
            }

            var payment = new ThanhToan
            {
                HoaDonId = invoice.Id,
                SoTien = request.SoTien,
                PhuongThuc = request.PhuongThuc,
                MaGiaoDich = Normalize(request.MaGiaoDich),
                GhiChu = Normalize(request.GhiChu),
                NgayThanhToan = DateTime.Now
            };
            _context.ThanhToans.Add(payment);

            var newPaidAmount = paidAmount + request.SoTien;
            invoice.TrangThai = newPaidAmount >= invoice.TongThanhToan
                ? "da_thanh_toan"
                : "thanh_toan_mot_phan";

            if (invoice.TrangThai == "da_thanh_toan")
            {
                var damageReports = await _context.BaoCaoHuHongs
                    .Where(x => x.ChiTietDonThue != null &&
                                x.ChiTietDonThue.DonThueId == invoice.DonThueId &&
                                x.TrangThai == "da_xac_nhan")
                    .ToListAsync();
                foreach (var report in damageReports)
                    report.TrangThai = "da_thanh_toan";
            }

            await _context.SaveChangesAsync();
            var orderCompleted = await RentalOrderLifecycleService.SyncCompletionAsync(
                _context,
                invoice.DonThueId);

            return Ok(new
            {
                message = invoice.TrangThai == "da_thanh_toan"
                    ? "Hóa đơn đã được thanh toán đủ."
                    : "Đã ghi nhận thanh toán một phần.",
                paymentId = payment.Id,
                invoiceId = invoice.Id,
                invoiceStatus = invoice.TrangThai,
                paidAmount = newPaidAmount,
                remaining = invoice.TongThanhToan - newPaidAmount,
                orderCompleted,
                orderStatus = invoice.DonThue?.TrangThai
            });
        }

        [HttpGet("contract/{donThueId}")]
        public async Task<IActionResult> GetContract(int donThueId)
        {
            var contract = await _context.HopDongs
                .FirstOrDefaultAsync(x => x.DonThueId == donThueId);
            if (contract == null)
                return NotFound(new { message = "Chưa có hợp đồng cho đơn thuê này." });

            return Ok(contract);
        }

        private static string? Normalize(string? value)
        {
            var text = value?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
    }
}
