using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;

namespace Nhom6_QLThietBi_API.Controllers
{
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
            var contracts = await _context.HopDongs
                .AsNoTracking()
                .OrderByDescending(x => x.NgayLap)
                .Select(x => new
                {
                    x.Id, x.DonThueId, x.MaHopDong, x.NgayLap,
                    x.NoiDung, x.FileUrl, x.TrangThai,
                    MaDonThue = x.DonThue == null ? null : x.DonThue.MaDonThue,
                    TenDonVi = x.DonThue == null || x.DonThue.DonVi == null
                        ? null : x.DonThue.DonVi.TenDonVi,
                    NgayBatDau = x.DonThue == null
                        ? (DateTime?)null : x.DonThue.NgayBatDau,
                    NgayKetThuc = x.DonThue == null
                        ? (DateTime?)null : x.DonThue.NgayKetThucDuKien,
                    TienThue = x.DonThue == null ? 0 : x.DonThue.TongTienThue,
                    TienDatCoc = x.DonThue == null ? 0 : x.DonThue.TongTienDatCoc,
                    TienDenBu = x.DonThue == null ? 0 : x.DonThue.TongTienDenBu,
                    Devices = x.DonThue!.ChiTietDonThues.Select(detail => new
                    {
                        detail.MayTinhId,
                        MaTaiSan = detail.MayTinh == null ? null : detail.MayTinh.MaTaiSan,
                        TenMay = detail.MayTinh == null ||
                                 detail.MayTinh.DongMayTinh == null
                            ? null
                            : detail.MayTinh.DongMayTinh.Hang + " " +
                              detail.MayTinh.DongMayTinh.TenDong
                    })
                })
                .ToListAsync();
            return Ok(contracts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateContract(
            [FromBody] CreateContractRequest request)
        {
            var code = request.MaHopDong.Trim();
            if (request.DonThueId <= 0 || string.IsNullOrWhiteSpace(code))
                return BadRequest(new { message = "Đơn thuê hoặc mã hợp đồng không hợp lệ." });
            if (code.Length > 50)
                return BadRequest(new { message = "Mã hợp đồng không được quá 50 ký tự." });

            var order = await _context.DonThues
                .FirstOrDefaultAsync(x => x.Id == request.DonThueId);
            if (order == null)
                return NotFound(new { message = "Không tìm thấy đơn thuê." });

            var statuses = new[] { "da_duyet", "dang_thue", "qua_han" };
            if (!statuses.Contains(order.TrangThai))
                return BadRequest(new { message = "Đơn thuê chưa đủ điều kiện lập hợp đồng." });
            if (await _context.HopDongs.AnyAsync(x => x.DonThueId == request.DonThueId))
                return Conflict(new { message = "Đơn thuê này đã có hợp đồng." });
            if (await _context.HopDongs.AnyAsync(
                    x => x.MaHopDong.ToLower() == code.ToLower()))
                return Conflict(new { message = "Mã hợp đồng đã tồn tại." });

            var contract = new HopDong
            {
                DonThueId = request.DonThueId,
                MaHopDong = code,
                NgayLap = request.NgayLap == default
                    ? DateTime.Today : request.NgayLap.Date,
                NoiDung = Normalize(request.NoiDung),
                FileUrl = Normalize(request.FileUrl),
                TrangThai = "hieu_luc"
            };
            _context.HopDongs.Add(contract);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Tạo hợp đồng thành công.", id = contract.Id });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdateContractStatusRequest request)
        {
            var statuses = new[] { "hieu_luc", "het_hieu_luc", "huy" };
            if (!statuses.Contains(request.Status))
                return BadRequest(new { message = "Trạng thái hợp đồng không hợp lệ." });

            var contract = await _context.HopDongs.FindAsync(id);
            if (contract == null)
                return NotFound(new { message = "Không tìm thấy hợp đồng." });

            contract.TrangThai = request.Status;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật hợp đồng thành công." });
        }

        private static string? Normalize(string? value)
        {
            var text = value?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
    }
}
