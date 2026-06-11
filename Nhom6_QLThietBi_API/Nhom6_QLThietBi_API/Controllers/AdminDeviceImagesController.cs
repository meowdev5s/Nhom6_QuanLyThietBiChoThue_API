using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;

namespace Nhom6_QLThietBi_API.Controllers
{
    [Authorize(Roles = "admin,nhan_vien")]
    [ApiController]
    [Route("api/admin/devices/{deviceId}/images")]
    public class AdminDeviceImagesController : ControllerBase
    {
        private static readonly string[] AllowedExtensions =
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminDeviceImagesController(
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> GetImages(int deviceId)
        {
            var deviceExists = await _context.MayTinhs.AnyAsync(x => x.Id == deviceId);
            if (!deviceExists)
                return NotFound(new { message = "Không tìm thấy thiết bị." });

            var images = await _context.AnhMayTinhs
                .Where(x => x.MayTinhId == deviceId)
                .OrderByDescending(x => x.LaAnhDaiDien)
                .ThenBy(x => x.NgayTao)
                .Select(x => new
                {
                    x.Id,
                    x.MayTinhId,
                    x.DuongDanAnh,
                    x.LaAnhDaiDien,
                    x.NgayTao
                })
                .ToListAsync();

            return Ok(images);
        }

        [HttpPost]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(int deviceId, IFormFile file)
        {
            var deviceExists = await _context.MayTinhs.AnyAsync(x => x.Id == deviceId);
            if (!deviceExists)
                return NotFound(new { message = "Không tìm thấy thiết bị." });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Vui lòng chọn tệp ảnh." });
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "Ảnh không được vượt quá 5 MB." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return BadRequest(new { message = "Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP." });

            var uploadDirectory = Path.Combine(
                _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"),
                "uploads",
                "devices",
                deviceId.ToString());
            Directory.CreateDirectory(uploadDirectory);

            var fileName = Guid.NewGuid().ToString("N") + extension;
            var physicalPath = Path.Combine(uploadDirectory, fileName);
            await using (var stream = System.IO.File.Create(physicalPath))
            {
                await file.CopyToAsync(stream);
            }

            var hasImages = await _context.AnhMayTinhs
                .AnyAsync(x => x.MayTinhId == deviceId);
            var image = new AnhMayTinh
            {
                MayTinhId = deviceId,
                DuongDanAnh = $"/uploads/devices/{deviceId}/{fileName}",
                LaAnhDaiDien = !hasImages,
                NgayTao = DateTime.Now
            };
            _context.AnhMayTinhs.Add(image);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Tải ảnh thiết bị thành công.",
                image.Id,
                image.DuongDanAnh,
                image.LaAnhDaiDien
            });
        }

        [HttpPatch("{imageId}/primary")]
        public async Task<IActionResult> SetPrimaryImage(int deviceId, int imageId)
        {
            var image = await _context.AnhMayTinhs.FirstOrDefaultAsync(x =>
                x.Id == imageId && x.MayTinhId == deviceId);
            if (image == null)
                return NotFound(new { message = "Không tìm thấy ảnh thiết bị." });

            var images = await _context.AnhMayTinhs
                .Where(x => x.MayTinhId == deviceId)
                .ToListAsync();
            foreach (var item in images)
                item.LaAnhDaiDien = item.Id == imageId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã đặt ảnh đại diện." });
        }

        [HttpDelete("{imageId}")]
        public async Task<IActionResult> DeleteImage(int deviceId, int imageId)
        {
            var image = await _context.AnhMayTinhs.FirstOrDefaultAsync(x =>
                x.Id == imageId && x.MayTinhId == deviceId);
            if (image == null)
                return NotFound(new { message = "Không tìm thấy ảnh thiết bị." });

            var wasPrimary = image.LaAnhDaiDien;
            _context.AnhMayTinhs.Remove(image);
            await _context.SaveChangesAsync();

            DeletePhysicalFile(image.DuongDanAnh);

            if (wasPrimary)
            {
                var replacement = await _context.AnhMayTinhs
                    .Where(x => x.MayTinhId == deviceId)
                    .OrderBy(x => x.NgayTao)
                    .FirstOrDefaultAsync();
                if (replacement != null)
                {
                    replacement.LaAnhDaiDien = true;
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { message = "Đã xóa ảnh thiết bị." });
        }

        private void DeletePhysicalFile(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;
            var normalized = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var root = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
            var fullRoot = Path.GetFullPath(root);
            if (fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase) &&
                System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}
