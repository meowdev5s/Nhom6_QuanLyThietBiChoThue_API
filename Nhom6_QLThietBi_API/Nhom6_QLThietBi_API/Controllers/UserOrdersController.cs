using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;
using Nhom6_QLThietBi_API.Models;
using Nhom6_QLThietBi_API.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nhom6_QLThietBi_API.Controllers
{
    [Route("api/user/[controller]")]
    [ApiController]
    public class UserOrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UserOrdersController(AppDbContext context) => _context = context;

        [HttpPost("booking")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequestDto req)
        {
            var days = (req.NgayKetThucDuKien - req.NgayBatDau).Days;
            if (days <= 0) return BadRequest("Thời gian thuê không hợp lệ");

            var donThue = new DonThue
            {
                MaDonThue = "DT-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                DonViId = req.DonViId,
                NguoiTaoId = req.NguoiTaoId,
                NgayBatDau = req.NgayBatDau,
                NgayKetThucDuKien = req.NgayKetThucDuKien,
                MucDichSuDung = req.MucDichSuDung,
                TrangThai = "cho_duyet",
                NgayTao = DateTime.Now
            };

            _context.DonThues.Add(donThue);
            await _context.SaveChangesAsync();

            decimal totalTienThue = 0;
            decimal totalTienCoc = 0;

            foreach (var mayId in req.MayTinhIds)
            {
                var may = await _context.MayTinhs.FindAsync(mayId);
                if (may == null || (may.TinhTrang != "san_sang" && may.TinhTrang != "san_ready")) continue;

                var detail = new ChiTietDonThue
                {
                    DonThueId = donThue.Id,
                    MayTinhId = may.Id,
                    GiaThueNgay = may.GiaThueNgay,
                    GiaTriMayTaiThoiDiemThue = may.GiaTriMay,
                    TiLeDatCoc = may.TiLeDatCoc,
                    TienDatCoc = may.GiaTriMay * (may.TiLeDatCoc / 100),
                    SoNgayThue = days,
                    ThanhTien = may.GiaThueNgay * days,
                    TrangThai = "dang_xu_ly"
                };

                totalTienThue += detail.ThanhTien;
                totalTienCoc += detail.TienDatCoc;
                may.TinhTrang = "dang_thue";

                _context.ChiTietDonThues.Add(detail);
            }

            donThue.TongTienThue = totalTienThue;
            donThue.TongTienDatCoc = totalTienCoc;

            var hoaDon = new HoaDon
            {
                DonThueId = donThue.Id,
                MaHoaDon = "HD-" + donThue.MaDonThue.Substring(3),
                TienThue = totalTienThue,
                TienDatCoc = totalTienCoc,
                TongThanhToan = totalTienThue + totalTienCoc,
                TrangThai = "chua_thanh_toan",
                NgayLap = DateTime.Now
            };
            _context.HoaDons.Add(hoaDon);

            await _context.SaveChangesAsync();
            return Ok(new { msg = "Đặt thành công", code = donThue.MaDonThue });
        }

        [HttpGet("my-orders/{userId}")]
        public async Task<IActionResult> GetMyOrders(int userId)
        {
            var data = await _context.DonThues
                .Where(x => x.NguoiTaoId == userId)
                .Select(x => new OrderSummaryDto
                {
                    Id = x.Id,
                    MaDonThue = x.MaDonThue,
                    NgayBatDau = x.NgayBatDau,
                    NgayKetThucDuKien = x.NgayKetThucDuKien,
                    TongTien = x.TongTienThue,
                    TrangThai = x.TrangThai
                }).ToListAsync();
            return Ok(data);
        }

        [HttpGet("detail/{id}")]
        public async Task<IActionResult> GetOrderDetail(int id)
        {
            var don = await _context.DonThues.FindAsync(id);
            if (don == null) return NotFound();

            var hd = await _context.HopDongs.FirstOrDefaultAsync(x => x.DonThueId == id);
            var hdon = await _context.HoaDons.FirstOrDefaultAsync(x => x.DonThueId == id);

            var items = await _context.ChiTietDonThues
                .Include(c => c.MayTinh)
                .ThenInclude(m => m.DongMayTinh)
                .Where(c => c.DonThueId == id)
                .Select(c => new DeviceCatalogDto
                {
                    Id = c.MayTinh.Id,
                    Name = c.MayTinh.DongMayTinh.Hang + " " + c.MayTinh.DongMayTinh.TenDong,
                    Price = c.GiaThueNgay
                }).ToListAsync();

            var dto = new OrderDetailDto
            {
                Id = don.Id,
                MaDonThue = don.MaDonThue,
                NgayBatDau = don.NgayBatDau,
                NgayKetThucDuKien = don.NgayKetThucDuKien,
                TrangThai = don.TrangThai,
                TongTienThue = don.TongTienThue,
                TongTienDatCoc = don.TongTienDatCoc,
                MucDichSuDung = don.MucDichSuDung,
                DanhSachMay = items,
                MaHopDong = hd?.MaHopDong,
                MaHoaDon = hdon?.MaHoaDon
            };
            return Ok(dto);
        }

        [HttpPost("extend")]
        public async Task<IActionResult> ExtendOrder([FromBody] ExtendOrderRequestDto req)
        {
            var order = await _context.DonThues
                .FirstOrDefaultAsync(x => x.Id == req.DonThueId);

            if (order == null)
            {
                return NotFound(new { message = "Không tìm thấy đơn thuê." });
            }

            if (order.TrangThai != "dang_thue" && order.TrangThai != "qua_han")
            {
                return BadRequest(new
                {
                    message = "Chỉ đơn đang thuê hoặc quá hạn mới được yêu cầu gia hạn."
                });
            }

            if (req.NgayKetThucMoi.Date <= order.NgayKetThucDuKien.Date)
            {
                return BadRequest(new
                {
                    message = "Ngày kết thúc mới phải sau ngày kết thúc hiện tại."
                });
            }

            var pendingRequestExists = await _context.YeuCauGiaHans
                .AnyAsync(x =>
                    x.DonThueId == req.DonThueId &&
                    x.TrangThai == "cho_duyet");

            if (pendingRequestExists)
            {
                return Conflict(new
                {
                    message = "Đơn thuê đã có yêu cầu gia hạn đang chờ duyệt."
                });
            }

            var yc = new YeuCauGiaHan
            {
                DonThueId = req.DonThueId,
                NgayKetThucMoi = req.NgayKetThucMoi.Date,
                LyDo = string.IsNullOrWhiteSpace(req.LyDo)
                    ? null
                    : req.LyDo.Trim(),
                TrangThai = "cho_duyet",
                NgayTao = DateTime.Now
            };
            _context.YeuCauGiaHans.Add(yc);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = "Đã gửi yêu cầu gia hạn.",
                id = yc.Id
            });
        }

        [HttpPost("return")]
        public async Task<IActionResult> ReturnOrder([FromBody] ReturnMachineRequestDto req)
        {
            var order = await _context.DonThues
                .FirstOrDefaultAsync(x => x.Id == req.DonThueId);

            if (order == null)
            {
                return NotFound(new { message = "Không tìm thấy đơn thuê." });
            }

            if (order.TrangThai != "dang_thue" && order.TrangThai != "qua_han")
            {
                return BadRequest(new
                {
                    message = "Chỉ đơn đang thuê hoặc quá hạn mới được yêu cầu trả máy."
                });
            }

            var pendingRequestExists = await _context.YeuCauTraMays
                .AnyAsync(x =>
                    x.DonThueId == req.DonThueId &&
                    x.TrangThai == "cho_xu_ly");

            if (pendingRequestExists)
            {
                return Conflict(new
                {
                    message = "Đơn thuê đã có yêu cầu trả máy đang chờ xử lý."
                });
            }

            var yc = new YeuCauTraMay
            {
                DonThueId = req.DonThueId,
                LyDo = string.IsNullOrWhiteSpace(req.LyDo)
                    ? null
                    : req.LyDo.Trim(),
                GhiChu = string.IsNullOrWhiteSpace(req.GhiChu)
                    ? null
                    : req.GhiChu.Trim(),
                NgayYeuCau = DateTime.Now,
                TrangThai = "cho_xu_ly"
            };

            order.TrangThai = "yeu_cau_tra";
            _context.YeuCauTraMays.Add(yc);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đã gửi yêu cầu trả máy.",
                id = yc.Id
            });
        }
    }
}
