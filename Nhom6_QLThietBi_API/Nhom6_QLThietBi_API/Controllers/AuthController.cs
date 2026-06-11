using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;

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

        public class RegistrationOrganizationRequest
        {
            public string TenDonVi { get; set; } = string.Empty;
            public string? DiaChi { get; set; }
            public string? MaSoThue { get; set; }
            public string? NguoiDaiDien { get; set; }
            public string? Email { get; set; }
            public string? SoDienThoai { get; set; }
        }

        public class RegisterRequest
        {
            public int? DonViId { get; set; }
            public RegistrationOrganizationRequest? DonViMoi { get; set; }
            public string HoTen { get; set; } = string.Empty;
            public string TenDangNhap { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? SoDienThoai { get; set; }
            public string MatKhau { get; set; } = string.Empty;
        }

        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("organizations")]
        public async Task<IActionResult> GetRegistrationOrganizations()
        {
            var organizations = await _context.DonVis
                .AsNoTracking()
                .Where(x => x.TrangThai == "hoat_dong")
                .OrderBy(x => x.TenDonVi)
                .Select(x => new { x.Id, x.TenDonVi, x.DiaChi, x.MaSoThue })
                .ToListAsync();
            return Ok(organizations);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var fullName = request.HoTen?.Trim();
            var username = request.TenDangNhap?.Trim();
            var email = request.Email?.Trim();
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) ||
                string.IsNullOrEmpty(email))
                return BadRequest(new { message = "Vui lòng nhập đầy đủ họ tên, tên đăng nhập và email." });
            if (!email.Contains('@'))
                return BadRequest(new { message = "Email không đúng định dạng." });
            if (!IsValidPassword(request.MatKhau))
                return BadRequest(new { message = "Mật khẩu phải có ít nhất 6 ký tự." });
            if (request.DonViId.HasValue == (request.DonViMoi != null))
                return BadRequest(new { message = "Hãy chọn một đơn vị hiện có hoặc nhập đơn vị mới." });

            var normalizedUsername = username.ToLowerInvariant();
            var normalizedEmail = email.ToLowerInvariant();
            if (await _context.NguoiDungs.AnyAsync(x =>
                    x.TenDangNhap.ToLower() == normalizedUsername))
                return Conflict(new { message = "Tên đăng nhập đã tồn tại." });
            if (await _context.NguoiDungs.AnyAsync(x =>
                    x.Email != null && x.Email.ToLower() == normalizedEmail))
                return Conflict(new { message = "Email đã được sử dụng." });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            DonVi organization;
            if (request.DonViId.HasValue)
            {
                organization = await _context.DonVis.FirstOrDefaultAsync(x =>
                    x.Id == request.DonViId.Value && x.TrangThai == "hoat_dong");
                if (organization == null)
                    return BadRequest(new { message = "Đơn vị đã chọn không tồn tại hoặc đã tạm khóa." });
            }
            else
            {
                var newOrganization = request.DonViMoi!;
                var organizationName = newOrganization.TenDonVi?.Trim();
                var taxCode = Normalize(newOrganization.MaSoThue);
                if (string.IsNullOrEmpty(organizationName))
                    return BadRequest(new { message = "Tên đơn vị không được để trống." });
                if (!string.IsNullOrWhiteSpace(newOrganization.Email) &&
                    !newOrganization.Email.Contains('@'))
                    return BadRequest(new { message = "Email đơn vị không đúng định dạng." });
                if (await _context.DonVis.AnyAsync(x =>
                        x.TenDonVi.ToLower() == organizationName.ToLower()))
                    return Conflict(new { message = "Tên đơn vị đã tồn tại. Hãy chọn đơn vị đó trong danh sách." });
                if (taxCode != null && await _context.DonVis.AnyAsync(x =>
                        x.MaSoThue != null && x.MaSoThue.ToLower() == taxCode.ToLower()))
                    return Conflict(new { message = "Mã số thuế đã thuộc một đơn vị khác." });

                organization = new DonVi
                {
                    TenDonVi = organizationName,
                    DiaChi = Normalize(newOrganization.DiaChi),
                    MaSoThue = taxCode,
                    NguoiDaiDien = Normalize(newOrganization.NguoiDaiDien) ?? fullName,
                    Email = Normalize(newOrganization.Email),
                    SoDienThoai = Normalize(newOrganization.SoDienThoai),
                    TrangThai = "hoat_dong",
                    NgayTao = DateTime.Now
                };
                _context.DonVis.Add(organization);
                await _context.SaveChangesAsync();
            }

            var user = new NguoiDung
            {
                DonViId = organization.Id,
                HoTen = fullName,
                TenDangNhap = username,
                Email = email,
                SoDienThoai = Normalize(request.SoDienThoai),
                MatKhauHash = HashPassword(request.MatKhau),
                VaiTro = "khach_hang",
                TrangThai = "hoat_dong",
                NgayTao = DateTime.Now
            };
            _context.NguoiDungs.Add(user);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                userId = user.Id,
                donViId = user.DonViId,
                user.HoTen,
                user.TenDangNhap,
                user.VaiTro,
                token = CreateToken(user.Id, user.TenDangNhap, user.VaiTro)
            });
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
                donViId = user.DonViId,
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

            return storedHash == password + "_demo_hash";
        }

        private static string? Normalize(string? value)
        {
            var text = value?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
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
