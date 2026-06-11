using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;

namespace Nhom6_QLThietBi_API.Controllers
{
    [ApiController]
    [Route("api/admin/return-requests")]
    public class AdminReturnRequestsController : ControllerBase
    {
        public class ResolveReturnRequestBody
        {
            public string Status { get; set; } = string.Empty;
            public string? TinhTrangTraMay { get; set; }
            public int? NguoiXuLyId { get; set; }
        }

        private readonly AppDbContext _context;

        public AdminReturnRequestsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetReturnRequests()
        {
            var requests = await _context.YeuCauTraMays
                .AsNoTracking()
                .OrderByDescending(x => x.NgayYeuCau)
                .Select(x => new
                {
                    x.Id,
                    x.DonThueId,
                    MaDonThue = x.DonThue == null ? null : x.DonThue.MaDonThue,
                    TenDonVi = x.DonThue == null || x.DonThue.DonVi == null
                        ? null
                        : x.DonThue.DonVi.TenDonVi,
                    TrangThaiDonThue = x.DonThue == null
                        ? null
                        : x.DonThue.TrangThai,
                    NgayBatDau = x.DonThue == null
                        ? (DateTime?)null
                        : x.DonThue.NgayBatDau,
                    NgayKetThucDuKien = x.DonThue == null
                        ? (DateTime?)null
                        : x.DonThue.NgayKetThucDuKien,
                    x.NgayYeuCau,
                    x.LyDo,
                    x.GhiChu,
                    x.TrangThai,
                    x.NguoiXuLyId,
                    NguoiXuLy = x.NguoiXuLy == null
                        ? null
                        : x.NguoiXuLy.HoTen,
                    x.NgayXuLy,
                    Devices = x.DonThue!.ChiTietDonThues.Select(detail => new
                        {
                            detail.Id,
                            detail.MayTinhId,
                            MaTaiSan = detail.MayTinh == null
                                ? null
                                : detail.MayTinh.MaTaiSan,
                            TenMay = detail.MayTinh == null ||
                                     detail.MayTinh.DongMayTinh == null
                                ? null
                                : detail.MayTinh.DongMayTinh.Hang + " " +
                                  detail.MayTinh.DongMayTinh.TenDong,
                            detail.TinhTrangTraMay,
                            detail.TrangThai
                        })
                })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpPatch("{id}/resolve")]
        public async Task<IActionResult> ResolveReturnRequest(
            int id,
            [FromBody] ResolveReturnRequestBody request)
        {
            var allowedStatuses = new[] { "da_nhan_may", "tu_choi" };
            if (!allowedStatuses.Contains(request.Status))
            {
                return BadRequest(new
                {
                    message = "Trạng thái xử lý yêu cầu trả máy không hợp lệ."
                });
            }

            var returnRequest = await _context.YeuCauTraMays
                .Include(x => x.DonThue)
                    .ThenInclude(x => x!.ChiTietDonThues)
                        .ThenInclude(x => x.MayTinh)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (returnRequest == null)
            {
                return NotFound(new { message = "Không tìm thấy yêu cầu trả máy." });
            }

            if (returnRequest.TrangThai != "cho_xu_ly")
            {
                return Conflict(new
                {
                    message = "Yêu cầu trả máy này đã được xử lý."
                });
            }

            if (request.NguoiXuLyId.HasValue)
            {
                var processorExists = await _context.NguoiDungs
                    .AnyAsync(x => x.Id == request.NguoiXuLyId.Value);

                if (!processorExists)
                {
                    return BadRequest(new
                    {
                        message = "Người xử lý không tồn tại."
                    });
                }
            }

            var order = returnRequest.DonThue;
            if (order == null)
            {
                return BadRequest(new
                {
                    message = "Yêu cầu không có đơn thuê hợp lệ."
                });
            }

            returnRequest.TrangThai = request.Status;
            returnRequest.NguoiXuLyId = request.NguoiXuLyId;
            returnRequest.NgayXuLy = DateTime.Now;

            if (request.Status == "tu_choi")
            {
                if (order.TrangThai == "yeu_cau_tra")
                {
                    order.TrangThai = "dang_thue";
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Đã từ chối yêu cầu trả máy." });
            }

            if (string.IsNullOrWhiteSpace(request.TinhTrangTraMay))
            {
                return BadRequest(new
                {
                    message = "Cần nhập tình trạng máy khi nhận lại."
                });
            }

            var detailIds = order.ChiTietDonThues.Select(x => x.Id).ToList();
            var damagedDetailIds = await _context.BaoCaoHuHongs
                .Where(x =>
                    detailIds.Contains(x.ChiTietDonThueId) &&
                    (x.TrangThai == "cho_xu_ly" ||
                     x.TrangThai == "da_xac_nhan"))
                .Select(x => x.ChiTietDonThueId)
                .Distinct()
                .ToListAsync();

            var returnCondition = request.TinhTrangTraMay.Trim();
            foreach (var detail in order.ChiTietDonThues)
            {
                detail.TinhTrangTraMay = returnCondition;
                detail.TrangThai = "da_tra";

                if (detail.MayTinh != null)
                {
                    detail.MayTinh.TinhTrang = damagedDetailIds.Contains(detail.Id)
                        ? "hong"
                        : "san_sang";
                }
            }

            order.TrangThai = "hoan_thanh";
            order.NgayKetThucThucTe = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đã xác nhận nhận lại máy.",
                donThueId = order.Id,
                trangThaiDonThue = order.TrangThai
            });
        }
    }
}
