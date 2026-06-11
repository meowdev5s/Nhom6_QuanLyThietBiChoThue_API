using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;

namespace Nhom6_QLThietBi_API.Controllers
{
    [Authorize(Roles = "admin")]
    [ApiController]
    [Route("api/admin/reports")]
    public class AdminReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminReportsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(DateTime? from, DateTime? to)
        {
            var endDate = (to ?? DateTime.Today).Date;
            var startDate = (from ?? endDate.AddMonths(-5).AddDays(1 - endDate.Day)).Date;

            if (startDate > endDate)
                return BadRequest(new { message = "Ngày bắt đầu không được sau ngày kết thúc." });
            if ((endDate - startDate).TotalDays > 1095)
                return BadRequest(new { message = "Khoảng báo cáo không được vượt quá 3 năm." });

            var endExclusive = endDate.AddDays(1);
            var payments = _context.ThanhToans.AsNoTracking()
                .Where(x => x.NgayThanhToan >= startDate && x.NgayThanhToan < endExclusive);

            var totalRevenue = await payments.SumAsync(x => (decimal?)x.SoTien) ?? 0;
            var paymentCount = await payments.CountAsync();
            var monthlyRevenue = await payments
                .GroupBy(x => new { x.NgayThanhToan.Year, x.NgayThanhToan.Month })
                .OrderBy(x => x.Key.Year)
                .ThenBy(x => x.Key.Month)
                .Select(x => new
                {
                    year = x.Key.Year,
                    month = x.Key.Month,
                    revenue = x.Sum(item => item.SoTien),
                    transactionCount = x.Count()
                })
                .ToListAsync();

            var paymentMethods = await payments
                .GroupBy(x => x.PhuongThuc)
                .Select(x => new
                {
                    method = x.Key,
                    amount = x.Sum(item => item.SoTien),
                    count = x.Count()
                })
                .OrderByDescending(x => x.amount)
                .ToListAsync();

            var validStatuses = new[]
            {
                "da_duyet", "dang_thue", "yeu_cau_tra", "hoan_thanh", "qua_han"
            };
            var orders = _context.DonThues.AsNoTracking()
                .Where(x => validStatuses.Contains(x.TrangThai) &&
                            x.NgayBatDau < endExclusive &&
                            (x.NgayKetThucThucTe ?? x.NgayKetThucDuKien) >= startDate);

            var rentalOrderCount = await orders.CountAsync();
            var usedDeviceCount = await _context.ChiTietDonThues.AsNoTracking()
                .Where(x => x.DonThue != null &&
                            validStatuses.Contains(x.DonThue.TrangThai) &&
                            x.DonThue.NgayBatDau < endExclusive &&
                            (x.DonThue.NgayKetThucThucTe ?? x.DonThue.NgayKetThucDuKien) >= startDate)
                .Select(x => x.MayTinhId)
                .Distinct()
                .CountAsync();
            var totalDeviceCount = await _context.MayTinhs.CountAsync();
            var utilizationRate = totalDeviceCount == 0
                ? 0
                : Math.Round(usedDeviceCount * 100m / totalDeviceCount, 2);

            return Ok(new
            {
                from = startDate,
                to = endDate,
                overview = new
                {
                    totalRevenue,
                    paymentCount,
                    rentalOrderCount,
                    usedDeviceCount,
                    totalDeviceCount,
                    utilizationRate
                },
                monthlyRevenue,
                paymentMethods
            });
        }
    }
}
