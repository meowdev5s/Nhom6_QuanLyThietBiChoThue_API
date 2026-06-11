using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;

namespace Nhom6_QLThietBi_API.Controllers
{
    [ApiController]
    [Route("api/admin/chats")]
    public class AdminChatsController : ControllerBase
    {
        public class UpdateChatRequest
        {
            public string Status { get; set; } = string.Empty;
            public int? NhanVienPhuTrachId { get; set; }
        }

        public class SendAdminMessageRequest
        {
            public int NguoiGuiId { get; set; }
            public string NoiDung { get; set; } = string.Empty;
            public string LoaiTinNhan { get; set; } = "text";
        }

        private readonly AppDbContext _context;

        public AdminChatsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetChats()
        {
            var chats = await _context.CuocTroChuyens
                .AsNoTracking()
                .OrderByDescending(x => x.NgayCapNhat)
                .Select(x => new
                {
                    x.Id,
                    x.DonViId,
                    TenDonVi = x.DonVi == null ? null : x.DonVi.TenDonVi,
                    x.KhachHangId,
                    TenKhachHang = x.KhachHang == null ? null : x.KhachHang.HoTen,
                    x.NhanVienPhuTrachId,
                    TenNhanVien = x.NhanVienPhuTrach == null
                        ? null : x.NhanVienPhuTrach.HoTen,
                    x.TieuDe,
                    x.TrangThai,
                    x.NgayTao,
                    x.NgayCapNhat,
                    SoTinChuaDoc = x.TinNhans.Count(t => !t.DaDoc),
                    TinNhanCuoi = x.TinNhans
                        .OrderByDescending(t => t.NgayGui)
                        .Select(t => t.NoiDung)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(chats);
        }

        [HttpGet("{id}/messages")]
        public async Task<IActionResult> GetMessages(int id)
        {
            var chatExists = await _context.CuocTroChuyens.AnyAsync(x => x.Id == id);
            if (!chatExists)
                return NotFound(new { message = "Không tìm thấy cuộc trò chuyện." });

            var messages = await _context.TinNhans
                .Where(x => x.CuocTroChuyenId == id)
                .OrderBy(x => x.NgayGui)
                .Select(x => new
                {
                    x.Id,
                    x.CuocTroChuyenId,
                    x.NguoiGuiId,
                    TenNguoiGui = x.NguoiGui == null ? null : x.NguoiGui.HoTen,
                    x.NoiDung,
                    x.LoaiTinNhan,
                    x.DaDoc,
                    x.NgayGui
                })
                .ToListAsync();

            var unread = await _context.TinNhans
                .Where(x => x.CuocTroChuyenId == id && !x.DaDoc)
                .ToListAsync();
            foreach (var message in unread) message.DaDoc = true;
            await _context.SaveChangesAsync();

            return Ok(messages);
        }

        [HttpPost("{id}/messages")]
        public async Task<IActionResult> SendMessage(
            int id,
            [FromBody] SendAdminMessageRequest request)
        {
            var chat = await _context.CuocTroChuyens.FindAsync(id);
            if (chat == null)
                return NotFound(new { message = "Không tìm thấy cuộc trò chuyện." });
            if (chat.TrangThai == "da_dong")
                return BadRequest(new { message = "Cuộc trò chuyện đã đóng." });
            if (string.IsNullOrWhiteSpace(request.NoiDung))
                return BadRequest(new { message = "Nội dung tin nhắn không được để trống." });

            var allowedTypes = new[] { "text", "image", "system" };
            if (!allowedTypes.Contains(request.LoaiTinNhan))
                return BadRequest(new { message = "Loại tin nhắn không hợp lệ." });

            var sender = await _context.NguoiDungs.FindAsync(request.NguoiGuiId);
            if (sender == null)
                return BadRequest(new { message = "Người gửi không tồn tại." });

            if (!new[] { "admin", "nhan_vien" }.Contains(sender.VaiTro))
                return BadRequest(new { message = "Tài khoản không có quyền hỗ trợ." });

            if (chat.NhanVienPhuTrachId == null)
            {
                chat.NhanVienPhuTrachId = sender.Id;
                chat.TrangThai = "dang_xu_ly";
            }

            chat.NgayCapNhat = DateTime.Now;
            var message = new TinNhan
            {
                CuocTroChuyenId = id,
                NguoiGuiId = sender.Id,
                NoiDung = request.NoiDung.Trim(),
                LoaiTinNhan = request.LoaiTinNhan,
                DaDoc = false,
                NgayGui = DateTime.Now
            };
            _context.TinNhans.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã gửi tin nhắn.", id = message.Id });
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateChat(
            int id,
            [FromBody] UpdateChatRequest request)
        {
            var statuses = new[] { "dang_mo", "dang_xu_ly", "da_dong" };
            if (!statuses.Contains(request.Status))
                return BadRequest(new { message = "Trạng thái chat không hợp lệ." });

            var chat = await _context.CuocTroChuyens.FindAsync(id);
            if (chat == null)
                return NotFound(new { message = "Không tìm thấy cuộc trò chuyện." });

            if (request.NhanVienPhuTrachId.HasValue)
            {
                var staff = await _context.NguoiDungs
                    .FirstOrDefaultAsync(x => x.Id == request.NhanVienPhuTrachId.Value);
                if (staff == null ||
                    !new[] { "admin", "nhan_vien" }.Contains(staff.VaiTro))
                    return BadRequest(new { message = "Nhân viên phụ trách không hợp lệ." });
            }

            chat.TrangThai = request.Status;
            chat.NhanVienPhuTrachId = request.NhanVienPhuTrachId;
            chat.NgayCapNhat = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã cập nhật cuộc trò chuyện." });
        }
    }
}
