using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;
using Nhom6_QLThietBi_API.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nhom6_QLThietBi_API.Controllers
{
    [Route("api/user/[controller]")]
    [ApiController]
    public class UserChatController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UserChatController(AppDbContext context) => _context = context;

        [HttpGet("session/{userId}")]
        public async Task<IActionResult> GetOrCreateSession(int userId)
        {
            var user = await _context.NguoiDungs.FindAsync(userId);
            if (user == null) return NotFound(new { message = "Không tìm thấy tài khoản." });

            var chat = await _context.CuocTroChuyens
                .Where(x => x.KhachHangId == userId &&
                            (x.TrangThai == "dang_mo" ||
                             x.TrangThai == "dang_xu_ly"))
                .OrderByDescending(x => x.NgayCapNhat)
                .FirstOrDefaultAsync();
            if (chat == null)
            {
                chat = new CuocTroChuyen
                {
                    DonViId = user.DonViId,
                    KhachHangId = user.Id,
                    TieuDe = "Hỗ trợ khách hàng - " + user.HoTen,
                    TrangThai = "dang_mo",
                    NgayTao = DateTime.Now,
                    NgayCapNhat = DateTime.Now
                };
                _context.CuocTroChuyens.Add(chat);
                await _context.SaveChangesAsync();
            }

            return Ok(new { chatId = chat.Id });
        }

        [HttpGet("messages/{chatId}")]
        public async Task<IActionResult> GetMessages(int chatId)
        {
            var data = await _context.TinNhans
                .Include(t => t.NguoiGui)
                .Where(t => t.CuocTroChuyenId == chatId)
                .OrderBy(t => t.NgayGui)
                .Select(t => new ChatMessageDto
                {
                    Id = t.Id,
                    CuocTroChuyenId = t.CuocTroChuyenId,
                    NguoiGuiId = t.NguoiGuiId,
                    TenNguoiGui = t.NguoiGui.HoTen,
                    NoiDung = t.NoiDung,
                    LoaiTinNhan = t.LoaiTinNhan,
                    DaDoc = t.DaDoc,
                    NgayGui = t.NgayGui
                }).ToListAsync();
            return Ok(data);
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageDto req)
        {
            var tn = new TinNhan
            {
                CuocTroChuyenId = req.CuocTroChuyenId,
                NguoiGuiId = req.NguoiGuiId,
                NoiDung = req.NoiDung,
                LoaiTinNhan = req.LoaiTinNhan,
                DaDoc = false,
                NgayGui = DateTime.Now
            };

            _context.TinNhans.Add(tn);

            var cuocTroChuyen = await _context.CuocTroChuyens.FindAsync(req.CuocTroChuyenId);
            if (cuocTroChuyen != null) cuocTroChuyen.NgayCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(tn);
        }
    }
}
