using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;

namespace Nhom6_QLThietBi_API.Controllers
{
    [Authorize(Roles = "admin")]
    [ApiController]
    [Route("api/admin/users")]
    public class AdminUsersController : ControllerBase
    {
        public class CreateUserRequest
        {
            public int? DonViId { get; set; }
            public string HoTen { get; set; } = string.Empty;
            public string TenDangNhap { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string? SoDienThoai { get; set; }
            public string MatKhau { get; set; } = string.Empty;
            public string VaiTro { get; set; } = "khach_hang";
            public string TrangThai { get; set; } = "hoat_dong";
        }

        public class UpdateUserRequest
        {
            public int? DonViId { get; set; }
            public string HoTen { get; set; } = string.Empty;
            public string TenDangNhap { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string? SoDienThoai { get; set; }
            public string VaiTro { get; set; } = "khach_hang";
            public string TrangThai { get; set; } = "hoat_dong";
        }

        public class UpdateStatusRequest
        {
            public string Status { get; set; } = string.Empty;
        }

        private static readonly string[] AllowedRoles =
        {
            "admin", "nhan_vien", "khach_hang"
        };

        private static readonly string[] AllowedStatuses =
        {
            "hoat_dong", "tam_khoa"
        };

        private readonly AppDbContext _context;

        public AdminUsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.NguoiDungs
                .Include(u => u.DonVi)
                .OrderBy(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.DonViId,
                    u.HoTen,
                    u.TenDangNhap,
                    u.Email,
                    u.SoDienThoai,
                    u.VaiTro,
                    u.TrangThai,
                    u.NgayTao,
                    TenDonVi = u.DonVi == null ? null : u.DonVi.TenDonVi
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var validation = await Validate(
                request.HoTen,
                request.TenDangNhap,
                request.Email,
                request.VaiTro,
                request.TrangThai,
                request.DonViId,
                null);
            if (validation != null) return validation;

            if (string.IsNullOrWhiteSpace(request.MatKhau) || request.MatKhau.Length < 6)
                return BadRequest(new { message = "Mật khẩu tạm phải có ít nhất 6 ký tự." });

            var user = new NguoiDung
            {
                HoTen = request.HoTen.Trim(),
                TenDangNhap = request.TenDangNhap.Trim(),
                Email = Normalize(request.Email),
                SoDienThoai = Normalize(request.SoDienThoai),
                MatKhauHash = HashPassword(request.MatKhau),
                VaiTro = request.VaiTro.Trim(),
                TrangThai = request.TrangThai.Trim(),
                DonViId = request.VaiTro == "khach_hang" ? request.DonViId : null,
                NgayTao = DateTime.Now
            };

            _context.NguoiDungs.Add(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Thêm tài khoản thành công.", id = user.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(
            int id,
            [FromBody] UpdateUserRequest request)
        {
            var user = await _context.NguoiDungs.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy tài khoản." });

            var validation = await Validate(
                request.HoTen,
                request.TenDangNhap,
                request.Email,
                request.VaiTro,
                request.TrangThai,
                request.DonViId,
                id);
            if (validation != null) return validation;

            var accessValidation = await ValidateAdminAccessChange(
                user,
                request.VaiTro.Trim(),
                request.TrangThai.Trim());
            if (accessValidation != null) return accessValidation;

            user.HoTen = request.HoTen.Trim();
            user.TenDangNhap = request.TenDangNhap.Trim();
            user.Email = Normalize(request.Email);
            user.SoDienThoai = Normalize(request.SoDienThoai);
            user.VaiTro = request.VaiTro.Trim();
            user.TrangThai = request.TrangThai.Trim();
            user.DonViId = request.VaiTro == "khach_hang" ? request.DonViId : null;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật tài khoản thành công." });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdateStatusRequest request)
        {
            if (!AllowedStatuses.Contains(request.Status?.Trim()))
                return BadRequest(new { message = "Trạng thái tài khoản không hợp lệ." });

            var user = await _context.NguoiDungs.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy tài khoản." });

            var accessValidation = await ValidateAdminAccessChange(
                user,
                user.VaiTro,
                request.Status.Trim());
            if (accessValidation != null) return accessValidation;

            user.TrangThai = request.Status.Trim();
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật trạng thái tài khoản thành công." });
        }

        private async Task<IActionResult?> Validate(
            string fullName,
            string username,
            string? email,
            string role,
            string status,
            int? organizationId,
            int? ignoredId)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username))
                return BadRequest(new { message = "Họ tên và tên đăng nhập không được để trống." });
            if (!AllowedRoles.Contains(role?.Trim()))
                return BadRequest(new { message = "Vai trò không hợp lệ." });
            if (!AllowedStatuses.Contains(status?.Trim()))
                return BadRequest(new { message = "Trạng thái tài khoản không hợp lệ." });
            if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
                return BadRequest(new { message = "Email không đúng định dạng." });

            if (role == "khach_hang")
            {
                if (!organizationId.HasValue)
                    return BadRequest(new { message = "Khách hàng phải thuộc một đơn vị." });
                var organizationExists = await _context.DonVis.AnyAsync(x =>
                    x.Id == organizationId.Value && x.TrangThai == "hoat_dong");
                if (!organizationExists)
                    return BadRequest(new { message = "Đơn vị không tồn tại hoặc đang bị khóa." });
            }

            var normalizedUsername = username.Trim().ToLower();
            var duplicateUsername = await _context.NguoiDungs.AnyAsync(x =>
                (!ignoredId.HasValue || x.Id != ignoredId.Value) &&
                x.TenDangNhap.ToLower() == normalizedUsername);
            if (duplicateUsername)
                return Conflict(new { message = "Tên đăng nhập đã tồn tại." });

            var normalizedEmail = Normalize(email)?.ToLower();
            if (normalizedEmail != null)
            {
                var duplicateEmail = await _context.NguoiDungs.AnyAsync(x =>
                    (!ignoredId.HasValue || x.Id != ignoredId.Value) &&
                    x.Email != null && x.Email.ToLower() == normalizedEmail);
                if (duplicateEmail)
                    return Conflict(new { message = "Email đã tồn tại." });
            }

            return null;
        }

        private async Task<IActionResult?> ValidateAdminAccessChange(
            NguoiDung target,
            string nextRole,
            string nextStatus)
        {
            var currentUserIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(currentUserIdText, out var currentUserId))
                return Unauthorized(new { message = "Không xác định được tài khoản đang đăng nhập." });

            var removesAdminAccess = target.VaiTro == "admin" &&
                target.TrangThai == "hoat_dong" &&
                (nextRole != "admin" || nextStatus != "hoat_dong");

            if (target.Id == currentUserId && removesAdminAccess)
            {
                return Conflict(new
                {
                    message = "Admin không thể tự đổi vai trò hoặc tự khóa tài khoản đang đăng nhập."
                });
            }

            if (removesAdminAccess)
            {
                var otherActiveAdmins = await _context.NguoiDungs.CountAsync(x =>
                    x.Id != target.Id &&
                    x.VaiTro == "admin" &&
                    x.TrangThai == "hoat_dong");
                if (otherActiveAdmins == 0)
                {
                    return Conflict(new
                    {
                        message = "Hệ thống phải luôn còn ít nhất một Admin đang hoạt động."
                    });
                }
            }

            return null;
        }

        private static string HashPassword(string password)
        {
            const int iterations = 100000;
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                32);
            return $"PBKDF2-SHA256${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        private static string? Normalize(string? value)
        {
            var text = value?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
    }
}
