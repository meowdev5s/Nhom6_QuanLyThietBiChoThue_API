using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;

namespace Nhom6_QLThietBi_API.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    public class AdminUsersController : ControllerBase
    {
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
    }
}