using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;

namespace Nhom6_QLThietBi_API.Services
{
    public static class RentalOrderLifecycleService
    {
        public static async Task<bool> SyncCompletionAsync(
            AppDbContext context,
            int orderId)
        {
            var order = await context.DonThues
                .Include(x => x.ChiTietDonThues)
                .Include(x => x.HoaDons)
                    .ThenInclude(x => x.ThanhToans)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (order == null || order.TrangThai is "huy" or "tu_choi")
                return false;

            var hasAcceptedReturn = await context.YeuCauTraMays.AnyAsync(x =>
                x.DonThueId == orderId && x.TrangThai == "da_nhan_may");
            var allDevicesReturned = order.ChiTietDonThues.Count > 0 &&
                order.ChiTietDonThues.All(x => x.TrangThai == "da_tra");
            var detailIds = order.ChiTietDonThues.Select(x => x.Id).ToList();
            var hasPendingDamage = await context.BaoCaoHuHongs.AnyAsync(x =>
                detailIds.Contains(x.ChiTietDonThueId) &&
                x.TrangThai == "cho_xu_ly");

            var invoice = order.HoaDons
                .Where(x => x.TrangThai != "huy")
                .OrderByDescending(x => x.NgayLap)
                .FirstOrDefault();
            var isPaidInFull = invoice != null &&
                invoice.ThanhToans.Sum(x => x.SoTien) >= invoice.TongThanhToan;
            var canComplete = hasAcceptedReturn && allDevicesReturned &&
                !hasPendingDamage && isPaidInFull;
            var changed = false;

            if (canComplete)
            {
                if (order.TrangThai != "hoan_thanh")
                {
                    order.TrangThai = "hoan_thanh";
                    changed = true;
                }

                if (!order.NgayKetThucThucTe.HasValue)
                {
                    order.NgayKetThucThucTe = DateTime.Now;
                    changed = true;
                }

                if (invoice!.TrangThai != "da_thanh_toan")
                {
                    invoice.TrangThai = "da_thanh_toan";
                    changed = true;
                }

                var activeContracts = await context.HopDongs
                    .Where(x => x.DonThueId == orderId && x.TrangThai == "hieu_luc")
                    .ToListAsync();
                foreach (var contract in activeContracts)
                {
                    contract.TrangThai = "het_hieu_luc";
                    changed = true;
                }
            }
            else if (order.TrangThai == "hoan_thanh" &&
                     hasAcceptedReturn && allDevicesReturned)
            {
                order.TrangThai = "yeu_cau_tra";
                changed = true;
            }

            if (changed)
                await context.SaveChangesAsync();

            return canComplete;
        }
    }
}
