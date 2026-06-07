using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;

namespace Nhom6_QLThietBi_API.Controllers
{
    [ApiController]
    [Route("api/admin/maintenances")]
    public class AdminMaintenancesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public class UpdateMaintenanceStatusRequest
        {
            public string Status { get; set; } = string.Empty;
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