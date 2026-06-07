using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;

namespace Nhom6_QLThietBi_API.Controllers
{
    [ApiController]
    [Route("api/admin/maintenances")]
    public class AdminMaintenancesController : ControllerBase
    {

        public class CreateMaintenanceRequest
        {
            public int MayTinhId { get; set; }
            public DateTime NgayBatDau { get; set; }
            public string NoiDung { get; set; } = string.Empty;
            public decimal ChiPhi { get; set; }
        }

        private readonly AppDbContext _context;

        public class UpdateMaintenanceStatusRequest
        {
            public string Status { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> CreateMaintenance(
    [FromBody] CreateMaintenanceRequest request
)
        {
            if (request.MayTinhId <= 0)
            {
                return BadRequest(new
                {
                    message = "Thiết bị không hợp lệ."
                });
            }

            if (string.IsNullOrWhiteSpace(request.NoiDung))
            {
                return BadRequest(new
                {
                    message = "Nội dung bảo trì không được để trống."
                });
            }

            if (request.ChiPhi < 0)
            {
                return BadRequest(new
                {
                    message = "Chi phí bảo trì không hợp lệ."
                });
            }

            var device = await _context.MayTinhs
                .FirstOrDefaultAsync(m => m.Id == request.MayTinhId);

            if (device == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy thiết bị."
                });
            }

            if (device.TinhTrang == "dang_thue")
            {
                return BadRequest(new
                {
                    message = "Thiết bị đang cho thuê, không thể tạo phiếu bảo trì."
                });
            }

            var maintenance = new BaoTriMayTinh
            {
                MayTinhId = request.MayTinhId,
                NgayBatDau = request.NgayBatDau.Date,
                NgayKetThuc = null,
                NoiDung = request.NoiDung.Trim(),
                ChiPhi = request.ChiPhi,
                TrangThai = "dang_bao_tri"
            };

            device.TinhTrang = "bao_tri";

            _context.BaoTriMayTinhs.Add(maintenance);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetMaintenances),
                new { id = maintenance.Id },
                new
                {
                    message = "Tạo phiếu bảo trì thành công.",
                    id = maintenance.Id
                }
            );
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(
    int id,
    [FromBody] UpdateMaintenanceStatusRequest request
)
        {
            var allowedStatuses = new[]
            {
        "dang_bao_tri",
        "hoan_thanh",
        "huy"
    };

            if (!allowedStatuses.Contains(request.Status))
            {
                return BadRequest(new
                {
                    message = "Trạng thái bảo trì không hợp lệ."
                });
            }

            var maintenance = await _context.BaoTriMayTinhs
                .Include(b => b.MayTinh)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (maintenance == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy phiếu bảo trì."
                });
            }

            maintenance.TrangThai = request.Status;

            if (request.Status == "hoan_thanh")
            {
                maintenance.NgayKetThuc = DateTime.Today;

                if (maintenance.MayTinh != null)
                {
                    maintenance.MayTinh.TinhTrang = "san_sang";
                }
            }
            else if (request.Status == "huy")
            {
                if (maintenance.MayTinh != null &&
                    maintenance.MayTinh.TinhTrang == "bao_tri")
                {
                    maintenance.MayTinh.TinhTrang = "san_sang";
                }
            }
            else if (request.Status == "dang_bao_tri")
            {
                maintenance.NgayKetThuc = null;

                if (maintenance.MayTinh != null)
                {
                    maintenance.MayTinh.TinhTrang = "bao_tri";
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật trạng thái bảo trì thành công.",
                id = maintenance.Id,
                status = maintenance.TrangThai
            });
        }

        public AdminMaintenancesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMaintenances()
        {
            var maintenances = await _context.BaoTriMayTinhs
                .Include(b => b.MayTinh)
                    .ThenInclude(m => m!.DongMayTinh)
                .OrderByDescending(b => b.NgayBatDau)
                .Select(b => new
                {
                    b.Id,
                    b.MayTinhId,
                    MaTaiSan = b.MayTinh == null ? null : b.MayTinh.MaTaiSan,
                    TenMay = b.MayTinh == null || b.MayTinh.DongMayTinh == null
                        ? null
                        : b.MayTinh.DongMayTinh.Hang + " " + b.MayTinh.DongMayTinh.TenDong,
                    b.NgayBatDau,
                    b.NgayKetThuc,
                    b.NoiDung,
                    b.ChiPhi,
                    b.TrangThai
                })
                .ToListAsync();

            return Ok(maintenances);
        }
    }
}