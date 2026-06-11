using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;

namespace Nhom6_QLThietBi_API.Controllers
{
    [ApiController]
    [Route("api/admin/devices")]
    public class AdminDevicesController : ControllerBase
    {
        public class SaveDeviceRequest
        {
            public int DongMayTinhId { get; set; }
            public string MaTaiSan { get; set; } = string.Empty;
            public string? SerialNumber { get; set; }
            public string LoaiMay { get; set; } = string.Empty;
            public string? Cpu { get; set; }
            public string? Ram { get; set; }
            public string? OCung { get; set; }
            public string? Gpu { get; set; }
            public string? ManHinh { get; set; }
            public string? HeDieuHanh { get; set; }
            public decimal GiaTriMay { get; set; }
            public decimal GiaThueNgay { get; set; }
            public decimal TiLeDatCoc { get; set; }
            public string TinhTrang { get; set; } = "san_sang";
            public DateTime? NgayNhap { get; set; }
            public string? GhiChu { get; set; }
        }

        private static readonly string[] AllowedStatuses =
        {
            "san_sang", "dang_thue", "bao_tri", "hong", "ngung_kinh_doanh"
        };

        private readonly AppDbContext _context;

        public AdminDevicesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDevices()
        {
            var devices = await _context.MayTinhs
                .Include(m => m.DongMayTinh)
                .OrderBy(m => m.Id)
                .Select(m => new
                {
                    m.Id,
                    m.DongMayTinhId,
                    m.MaTaiSan,
                    m.SerialNumber,
                    TenDong = m.DongMayTinh == null ? null : m.DongMayTinh.TenDong,
                    Hang = m.DongMayTinh == null ? null : m.DongMayTinh.Hang,
                    m.LoaiMay,
                    m.Cpu,
                    m.Ram,
                    OCung = m.OCung,
                    m.Gpu,
                    m.ManHinh,
                    m.HeDieuHanh,
                    m.GiaTriMay,
                    m.GiaThueNgay,
                    m.TiLeDatCoc,
                    m.TinhTrang,
                    m.NgayNhap,
                    m.GhiChu
                })
                .ToListAsync();

            return Ok(devices);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDevice([FromBody] SaveDeviceRequest request)
        {
            var validation = await Validate(request, null);
            if (validation != null) return validation;

            var device = new MayTinh();
            Apply(device, request);
            device.NgayTao = DateTime.Now;

            _context.MayTinhs.Add(device);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Thêm thiết bị thành công.", id = device.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDevice(
            int id,
            [FromBody] SaveDeviceRequest request)
        {
            var device = await _context.MayTinhs.FindAsync(id);
            if (device == null)
                return NotFound(new { message = "Không tìm thấy thiết bị." });

            var validation = await Validate(request, id);
            if (validation != null) return validation;

            Apply(device, request);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thiết bị thành công." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDevice(int id)
        {
            var device = await _context.MayTinhs.FindAsync(id);
            if (device == null)
                return NotFound(new { message = "Không tìm thấy thiết bị." });

            var hasRentalHistory = await _context.ChiTietDonThues
                .AnyAsync(x => x.MayTinhId == id);
            var hasMaintenanceHistory = await _context.BaoTriMayTinhs
                .AnyAsync(x => x.MayTinhId == id);
            var hasImages = await _context.AnhMayTinhs
                .AnyAsync(x => x.MayTinhId == id);

            if (hasRentalHistory || hasMaintenanceHistory || hasImages)
            {
                return Conflict(new
                {
                    message = "Thiết bị đã phát sinh dữ liệu nên không thể xóa. Hãy chuyển sang trạng thái ngừng kinh doanh."
                });
            }

            _context.MayTinhs.Remove(device);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa thiết bị thành công." });
        }

        private async Task<IActionResult?> Validate(SaveDeviceRequest request, int? ignoredId)
        {
            if (request.DongMayTinhId <= 0 ||
                string.IsNullOrWhiteSpace(request.MaTaiSan) ||
                string.IsNullOrWhiteSpace(request.LoaiMay))
            {
                return BadRequest(new
                {
                    message = "Dòng máy, mã tài sản và loại máy không được để trống."
                });
            }

            if (request.GiaTriMay < 0 || request.GiaThueNgay < 0 ||
                request.TiLeDatCoc < 0 || request.TiLeDatCoc > 100)
            {
                return BadRequest(new
                {
                    message = "Giá trị máy, giá thuê hoặc tỷ lệ đặt cọc không hợp lệ."
                });
            }

            if (!AllowedStatuses.Contains(request.TinhTrang?.Trim()))
                return BadRequest(new { message = "Tình trạng thiết bị không hợp lệ." });

            var lineExists = await _context.DongMayTinhs
                .AnyAsync(x => x.Id == request.DongMayTinhId);
            if (!lineExists)
                return BadRequest(new { message = "Dòng máy không tồn tại." });

            var assetCode = request.MaTaiSan.Trim().ToLower();
            var duplicateAssetCode = await _context.MayTinhs.AnyAsync(x =>
                (!ignoredId.HasValue || x.Id != ignoredId.Value) &&
                x.MaTaiSan.ToLower() == assetCode);
            if (duplicateAssetCode)
                return Conflict(new { message = "Mã tài sản đã tồn tại." });

            var serial = Normalize(request.SerialNumber)?.ToLower();
            if (serial != null)
            {
                var duplicateSerial = await _context.MayTinhs.AnyAsync(x =>
                    (!ignoredId.HasValue || x.Id != ignoredId.Value) &&
                    x.SerialNumber != null && x.SerialNumber.ToLower() == serial);
                if (duplicateSerial)
                    return Conflict(new { message = "Serial thiết bị đã tồn tại." });
            }

            return null;
        }

        private static void Apply(MayTinh device, SaveDeviceRequest request)
        {
            device.DongMayTinhId = request.DongMayTinhId;
            device.MaTaiSan = request.MaTaiSan.Trim();
            device.SerialNumber = Normalize(request.SerialNumber);
            device.LoaiMay = request.LoaiMay.Trim();
            device.Cpu = Normalize(request.Cpu);
            device.Ram = Normalize(request.Ram);
            device.OCung = Normalize(request.OCung);
            device.Gpu = Normalize(request.Gpu);
            device.ManHinh = Normalize(request.ManHinh);
            device.HeDieuHanh = Normalize(request.HeDieuHanh);
            device.GiaTriMay = request.GiaTriMay;
            device.GiaThueNgay = request.GiaThueNgay;
            device.TiLeDatCoc = request.TiLeDatCoc;
            device.TinhTrang = request.TinhTrang.Trim();
            device.NgayNhap = request.NgayNhap?.Date;
            device.GhiChu = Normalize(request.GhiChu);
        }

        private static string? Normalize(string? value)
        {
            var text = value?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
    }
}
