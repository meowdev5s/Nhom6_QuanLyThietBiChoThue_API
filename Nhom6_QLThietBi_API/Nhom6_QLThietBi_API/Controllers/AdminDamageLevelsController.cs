using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;

namespace Nhom6_QLThietBi_API.Controllers
{
    [ApiController]
    [Route("api/admin/damage-levels")]
    public class AdminDamageLevelsController : ControllerBase
    {
        public class SaveDamageLevelRequest
        {
            public string TenMucDo { get; set; } = string.Empty;
            public string? MoTa { get; set; }
            public decimal PhanTramDenBu { get; set; }
        }

        private readonly AppDbContext _context;

        public AdminDamageLevelsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDamageLevels()
        {
            var levels = await _context.MucDoHuHongs
                .OrderBy(x => x.PhanTramDenBu)
                .Select(x => new
                {
                    x.Id,
                    x.TenMucDo,
                    x.MoTa,
                    x.PhanTramDenBu
                })
                .ToListAsync();

            return Ok(levels);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDamageLevel(
            [FromBody] SaveDamageLevelRequest request)
        {
            var validationResult = ValidateRequest(request);
            if (validationResult != null)
            {
                return validationResult;
            }

            var normalizedName = request.TenMucDo.Trim();
            var duplicate = await _context.MucDoHuHongs
                .AnyAsync(x => x.TenMucDo.ToLower() == normalizedName.ToLower());

            if (duplicate)
            {
                return Conflict(new
                {
                    message = "Tên mức độ hư hỏng đã tồn tại."
                });
            }

            var level = new MucDoHuHong
            {
                TenMucDo = normalizedName,
                MoTa = NormalizeDescription(request.MoTa),
                PhanTramDenBu = request.PhanTramDenBu
            };

            _context.MucDoHuHongs.Add(level);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetDamageLevels),
                new { id = level.Id },
                new
                {
                    message = "Thêm mức độ hư hỏng thành công.",
                    id = level.Id
                });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDamageLevel(
            int id,
            [FromBody] SaveDamageLevelRequest request)
        {
            var validationResult = ValidateRequest(request);
            if (validationResult != null)
            {
                return validationResult;
            }

            var level = await _context.MucDoHuHongs.FindAsync(id);
            if (level == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy mức độ hư hỏng."
                });
            }

            var normalizedName = request.TenMucDo.Trim();
            var duplicate = await _context.MucDoHuHongs.AnyAsync(
                x => x.Id != id &&
                     x.TenMucDo.ToLower() == normalizedName.ToLower());

            if (duplicate)
            {
                return Conflict(new
                {
                    message = "Tên mức độ hư hỏng đã tồn tại."
                });
            }

            level.TenMucDo = normalizedName;
            level.MoTa = NormalizeDescription(request.MoTa);
            level.PhanTramDenBu = request.PhanTramDenBu;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật mức độ hư hỏng thành công."
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDamageLevel(int id)
        {
            var level = await _context.MucDoHuHongs.FindAsync(id);
            if (level == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy mức độ hư hỏng."
                });
            }

            var isInUse = await _context.BaoCaoHuHongs
                .AnyAsync(x => x.MucDoHuHongId == id);

            if (isInUse)
            {
                return Conflict(new
                {
                    message = "Mức độ này đang được sử dụng trong báo cáo hư hỏng nên không thể xóa."
                });
            }

            _context.MucDoHuHongs.Remove(level);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Xóa mức độ hư hỏng thành công."
            });
        }

        private BadRequestObjectResult? ValidateRequest(
            SaveDamageLevelRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TenMucDo))
            {
                return BadRequest(new
                {
                    message = "Tên mức độ hư hỏng không được để trống."
                });
            }

            if (request.TenMucDo.Trim().Length > 100)
            {
                return BadRequest(new
                {
                    message = "Tên mức độ hư hỏng không được quá 100 ký tự."
                });
            }

            if (request.PhanTramDenBu < 0 || request.PhanTramDenBu > 100)
            {
                return BadRequest(new
                {
                    message = "Tỷ lệ đền bù phải từ 0 đến 100."
                });
            }

            return null;
        }

        private static string? NormalizeDescription(string? description)
        {
            var normalized = description?.Trim();
            return string.IsNullOrEmpty(normalized) ? null : normalized;
        }
    }
}
