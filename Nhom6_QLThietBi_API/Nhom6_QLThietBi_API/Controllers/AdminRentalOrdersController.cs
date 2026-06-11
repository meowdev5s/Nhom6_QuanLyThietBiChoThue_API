using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Services;

namespace Nhom6_QLThietBi_API.Controllers
{
    [Authorize(Roles = "admin,nhan_vien")]
    [ApiController]
    [Route("api/admin/rental-orders")]
    public class AdminRentalOrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public class UpdateRentalOrderStatusRequest
        {
            public string Status { get; set; } = string.Empty;
        }

        public AdminRentalOrdersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdateRentalOrderStatusRequest request)
        {
            var nextStatus = (request.Status ?? string.Empty).Trim().ToLowerInvariant();
            var order = await _context.DonThues
                .Include(x => x.ChiTietDonThues)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (order == null)
                return NotFound(new { message = "Không tìm thấy đơn thuê." });

            var allowedTransitions = order.TrangThai switch
            {
                "cho_duyet" => new[] { "da_duyet", "tu_choi" },
                "da_duyet" => new[] { "dang_thue", "huy" },
                _ => Array.Empty<string>()
            };

            if (!allowedTransitions.Contains(nextStatus))
            {
                return Conflict(new
                {
                    message = "Không thể chuyển đơn sang trạng thái này. Đơn đang thuê phải đi qua quy trình trả máy và thanh toán."
                });
            }

            if (order.ChiTietDonThues.Count == 0)
                return BadRequest(new { message = "Đơn thuê chưa có thiết bị." });

            if (nextStatus == "dang_thue")
            {
                var hasActiveContract = await _context.HopDongs.AnyAsync(x =>
                    x.DonThueId == order.Id && x.TrangThai == "hieu_luc");
                if (!hasActiveContract)
                {
                    return BadRequest(new
                    {
                        message = "Cần có hợp đồng hiệu lực trước khi bàn giao máy và bắt đầu thuê."
                    });
                }

                var deviceIds = order.ChiTietDonThues
                    .Select(x => x.MayTinhId)
                    .Distinct()
                    .ToList();
                var devices = await _context.MayTinhs
                    .Where(x => deviceIds.Contains(x.Id))
                    .ToListAsync();

                if (devices.Count != deviceIds.Count ||
                    devices.Any(x => x.TinhTrang != "san_sang"))
                {
                    return Conflict(new
                    {
                        message = "Có thiết bị không còn sẵn sàng. Vui lòng kiểm tra lại kho trước khi bắt đầu thuê."
                    });
                }

                foreach (var detail in order.ChiTietDonThues)
                    detail.TrangThai = "dang_thue";
                foreach (var device in devices)
                    device.TinhTrang = "dang_thue";
            }
            else if (nextStatus == "huy" || nextStatus == "tu_choi")
            {
                foreach (var detail in order.ChiTietDonThues)
                    detail.TrangThai = "huy";
            }

            order.TrangThai = nextStatus;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật trạng thái đơn thuê thành công.",
                id = order.Id,
                status = order.TrangThai
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetRentalOrders()
        {
            await BusinessStatusSyncService.SyncAsync(_context);

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
