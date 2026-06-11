using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nhom6_QLThietBi_API.Data;

namespace Nhom6_QLThietBi_API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        public class LoginRequest
        {
            public string Account { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class ResetPasswordRequest
        {
            public string Account { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }

        public class ChangePasswordRequest
        {
            public string CurrentPassword { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }

        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var account = request.Account?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrEmpty(request.Password))
                return BadRequest(new { message = "Vui lòng nhập tài khoản và mật khẩu." });

            var user = await _context.NguoiDungs.FirstOrDefaultAsync(x =>
                x.TenDangNhap.ToLower() == account ||
                (x.Email != null && x.Email.ToLower() == account));
            if (user == null || !VerifyPassword(request.Password, user.MatKhauHash))
                return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không đúng." });
            if (user.TrangThai != "hoat_dong")
                return StatusCode(403, new { message = "Tài khoản đang bị khóa." });

            var token = CreateToken(user.Id, user.TenDangNhap, user.VaiTro);
            return Ok(new
            {
                userId = user.Id,
                user.HoTen,
                user.TenDangNhap,
                user.VaiTro,
                token
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var account = request.Account.Trim().ToLowerInvariant();
            var email = request.Email.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(email))
                return BadRequest(new { message = "Vui lòng nhập tên đăng nhập và email." });
            if (!IsValidPassword(request.NewPassword))
                return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 6 ký tự." });

            var user = await _context.NguoiDungs.FirstOrDefaultAsync(x =>
                x.TenDangNhap.ToLower() == account &&
                x.Email != null && x.Email.ToLower() == email);
            if (user == null)
                return BadRequest(new { message = "Tên đăng nhập và email không khớp." });
            if (user.TrangThai != "hoat_dong")
                return StatusCode(403, new { message = "Tài khoản đang bị khóa." });

            user.MatKhauHash = HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đặt lại mật khẩu thành công." });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var idText = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idText, out var userId)) return Unauthorized();
            if (!IsValidPassword(request.NewPassword))
                return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 6 ký tự." });
            if (request.CurrentPassword == request.NewPassword)
                return BadRequest(new { message = "Mật khẩu mới phải khác mật khẩu hiện tại." });

            var user = await _context.NguoiDungs.FindAsync(userId);
            if (user == null || !VerifyPassword(request.CurrentPassword, user.MatKhauHash))
                return BadRequest(new { message = "Mật khẩu hiện tại không đúng." });

            user.MatKhauHash = HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đổi mật khẩu thành công." });
        }

        private string CreateToken(int userId, string username, string role)
        {
            var key = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Key.");
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };
            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            if (storedHash.StartsWith("PBKDF2-SHA256$"))
            {
                var parts = storedHash.Split('$');
                if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations))
                    return false;
                try
                {
                    var salt = Convert.FromBase64String(parts[2]);
                    var expected = Convert.FromBase64String(parts[3]);
                    var actual = Rfc2898DeriveBytes.Pbkdf2(
                        password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                    return CryptographicOperations.FixedTimeEquals(actual, expected);
                }
                catch (FormatException)
                {
                    return false;
                }
            }

            // Hỗ trợ dữ liệu minh họa cũ trong script SQL.
            return storedHash == password + "_demo_hash";
        }

        private static bool IsValidPassword(string password) =>
            !string.IsNullOrWhiteSpace(password) && password.Length >= 6;

        private static string HashPassword(string password)
        {
            const int iterations = 100000;
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, iterations, HashAlgorithmName.SHA256, 32);
            return $"PBKDF2-SHA256${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }
    }
}
