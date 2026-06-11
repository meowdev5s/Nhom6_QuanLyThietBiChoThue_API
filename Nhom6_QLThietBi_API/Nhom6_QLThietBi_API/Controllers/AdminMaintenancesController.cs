using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;

namespace Nhom6_QLThietBi_API.Controllers
{
    [Authorize(Roles = "admin,nhan_vien")]
    [ApiController]
    [Route("api/admin/maintenances")]
    public class AdminMaintenancesController : ControllerBase
    {
        public class SaveMaintenanceRequest
        {
            public int MayTinhId { get; set; }
            public DateTime NgayBatDau { get; set; }
            public string NoiDung { get; set; } = string.Empty;
            public decimal ChiPhi { get; set; }
        }

        public class UpdateMaintenanceStatusRequest
        {
            public string Status { get; set; } = string.Empty;
        }

        private readonly AppDbContext _context;

        public AdminMaintenancesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMaintenances()
        {
            var records = await MaintenanceQuery()
                .OrderByDescending(b => b.NgayBatDau)
                .ToListAsync();

            return Ok(records.Select(ToResponse));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMaintenance(int id)
        {
            var maintenance = await MaintenanceQuery()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (maintenance == null)
                return NotFound(new { message = "Không tìm thấy phiếu bảo trì." });

            return Ok(ToResponse(maintenance));
        }

        [HttpPost]
        public async Task<IActionResult> CreateMaintenance([FromBody] SaveMaintenanceRequest request)
        {
            var validation = ValidateRequest(request);
            if (validation != null) return validation;

            var device = await _context.MayTinhs.FindAsync(request.MayTinhId);
            if (device == null)
                return NotFound(new { message = "Không tìm thấy thiết bị." });

            if (device.TinhTrang == "dang_thue" || device.TinhTrang == "bao_tri")
                return BadRequest(new { message = "Thiết bị đang được sử dụng hoặc bảo trì." });

            var maintenance = new BaoTriMayTinh
            {
                MayTinhId = request.MayTinhId,
                NgayBatDau = request.NgayBatDau.Date,
                NoiDung = request.NoiDung.Trim(),
                ChiPhi = request.ChiPhi,
                TrangThai = "dang_bao_tri"
            };

            device.TinhTrang = "bao_tri";
            _context.BaoTriMayTinhs.Add(maintenance);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMaintenance), new { id = maintenance.Id },
                new { message = "Tạo phiếu bảo trì thành công.", id = maintenance.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMaintenance(
            int id,
            [FromBody] SaveMaintenanceRequest request)
        {
            var validation = ValidateRequest(request);
            if (validation != null) return validation;

            var maintenance = await _context.BaoTriMayTinhs
                .Include(b => b.MayTinh)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (maintenance == null)
                return NotFound(new { message = "Không tìm thấy phiếu bảo trì." });

            if (maintenance.TrangThai != "dang_bao_tri")
                return Conflict(new { message = "Chỉ được sửa phiếu đang bảo trì." });

            if (maintenance.MayTinhId != request.MayTinhId)
            {
                var newDevice = await _context.MayTinhs.FindAsync(request.MayTinhId);
                if (newDevice == null)
                    return NotFound(new { message = "Không tìm thấy thiết bị mới." });

                if (newDevice.TinhTrang == "dang_thue" || newDevice.TinhTrang == "bao_tri")
                    return BadRequest(new { message = "Thiết bị mới đang được sử dụng hoặc bảo trì." });

                if (maintenance.MayTinh != null && maintenance.MayTinh.TinhTrang == "bao_tri")
                    maintenance.MayTinh.TinhTrang = "san_sang";

                newDevice.TinhTrang = "bao_tri";
                maintenance.MayTinhId = newDevice.Id;
            }

            maintenance.NgayBatDau = request.NgayBatDau.Date;
            maintenance.NoiDung = request.NoiDung.Trim();
            maintenance.ChiPhi = request.ChiPhi;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật phiếu bảo trì thành công.", id });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdateMaintenanceStatusRequest request)
        {
            var status = request.Status.Trim().ToLowerInvariant();
            if (status != "hoan_thanh" && status != "huy")
                return BadRequest(new { message = "Trạng thái bảo trì không hợp lệ." });

            var maintenance = await _context.BaoTriMayTinhs
                .Include(b => b.MayTinh)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (maintenance == null)
                return NotFound(new { message = "Không tìm thấy phiếu bảo trì." });

            if (maintenance.TrangThai != "dang_bao_tri")
                return Conflict(new { message = "Phiếu bảo trì đã được chốt." });

            maintenance.TrangThai = status;
            maintenance.NgayKetThuc = DateTime.Today;

            if (maintenance.MayTinh != null && maintenance.MayTinh.TinhTrang == "bao_tri")
                maintenance.MayTinh.TinhTrang = "san_sang";

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật trạng thái bảo trì thành công.", id, status });
        }

        private IActionResult? ValidateRequest(SaveMaintenanceRequest request)
        {
            if (request.MayTinhId <= 0)
                return BadRequest(new { message = "Thiết bị không hợp lệ." });
            if (request.NgayBatDau == default)
                return BadRequest(new { message = "Ngày bắt đầu không hợp lệ." });
            if (string.IsNullOrWhiteSpace(request.NoiDung))
                return BadRequest(new { message = "Nội dung bảo trì không được để trống." });
            if (request.ChiPhi < 0)
                return BadRequest(new { message = "Chi phí bảo trì không hợp lệ." });
            return null;
        }

        private IQueryable<BaoTriMayTinh> MaintenanceQuery()
        {
            return _context.BaoTriMayTinhs
                .Include(b => b.MayTinh)
                .ThenInclude(m => m!.DongMayTinh);
        }

        private static object ToResponse(BaoTriMayTinh b)
        {
            return new
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
            };
        }
    }
}
