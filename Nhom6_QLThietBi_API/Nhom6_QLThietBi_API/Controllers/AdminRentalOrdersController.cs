using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using System.Linq;

namespace Nhom6_QLThietBi_API.Controllers
{
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
    [FromBody] UpdateRentalOrderStatusRequest request
)
        {
            var allowedStatuses = new[]
            {
        "cho_duyet",
        "da_duyet",
        "dang_thue",
        "yeu_cau_tra",
        "hoan_thanh",
        "qua_han",
        "huy",
        "tu_choi"
    };

            if (!allowedStatuses.Contains(request.Status))
            {
                return BadRequest(new { message = "Trạng thái đơn thuê không hợp lệ." });
            }

            var order = await _context.DonThues
                .Include(d => d.ChiTietDonThues)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (order == null)
            {
                return NotFound(new { message = "Không tìm thấy đơn thuê." });
            }

            var deviceIds = order.ChiTietDonThues
                .Select(ct => ct.MayTinhId)
                .ToList();

            var devices = await _context.MayTinhs
                .Where(m => deviceIds.Contains(m.Id))
                .ToListAsync();

            order.TrangThai = request.Status;

            foreach (var detail in order.ChiTietDonThues)
            {
                if (request.Status == "dang_thue")
                {
                    detail.TrangThai = "dang_thue";
                }
                else if (request.Status == "hoan_thanh")
                {
                    detail.TrangThai = "da_tra";
                }
                else if (request.Status == "huy" || request.Status == "tu_choi")
                {
                    detail.TrangThai = "huy";
                }
            }

            foreach (var device in devices)
            {
                if (request.Status == "dang_thue")
                {
                    device.TinhTrang = "dang_thue";
                }
                else if (
                    request.Status == "hoan_thanh" ||
                    request.Status == "huy" ||
                    request.Status == "tu_choi"
                )
                {
                    device.TinhTrang = "san_sang";
                }
            }

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