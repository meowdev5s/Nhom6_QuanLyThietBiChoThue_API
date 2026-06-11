using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;

namespace Nhom6_QLThietBi_API.Controllers
{
    [ApiController]
    [Route("api/user/profile")]
    public class UserProfileController : ControllerBase
    {
        public class UpdateProfileRequest
        {
            public string HoTen { get; set; } = string.Empty;
            public string TenDangNhap { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string? SoDienThoai { get; set; }
        }

        private readonly AppDbContext _context;

        public UserProfileController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetProfile(int userId)
        {
            var user = await _context.NguoiDungs
                .AsNoTracking()
                .Include(x => x.DonVi)
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
                return NotFound(new { message = "Không tìm thấy tài khoản." });

            return Ok(ToResponse(user));
        }

        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateProfile(
            int userId,
            [FromBody] UpdateProfileRequest request)
        {
            var fullName = request.HoTen.Trim();
            var username = request.TenDangNhap.Trim();
            var email = Normalize(request.Email);
            var phone = Normalize(request.SoDienThoai);

            if (string.IsNullOrEmpty(fullName))
                return BadRequest(new { message = "Họ tên không được để trống." });
            if (string.IsNullOrEmpty(username))
                return BadRequest(new { message = "Tên đăng nhập không được để trống." });
            if (email != null && !email.Contains('@'))
                return BadRequest(new { message = "Email không đúng định dạng." });

            var user = await _context.NguoiDungs
                .Include(x => x.DonVi)
                .FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy tài khoản." });

            var normalizedUsername = username.ToLower();
            if (await _context.NguoiDungs.AnyAsync(x =>
                    x.Id != userId &&
                    x.TenDangNhap.ToLower() == normalizedUsername))
            {
                return Conflict(new { message = "Tên đăng nhập đã được sử dụng." });
            }

            if (email != null)
            {
                var normalizedEmail = email.ToLower();
                if (await _context.NguoiDungs.AnyAsync(x =>
                        x.Id != userId &&
                        x.Email != null &&
                        x.Email.ToLower() == normalizedEmail))
                {
                    return Conflict(new { message = "Email đã được sử dụng." });
                }
            }

            user.HoTen = fullName;
            user.TenDangNhap = username;
            user.Email = email;
            user.SoDienThoai = phone;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật thông tin cá nhân thành công.",
                profile = ToResponse(user)
            });
        }

        private static object ToResponse(NguoiDung user)
        {
            return new
            {
                user.Id,
                user.DonViId,
                user.HoTen,
                user.TenDangNhap,
                user.Email,
                user.SoDienThoai,
                user.VaiTro,
                user.TrangThai,
                user.NgayTao,
                TenDonVi = user.DonVi == null ? null : user.DonVi.TenDonVi
            };
        }

        private static string? Normalize(string? value)
        {
            var text = value?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
    }
}
