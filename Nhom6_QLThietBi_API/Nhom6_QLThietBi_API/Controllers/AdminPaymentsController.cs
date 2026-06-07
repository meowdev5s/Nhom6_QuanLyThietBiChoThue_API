using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;

namespace Nhom6_QLThietBi_API.Controllers
{
    [ApiController]
    [Route("api/admin/payments")]
    public class AdminPaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminPaymentsController(AppDbContext context)
        {
            _context = context;
        }

        public class CreatePaymentRequest
        {
            public int HoaDonId { get; set; }
            public decimal SoTien { get; set; }
            public string PhuongThuc { get; set; } = string.Empty;
            public string? MaGiaoDich { get; set; }
            public string? GhiChu { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetPayments()
        {
            var payments = await _context.ThanhToans
                .Include(t => t.HoaDon)
                    .ThenInclude(h => h!.DonThue)
                        .ThenInclude(d => d!.DonVi)
                .OrderByDescending(t => t.NgayThanhToan)
                .Select(t => new
                {
                    t.Id,
                    t.HoaDonId,
                    MaHoaDon = t.HoaDon == null ? null : t.HoaDon.MaHoaDon,
                    MaDonThue = t.HoaDon == null || t.HoaDon.DonThue == null
                        ? null
                        : t.HoaDon.DonThue.MaDonThue,
                    TenDonVi = t.HoaDon == null ||
                               t.HoaDon.DonThue == null ||
                               t.HoaDon.DonThue.DonVi == null
                        ? null
                        : t.HoaDon.DonThue.DonVi.TenDonVi,
                    t.SoTien,
                    t.PhuongThuc,
                    t.MaGiaoDich,
                    t.GhiChu,
                    t.NgayThanhToan
                })
                .ToListAsync();

            return Ok(payments);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment(
            [FromBody] CreatePaymentRequest request
        )
        {
            var allowedMethods = new[]
            {
                "tien_mat",
                "chuyen_khoan",
                "vi_dien_tu"
            };

            if (request.HoaDonId <= 0)
            {
                return BadRequest(new { message = "Hóa đơn không hợp lệ." });
            }

            if (request.SoTien <= 0)
            {
                return BadRequest(new { message = "Số tiền thanh toán phải lớn hơn 0." });
            }

            if (!allowedMethods.Contains(request.PhuongThuc))
            {
                return BadRequest(new { message = "Phương thức thanh toán không hợp lệ." });
            }

            var invoice = await _context.HoaDons
                .Include(h => h.ThanhToans)
                .FirstOrDefaultAsync(h => h.Id == request.HoaDonId);

            if (invoice == null)
            {
                return NotFound(new { message = "Không tìm thấy hóa đơn." });
            }

            if (invoice.TrangThai == "huy")
            {
                return BadRequest(new { message = "Hóa đơn đã hủy, không thể thanh toán." });
            }

            if (invoice.TrangThai == "da_thanh_toan")
            {
                return BadRequest(new { message = "Hóa đơn đã thanh toán đủ." });
            }

            var paidAmount = invoice.ThanhToans.Sum(t => t.SoTien);
            var newPaidAmount = paidAmount + request.SoTien;

            if (request.SoTien > invoice.TongThanhToan - paidAmount)
            {
                return BadRequest(new { message = "Số tiền thanh toán vượt quá số tiền còn lại." });
            }

            if (newPaidAmount >= invoice.TongThanhToan)
            {
                invoice.TrangThai = "da_thanh_toan";
            }
            else
            {
                invoice.TrangThai = "thanh_toan_mot_phan";
            }

            var payment = new ThanhToan
            {
                HoaDonId = request.HoaDonId,
                SoTien = request.SoTien,
                PhuongThuc = request.PhuongThuc,
                MaGiaoDich = string.IsNullOrWhiteSpace(request.MaGiaoDich)
                    ? null
                    : request.MaGiaoDich.Trim(),
                GhiChu = string.IsNullOrWhiteSpace(request.GhiChu)
                    ? null
                    : request.GhiChu.Trim(),
                NgayThanhToan = DateTime.Now
            };

            _context.ThanhToans.Add(payment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetPayments),
                new { id = payment.Id },
                new
                {
                    message = "Ghi nhận thanh toán thành công.",
                    id = payment.Id,
                    hoaDonId = payment.HoaDonId,
                    trangThaiHoaDon = invoice.TrangThai
                }
            );
        }
    }
}