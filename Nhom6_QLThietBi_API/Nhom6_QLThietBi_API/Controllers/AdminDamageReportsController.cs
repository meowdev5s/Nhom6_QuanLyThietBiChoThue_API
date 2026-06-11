using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;

namespace Nhom6_QLThietBi_API.Controllers
{
    [Authorize(Roles = "admin,nhan_vien")]
    [ApiController]
    [Route("api/admin/damage-reports")]
    public class AdminDamageReportsController : ControllerBase
    {
        public class ResolveDamageReportRequest
        {
            public string Status { get; set; } = string.Empty;
            public int? MucDoHuHongId { get; set; }
        }

        private readonly AppDbContext _context;

        public AdminDamageReportsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDamageReports()
        {
            var reports = await _context.BaoCaoHuHongs
                .AsNoTracking()
                .OrderByDescending(x => x.NgayBaoCao)
                .Select(x => new
                {
                    x.Id,
                    x.ChiTietDonThueId,
                    x.NguoiBaoCaoId,
                    NguoiBaoCao = x.NguoiBaoCao == null
                        ? null
                        : x.NguoiBaoCao.HoTen,
                    x.MoTa,
                    x.HinhAnhUrl,
                    x.MucDoHuHongId,
                    TenMucDo = x.MucDoHuHong == null
                        ? null
                        : x.MucDoHuHong.TenMucDo,
                    PhanTramDenBu = x.MucDoHuHong == null
                        ? (decimal?)null
                        : x.MucDoHuHong.PhanTramDenBu,
                    x.TienDenBu,
                    x.TrangThai,
                    x.NgayBaoCao,
                    MayTinhId = x.ChiTietDonThue == null
                        ? 0
                        : x.ChiTietDonThue.MayTinhId,
                    MaTaiSan = x.ChiTietDonThue == null ||
                               x.ChiTietDonThue.MayTinh == null
                        ? null
                        : x.ChiTietDonThue.MayTinh.MaTaiSan,
                    TenMay = x.ChiTietDonThue == null ||
                             x.ChiTietDonThue.MayTinh == null ||
                             x.ChiTietDonThue.MayTinh.DongMayTinh == null
                        ? null
                        : x.ChiTietDonThue.MayTinh.DongMayTinh.TenDong,
                    GiaTriMay = x.ChiTietDonThue == null
                        ? 0
                        : x.ChiTietDonThue.GiaTriMayTaiThoiDiemThue,
                    DonThueId = x.ChiTietDonThue == null
                        ? 0
                        : x.ChiTietDonThue.DonThueId,
                    MaDonThue = x.ChiTietDonThue == null ||
                                x.ChiTietDonThue.DonThue == null
                        ? null
                        : x.ChiTietDonThue.DonThue.MaDonThue,
                    TenDonVi = x.ChiTietDonThue == null ||
                               x.ChiTietDonThue.DonThue == null ||
                               x.ChiTietDonThue.DonThue.DonVi == null
                        ? null
                        : x.ChiTietDonThue.DonThue.DonVi.TenDonVi
                })
                .ToListAsync();

            return Ok(reports);
        }

        [HttpPatch("{id}/resolve")]
        public async Task<IActionResult> ResolveDamageReport(
            int id,
            [FromBody] ResolveDamageReportRequest request)
        {
            var allowedStatuses = new[] { "da_xac_nhan", "da_tu_choi" };
            if (!allowedStatuses.Contains(request.Status))
            {
                return BadRequest(new
                {
                    message = "Trạng thái xử lý báo cáo không hợp lệ."
                });
            }

            var report = await _context.BaoCaoHuHongs
                .Include(x => x.ChiTietDonThue)
                    .ThenInclude(x => x!.DonThue)
                        .ThenInclude(x => x!.HoaDons)
                            .ThenInclude(x => x.ThanhToans)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (report == null)
            {
                return NotFound(new { message = "Không tìm thấy báo cáo hư hỏng." });
            }

            if (report.TrangThai != "cho_xu_ly")
            {
                return Conflict(new
                {
                    message = "Báo cáo này đã được xử lý trước đó."
                });
            }

            if (request.Status == "da_tu_choi")
            {
                report.MucDoHuHongId = null;
                report.TienDenBu = 0;
                report.TrangThai = "da_tu_choi";
                await _context.SaveChangesAsync();

                return Ok(new { message = "Đã từ chối báo cáo hư hỏng." });
            }

            if (!request.MucDoHuHongId.HasValue)
            {
                return BadRequest(new
                {
                    message = "Cần chọn mức độ hư hỏng khi xác nhận báo cáo."
                });
            }

            var level = await _context.MucDoHuHongs
                .FirstOrDefaultAsync(x => x.Id == request.MucDoHuHongId.Value);

            if (level == null)
            {
                return NotFound(new { message = "Không tìm thấy mức độ hư hỏng." });
            }

            var rentalItem = report.ChiTietDonThue;
            if (rentalItem == null || rentalItem.DonThue == null)
            {
                return BadRequest(new
                {
                    message = "Báo cáo không có thông tin chi tiết đơn thuê hợp lệ."
                });
            }

            var compensation = Math.Round(
                rentalItem.GiaTriMayTaiThoiDiemThue *
                level.PhanTramDenBu / 100,
                2);

            report.MucDoHuHongId = level.Id;
            report.TienDenBu = compensation;
            report.TrangThai = "da_xac_nhan";

            var rentalOrder = rentalItem.DonThue;
            rentalOrder.TongTienDenBu += compensation;

            var invoice = rentalOrder.HoaDons
                .Where(x => x.TrangThai != "huy")
                .OrderByDescending(x => x.NgayLap)
                .FirstOrDefault();

            if (invoice != null)
            {
                invoice.TienDenBu += compensation;
                invoice.TongThanhToan += compensation;

                var paidAmount = invoice.ThanhToans.Sum(x => x.SoTien);
                invoice.TrangThai = paidAmount <= 0
                    ? "chua_thanh_toan"
                    : paidAmount >= invoice.TongThanhToan
                        ? "da_thanh_toan"
                        : "thanh_toan_mot_phan";
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đã xác nhận báo cáo hư hỏng.",
                tienDenBu = compensation
            });
        }
    }
}
