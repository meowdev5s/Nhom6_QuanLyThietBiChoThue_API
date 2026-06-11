using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;

namespace Nhom6_QLThietBi_API.Controllers
{
    [Authorize(Roles = "admin,nhan_vien")]
    [ApiController]
    [Route("api/admin/extension-requests")]
    public class AdminExtensionRequestsController : ControllerBase
    {
        public class ResolveExtensionRequestBody
        {
            public string Status { get; set; } = string.Empty;
            public int? NguoiDuyetId { get; set; }
        }

        private readonly AppDbContext _context;

        public AdminExtensionRequestsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetExtensionRequests()
        {
            var requests = await _context.YeuCauGiaHans
                .AsNoTracking()
                .OrderByDescending(x => x.NgayTao)
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
                    NgayKetThucHienTai = x.DonThue == null
                        ? (DateTime?)null
                        : x.DonThue.NgayKetThucDuKien,
                    x.NgayKetThucMoi,
                    x.LyDo,
                    x.TrangThai,
                    x.NgayTao,
                    x.NguoiDuyetId,
                    NguoiDuyet = x.NguoiDuyet == null
                        ? null
                        : x.NguoiDuyet.HoTen,
                    x.NgayDuyet,
                    Devices = x.DonThue!.ChiTietDonThues.Select(detail => new
                    {
                        detail.MayTinhId,
                        MaTaiSan = detail.MayTinh == null
                            ? null
                            : detail.MayTinh.MaTaiSan,
                        TenMay = detail.MayTinh == null ||
                                 detail.MayTinh.DongMayTinh == null
                            ? null
                            : detail.MayTinh.DongMayTinh.Hang + " " +
                              detail.MayTinh.DongMayTinh.TenDong
                    })
                })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpPatch("{id}/resolve")]
        public async Task<IActionResult> ResolveExtensionRequest(
            int id,
            [FromBody] ResolveExtensionRequestBody request)
        {
            var allowedStatuses = new[] { "da_duyet", "tu_choi" };
            if (!allowedStatuses.Contains(request.Status))
            {
                return BadRequest(new
                {
                    message = "Trạng thái xử lý yêu cầu gia hạn không hợp lệ."
                });
            }

            var extension = await _context.YeuCauGiaHans
                .Include(x => x.DonThue)
                    .ThenInclude(x => x!.ChiTietDonThues)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (extension == null)
            {
                return NotFound(new { message = "Không tìm thấy yêu cầu gia hạn." });
            }

            if (extension.TrangThai != "cho_duyet")
            {
                return Conflict(new
                {
                    message = "Yêu cầu gia hạn này đã được xử lý."
                });
            }

            if (request.NguoiDuyetId.HasValue)
            {
                var approverExists = await _context.NguoiDungs
                    .AnyAsync(x => x.Id == request.NguoiDuyetId.Value);

                if (!approverExists)
                {
                    return BadRequest(new { message = "Người duyệt không tồn tại." });
                }
            }

            var order = extension.DonThue;
            if (order == null)
            {
                return BadRequest(new
                {
                    message = "Yêu cầu không có đơn thuê hợp lệ."
                });
            }

            if (request.Status == "da_duyet")
            {
                if (order.TrangThai != "dang_thue" &&
                    order.TrangThai != "qua_han")
                {
                    return BadRequest(new
                    {
                        message = "Đơn thuê hiện không còn đủ điều kiện gia hạn."
                    });
                }

                if (extension.NgayKetThucMoi.Date <=
                    order.NgayKetThucDuKien.Date)
                {
                    return BadRequest(new
                    {
                        message = "Ngày kết thúc mới phải sau ngày kết thúc hiện tại."
                    });
                }

                var deviceIds = order.ChiTietDonThues
                    .Select(x => x.MayTinhId)
                    .ToList();

                var blockingStatuses = new[]
                {
                    "cho_duyet",
                    "da_duyet",
                    "dang_thue",
                    "yeu_cau_tra",
                    "qua_han"
                };

                var conflictingOrders = await _context.DonThues
                    .Where(x =>
                        x.Id != order.Id &&
                        blockingStatuses.Contains(x.TrangThai) &&
                        x.NgayBatDau.Date <= extension.NgayKetThucMoi.Date &&
                        x.NgayKetThucDuKien.Date >
                            order.NgayKetThucDuKien.Date &&
                        x.ChiTietDonThues.Any(detail =>
                            deviceIds.Contains(detail.MayTinhId)))
                    .Select(x => x.MaDonThue)
                    .ToListAsync();

                if (conflictingOrders.Count > 0)
                {
                    return Conflict(new
                    {
                        message = "Thiết bị đã được xếp cho đơn thuê khác trong thời gian gia hạn.",
                        conflictingOrders
                    });
                }

                order.NgayKetThucDuKien = extension.NgayKetThucMoi.Date;
                if (order.TrangThai == "qua_han")
                {
                    order.TrangThai = "dang_thue";
                }
            }

            extension.TrangThai = request.Status;
            extension.NguoiDuyetId = request.NguoiDuyetId;
            extension.NgayDuyet = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = request.Status == "da_duyet"
                    ? "Đã duyệt yêu cầu gia hạn."
                    : "Đã từ chối yêu cầu gia hạn.",
                ngayKetThucMoi = extension.NgayKetThucMoi
            });
        }
    }
}
