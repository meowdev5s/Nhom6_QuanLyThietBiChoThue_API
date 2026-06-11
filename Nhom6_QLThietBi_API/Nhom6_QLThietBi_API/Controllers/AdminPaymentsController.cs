using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;

namespace Nhom6_QLThietBi_API.Controllers
{
    [Authorize(Roles = "admin,nhan_vien")]
    [ApiController]
    [Route("api/admin/payments")]
    public class AdminPaymentsController : ControllerBase
    {
        public class CreatePaymentRequest
        {
            public int DonThueId { get; set; }
            public decimal SoTien { get; set; }
            public string PhuongThuc { get; set; } = string.Empty;
            public string? MaGiaoDich { get; set; }
            public string? GhiChu { get; set; }
        }

        public class CreatePaymentAdjustmentRequest
        {
            public decimal SoTien { get; set; }
            public string LyDo { get; set; } = string.Empty;
        }

        private static readonly string[] PayableOrderStatuses =
        {
            "da_duyet", "dang_thue", "yeu_cau_tra", "hoan_thanh", "qua_han"
        };

        private readonly AppDbContext _context;

        public AdminPaymentsController(AppDbContext context)
        {
            _context = context;
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
                        ? null : t.HoaDon.DonThue.MaDonThue,
                    TenDonVi = t.HoaDon == null ||
                               t.HoaDon.DonThue == null ||
                               t.HoaDon.DonThue.DonVi == null
                        ? null : t.HoaDon.DonThue.DonVi.TenDonVi,
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
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            var allowedMethods = new[] { "tien_mat", "chuyen_khoan", "vi_dien_tu" };
            if (request.DonThueId <= 0)
                return BadRequest(new { message = "Đơn thuê không hợp lệ." });
            if (request.SoTien <= 0)
                return BadRequest(new { message = "Số tiền thanh toán phải lớn hơn 0." });
            if (!allowedMethods.Contains(request.PhuongThuc))
                return BadRequest(new { message = "Phương thức thanh toán không hợp lệ." });

            var order = await _context.DonThues
                .Include(x => x.HoaDons)
                    .ThenInclude(x => x.ThanhToans)
                .FirstOrDefaultAsync(x => x.Id == request.DonThueId);
            if (order == null)
                return NotFound(new { message = "Không tìm thấy đơn thuê." });
            if (!PayableOrderStatuses.Contains(order.TrangThai))
                return BadRequest(new { message = "Đơn thuê chưa đủ điều kiện thanh toán." });

            var invoice = order.HoaDons
                .Where(x => x.TrangThai != "huy")
                .OrderByDescending(x => x.NgayLap)
                .FirstOrDefault();

            if (invoice == null)
            {
                var total = order.TongTienThue + order.TongTienDatCoc + order.TongTienDenBu;
                if (total <= 0)
                    return BadRequest(new { message = "Đơn thuê chưa có khoản tiền cần thanh toán." });

                invoice = new HoaDon
                {
                    DonThueId = order.Id,
                    MaHoaDon = await GenerateInvoiceCode(),
                    TienThue = order.TongTienThue,
                    TienDatCoc = order.TongTienDatCoc,
                    TienDenBu = order.TongTienDenBu,
                    TongThanhToan = total,
                    TrangThai = "chua_thanh_toan",
                    NgayLap = DateTime.Now
                };
                _context.HoaDons.Add(invoice);
            }

            if (invoice.TrangThai == "da_thanh_toan")
                return BadRequest(new { message = "Hóa đơn của đơn thuê đã thanh toán đủ." });

            var paidAmount = invoice.ThanhToans.Sum(x => x.SoTien);
            var remaining = invoice.TongThanhToan - paidAmount;
            if (request.SoTien > remaining)
                return BadRequest(new { message = "Số tiền thanh toán vượt quá số tiền còn lại." });

            var payment = new ThanhToan
            {
                HoaDon = invoice,
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
                                x.ChiTietDonThue.DonThueId == order.Id &&
                                x.TrangThai == "da_xac_nhan")
                    .ToListAsync();
                foreach (var report in damageReports)
                    report.TrangThai = "da_thanh_toan";
            }

            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = "Ghi nhận thanh toán và cập nhật hóa đơn thành công.",
                paymentId = payment.Id,
                invoiceId = invoice.Id,
                invoiceCode = invoice.MaHoaDon,
                invoiceStatus = invoice.TrangThai
            });
        }

        [HttpPost("{id}/adjustments")]
        public async Task<IActionResult> CreateAdjustment(
            int id,
            [FromBody] CreatePaymentAdjustmentRequest request)
        {
            if (request.SoTien <= 0)
                return BadRequest(new { message = "Số tiền điều chỉnh phải lớn hơn 0." });

            var reason = Normalize(request.LyDo);
            if (reason == null)
                return BadRequest(new { message = "Vui lòng nhập lý do điều chỉnh." });

            var payment = await _context.ThanhToans
                .Include(x => x.HoaDon)
                    .ThenInclude(x => x!.DonThue)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (payment == null)
                return NotFound(new { message = "Không tìm thấy giao dịch thanh toán." });
            if (payment.SoTien <= 0)
                return BadRequest(new { message = "Không thể điều chỉnh một giao dịch điều chỉnh." });
            if (payment.HoaDon == null)
                return Conflict(new { message = "Giao dịch không còn liên kết với hóa đơn." });

            var adjustmentPrefix = $"ADJ-{payment.Id}-";
            var adjustedAmount = -(await _context.ThanhToans
                .Where(x => x.HoaDonId == payment.HoaDonId &&
                            x.MaGiaoDich != null &&
                            x.MaGiaoDich.StartsWith(adjustmentPrefix))
                .SumAsync(x => x.SoTien));
            var adjustableAmount = payment.SoTien - adjustedAmount;

            if (request.SoTien > adjustableAmount)
                return BadRequest(new
                {
                    message = "Số tiền điều chỉnh vượt quá phần còn có thể điều chỉnh của giao dịch."
                });

            var adjustment = new ThanhToan
            {
                HoaDonId = payment.HoaDonId,
                SoTien = -request.SoTien,
                PhuongThuc = payment.PhuongThuc,
                MaGiaoDich = $"{adjustmentPrefix}{DateTime.Now:yyyyMMddHHmmssfff}",
                GhiChu = $"Điều chỉnh giao dịch #{payment.Id}: {reason}",
                NgayThanhToan = DateTime.Now
            };
            _context.ThanhToans.Add(adjustment);

            var paidAmount = await _context.ThanhToans
                .Where(x => x.HoaDonId == payment.HoaDonId)
                .SumAsync(x => x.SoTien) - request.SoTien;

            payment.HoaDon.TrangThai = paidAmount <= 0
                ? "chua_thanh_toan"
                : paidAmount >= payment.HoaDon.TongThanhToan
                    ? "da_thanh_toan"
                    : "thanh_toan_mot_phan";

            if (payment.HoaDon.TrangThai != "da_thanh_toan" &&
                payment.HoaDon.DonThueId > 0)
            {
                var damageReports = await _context.BaoCaoHuHongs
                    .Where(x => x.ChiTietDonThue != null &&
                                x.ChiTietDonThue.DonThueId == payment.HoaDon.DonThueId &&
                                x.TrangThai == "da_thanh_toan")
                    .ToListAsync();
                foreach (var report in damageReports)
                    report.TrangThai = "da_xac_nhan";
            }

            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = "Đã ghi nhận điều chỉnh thanh toán.",
                adjustmentId = adjustment.Id,
                invoiceStatus = payment.HoaDon.TrangThai
            });
        }

        private async Task<string> GenerateInvoiceCode()
        {
            var prefix = "HD-" + DateTime.Now.ToString("yyyyMMdd") + "-";
            var count = await _context.HoaDons.CountAsync(x => x.MaHoaDon.StartsWith(prefix));
            string code;
            do
            {
                count++;
                code = prefix + count.ToString("D3");
            }
            while (await _context.HoaDons.AnyAsync(x => x.MaHoaDon == code));
            return code;
        }

        private static string? Normalize(string? value)
        {
            var text = value?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
    }
}
