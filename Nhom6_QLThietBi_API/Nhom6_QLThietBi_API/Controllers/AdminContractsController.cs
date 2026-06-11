using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;

namespace Nhom6_QLThietBi_API.Controllers
{
    [Authorize(Roles = "admin,nhan_vien")]
    [ApiController]
    [Route("api/admin/contracts")]
    public class AdminContractsController : ControllerBase
    {
        public class CreateContractRequest
        {
            public int DonThueId { get; set; }
            public string MaHopDong { get; set; } = string.Empty;
            public DateTime NgayLap { get; set; }
            public string? NoiDung { get; set; }
            public string? FileUrl { get; set; }
        }

        public class UpdateContractRequest
        {
            public string MaHopDong { get; set; } = string.Empty;
            public DateTime NgayLap { get; set; }
            public string? NoiDung { get; set; }
            public string? FileUrl { get; set; }
        }

        public class UpdateContractStatusRequest
        {
            public string Status { get; set; } = string.Empty;
        }

        private readonly AppDbContext _context;

        public AdminContractsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetContracts()
        {
            var contracts = await ContractQuery()
                .OrderByDescending(x => x.NgayLap)
                .ToListAsync();
            return Ok(contracts.Select(ToResponse));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetContract(int id)
        {
            var contract = await ContractQuery().FirstOrDefaultAsync(x => x.Id == id);
            if (contract == null)
                return NotFound(new { message = "Không tìm thấy hợp đồng." });
            return Ok(ToResponse(contract));
        }

        [HttpPost]
        public async Task<IActionResult> CreateContract([FromBody] CreateContractRequest request)
        {
            var codeError = ValidateCode(request.MaHopDong);
            if (codeError != null) return codeError;
            if (request.DonThueId <= 0)
                return BadRequest(new { message = "Đơn thuê không hợp lệ." });

            var code = request.MaHopDong.Trim();
            var order = await _context.DonThues.FindAsync(request.DonThueId);
            if (order == null)
                return NotFound(new { message = "Không tìm thấy đơn thuê." });

            var orderStatuses = new[] { "da_duyet", "dang_thue", "qua_han" };
            if (!orderStatuses.Contains(order.TrangThai))
                return BadRequest(new { message = "Đơn thuê chưa đủ điều kiện lập hợp đồng." });
            if (await _context.HopDongs.AnyAsync(x => x.DonThueId == request.DonThueId))
                return Conflict(new { message = "Đơn thuê này đã có hợp đồng." });
            if (await CodeExists(code))
                return Conflict(new { message = "Mã hợp đồng đã tồn tại." });

            var contract = new HopDong
            {
                DonThueId = request.DonThueId,
                MaHopDong = code,
                NgayLap = request.NgayLap == default ? DateTime.Today : request.NgayLap.Date,
                NoiDung = Normalize(request.NoiDung),
                FileUrl = Normalize(request.FileUrl),
                TrangThai = "hieu_luc"
            };
            _context.HopDongs.Add(contract);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetContract), new { id = contract.Id },
                new { message = "Tạo hợp đồng thành công.", id = contract.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContract(
            int id,
            [FromBody] UpdateContractRequest request)
        {
            var codeError = ValidateCode(request.MaHopDong);
            if (codeError != null) return codeError;

            var contract = await _context.HopDongs.FindAsync(id);
            if (contract == null)
                return NotFound(new { message = "Không tìm thấy hợp đồng." });
            if (contract.TrangThai != "hieu_luc")
                return Conflict(new { message = "Chỉ được sửa hợp đồng đang hiệu lực." });

            var code = request.MaHopDong.Trim();
            if (await CodeExists(code, id))
                return Conflict(new { message = "Mã hợp đồng đã tồn tại." });

            contract.MaHopDong = code;
            contract.NgayLap = request.NgayLap == default ? contract.NgayLap : request.NgayLap.Date;
            contract.NoiDung = Normalize(request.NoiDung);
            contract.FileUrl = Normalize(request.FileUrl);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật hợp đồng thành công.", id });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdateContractStatusRequest request)
        {
            var status = request.Status.Trim().ToLowerInvariant();
            if (status != "het_hieu_luc" && status != "huy")
                return BadRequest(new { message = "Trạng thái hợp đồng không hợp lệ." });

            var contract = await _context.HopDongs.FindAsync(id);
            if (contract == null)
                return NotFound(new { message = "Không tìm thấy hợp đồng." });
            if (contract.TrangThai != "hieu_luc")
                return Conflict(new { message = "Hợp đồng đã được chốt trạng thái." });

            contract.TrangThai = status;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật hợp đồng thành công.", id, status });
        }

        private IQueryable<HopDong> ContractQuery()
        {
            return _context.HopDongs
                .AsNoTracking()
                .Include(x => x.DonThue)!.ThenInclude(x => x!.DonVi)
                .Include(x => x.DonThue)!.ThenInclude(x => x!.ChiTietDonThues)
                    .ThenInclude(x => x.MayTinh)!.ThenInclude(x => x!.DongMayTinh);
        }

        private static object ToResponse(HopDong x)
        {
            return new
            {
                x.Id,
                x.DonThueId,
                x.MaHopDong,
                x.NgayLap,
                x.NoiDung,
                x.FileUrl,
                x.TrangThai,
                MaDonThue = x.DonThue?.MaDonThue,
                TenDonVi = x.DonThue?.DonVi?.TenDonVi,
                NgayBatDau = x.DonThue?.NgayBatDau,
                NgayKetThuc = x.DonThue?.NgayKetThucDuKien,
                TienThue = x.DonThue?.TongTienThue ?? 0,
                TienDatCoc = x.DonThue?.TongTienDatCoc ?? 0,
                TienDenBu = x.DonThue?.TongTienDenBu ?? 0,
                Devices = x.DonThue?.ChiTietDonThues.Select(detail => (object)new
                {
                    detail.MayTinhId,
                    MaTaiSan = detail.MayTinh?.MaTaiSan,
                    TenMay = detail.MayTinh?.DongMayTinh == null
                        ? null
                        : detail.MayTinh.DongMayTinh.Hang + " " + detail.MayTinh.DongMayTinh.TenDong
                }) ?? Enumerable.Empty<object>()
            };
        }

        private IActionResult? ValidateCode(string? value)
        {
            var code = value?.Trim();
            if (string.IsNullOrEmpty(code))
                return BadRequest(new { message = "Mã hợp đồng không được để trống." });
            if (code.Length > 50)
                return BadRequest(new { message = "Mã hợp đồng không được quá 50 ký tự." });
            return null;
        }

        private Task<bool> CodeExists(string code, int? excludedId = null)
        {
            return _context.HopDongs.AnyAsync(x =>
                (!excludedId.HasValue || x.Id != excludedId.Value) &&
                x.MaHopDong.ToLower() == code.ToLower());
        }

        private static string? Normalize(string? value)
        {
            var text = value?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
    }
}
