using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;

namespace Nhom6_QLThietBi_API.Controllers
{
    [Authorize(Roles = "admin")]
    [ApiController]
    [Route("api/admin/organizations")]
    public class AdminOrganizationsController : ControllerBase
    {
        public class SaveOrganizationRequest
        {
            public string TenDonVi { get; set; } = string.Empty;
            public string? DiaChi { get; set; }
            public string? MaSoThue { get; set; }
            public string? NguoiDaiDien { get; set; }
            public string? Email { get; set; }
            public string? SoDienThoai { get; set; }
            public string TrangThai { get; set; } = "hoat_dong";
        }

        private readonly AppDbContext _context;

        public AdminOrganizationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrganizations()
        {
            var organizations = await _context.DonVis
                .OrderBy(x => x.TenDonVi)
                .Select(x => new
                {
                    x.Id,
                    x.TenDonVi,
                    x.DiaChi,
                    x.MaSoThue,
                    x.NguoiDaiDien,
                    x.Email,
                    x.SoDienThoai,
                    x.TrangThai,
                    x.NgayTao,
                    SoNguoiDung = x.NguoiDungs.Count,
                    SoDonThue = x.DonThues.Count
                })
                .ToListAsync();

            return Ok(organizations);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrganization(
            [FromBody] SaveOrganizationRequest request)
        {
            var validation = Validate(request);
            if (validation != null) return validation;

            var duplicate = await FindDuplicate(request, null);
            if (duplicate != null) return duplicate;

            var organization = new DonVi
            {
                TenDonVi = request.TenDonVi.Trim(),
                DiaChi = Normalize(request.DiaChi),
                MaSoThue = Normalize(request.MaSoThue),
                NguoiDaiDien = Normalize(request.NguoiDaiDien),
                Email = Normalize(request.Email),
                SoDienThoai = Normalize(request.SoDienThoai),
                TrangThai = request.TrangThai.Trim(),
                NgayTao = DateTime.Now
            };

            _context.DonVis.Add(organization);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Thêm đơn vị thành công.", id = organization.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrganization(
            int id,
            [FromBody] SaveOrganizationRequest request)
        {
            var validation = Validate(request);
            if (validation != null) return validation;

            var organization = await _context.DonVis.FindAsync(id);
            if (organization == null)
                return NotFound(new { message = "Không tìm thấy đơn vị." });

            var duplicate = await FindDuplicate(request, id);
            if (duplicate != null) return duplicate;

            organization.TenDonVi = request.TenDonVi.Trim();
            organization.DiaChi = Normalize(request.DiaChi);
            organization.MaSoThue = Normalize(request.MaSoThue);
            organization.NguoiDaiDien = Normalize(request.NguoiDaiDien);
            organization.Email = Normalize(request.Email);
            organization.SoDienThoai = Normalize(request.SoDienThoai);
            organization.TrangThai = request.TrangThai.Trim();

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật đơn vị thành công." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrganization(int id)
        {
            var organization = await _context.DonVis
                .Include(x => x.NguoiDungs)
                .Include(x => x.DonThues)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (organization == null)
                return NotFound(new { message = "Không tìm thấy đơn vị." });

            var hasChats = await _context.CuocTroChuyens
                .AnyAsync(x => x.DonViId == id);
            if (organization.NguoiDungs.Count > 0 ||
                organization.DonThues.Count > 0 || hasChats)
            {
                return Conflict(new
                {
                    message = "Đơn vị đã phát sinh dữ liệu nên không thể xóa. Hãy chuyển sang trạng thái ngừng hoạt động."
                });
            }

            _context.DonVis.Remove(organization);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa đơn vị thành công." });
        }

        private BadRequestObjectResult? Validate(SaveOrganizationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TenDonVi))
                return BadRequest(new { message = "Tên đơn vị không được để trống." });

            var allowedStatuses = new[] { "hoat_dong", "tam_khoa" };
            if (!allowedStatuses.Contains(request.TrangThai?.Trim()))
                return BadRequest(new { message = "Trạng thái đơn vị không hợp lệ." });

            if (!string.IsNullOrWhiteSpace(request.Email) &&
                !request.Email.Contains('@'))
                return BadRequest(new { message = "Email không đúng định dạng." });

            return null;
        }

        private async Task<ConflictObjectResult?> FindDuplicate(
            SaveOrganizationRequest request,
            int? ignoredId)
        {
            var name = request.TenDonVi.Trim().ToLower();
            var taxCode = Normalize(request.MaSoThue)?.ToLower();

            var duplicateName = await _context.DonVis.AnyAsync(x =>
                (!ignoredId.HasValue || x.Id != ignoredId.Value) &&
                x.TenDonVi.ToLower() == name);
            if (duplicateName)
                return Conflict(new { message = "Tên đơn vị đã tồn tại." });

            if (taxCode != null)
            {
                var duplicateTaxCode = await _context.DonVis.AnyAsync(x =>
                    (!ignoredId.HasValue || x.Id != ignoredId.Value) &&
                    x.MaSoThue != null &&
                    x.MaSoThue.ToLower() == taxCode);
                if (duplicateTaxCode)
                    return Conflict(new { message = "Mã số thuế đã tồn tại." });
            }

            return null;
        }

        private static string? Normalize(string? value)
        {
            var text = value?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
    }
}
