using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;

namespace Nhom6_QLThietBi_API.Controllers
{
    [Authorize(Roles = "admin")]
    [ApiController]
    [Route("api/admin/computer-lines")]
    public class AdminComputerLinesController : ControllerBase
    {
        public class SaveComputerLineRequest
        {
            public string TenDong { get; set; } = string.Empty;
            public string Hang { get; set; } = string.Empty;
            public string? MoTa { get; set; }
        }

        private readonly AppDbContext _context;

        public AdminComputerLinesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetComputerLines()
        {
            var lines = await _context.DongMayTinhs
                .Include(x => x.MayTinhs)
                .OrderBy(x => x.Hang)
                .ThenBy(x => x.TenDong)
                .Select(x => new
                {
                    x.Id,
                    x.TenDong,
                    x.Hang,
                    x.MoTa,
                    SoLuongMay = x.MayTinhs.Count
                })
                .ToListAsync();
            return Ok(lines);
        }

        [HttpPost]
        public async Task<IActionResult> CreateComputerLine(
            [FromBody] SaveComputerLineRequest request)
        {
            var error = Validate(request);
            if (error != null) return error;

            var name = request.TenDong.Trim();
            var brand = request.Hang.Trim();
            var exists = await _context.DongMayTinhs.AnyAsync(x =>
                x.TenDong.ToLower() == name.ToLower() &&
                x.Hang.ToLower() == brand.ToLower());
            if (exists)
                return Conflict(new { message = "Dòng máy và hãng này đã tồn tại." });

            var line = new DongMayTinh
            {
                TenDong = name,
                Hang = brand,
                MoTa = Normalize(request.MoTa)
            };
            _context.DongMayTinhs.Add(line);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Thêm dòng máy thành công.", id = line.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComputerLine(
            int id,
            [FromBody] SaveComputerLineRequest request)
        {
            var error = Validate(request);
            if (error != null) return error;

            var line = await _context.DongMayTinhs.FindAsync(id);
            if (line == null)
                return NotFound(new { message = "Không tìm thấy dòng máy." });

            var name = request.TenDong.Trim();
            var brand = request.Hang.Trim();
            var exists = await _context.DongMayTinhs.AnyAsync(x =>
                x.Id != id &&
                x.TenDong.ToLower() == name.ToLower() &&
                x.Hang.ToLower() == brand.ToLower());
            if (exists)
                return Conflict(new { message = "Dòng máy và hãng này đã tồn tại." });

            line.TenDong = name;
            line.Hang = brand;
            line.MoTa = Normalize(request.MoTa);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật dòng máy thành công." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComputerLine(int id)
        {
            var line = await _context.DongMayTinhs
                .Include(x => x.MayTinhs)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (line == null)
                return NotFound(new { message = "Không tìm thấy dòng máy." });
            if (line.MayTinhs.Count > 0)
                return Conflict(new
                {
                    message = "Dòng máy đang có thiết bị nên không thể xóa."
                });

            _context.DongMayTinhs.Remove(line);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa dòng máy thành công." });
        }

        private BadRequestObjectResult? Validate(SaveComputerLineRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TenDong) ||
                string.IsNullOrWhiteSpace(request.Hang))
                return BadRequest(new
                {
                    message = "Tên dòng máy và hãng không được để trống."
                });
            if (request.TenDong.Trim().Length > 100 ||
                request.Hang.Trim().Length > 80)
                return BadRequest(new
                {
                    message = "Tên dòng máy hoặc hãng vượt quá độ dài cho phép."
                });
            return null;
        }

        private static string? Normalize(string? value)
        {
            var text = value?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
    }
}
