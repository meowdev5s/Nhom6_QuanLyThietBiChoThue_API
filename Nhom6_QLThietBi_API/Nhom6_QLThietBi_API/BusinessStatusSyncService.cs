using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Data;

namespace Nhom6_QLThietBi_API.Services
{
    public static class BusinessStatusSyncService
    {
        public static async Task SyncAsync(AppDbContext context)
        {
            var today = DateTime.Today;
            var changed = false;

            var overdueOrders = await context.DonThues
                .Where(x => (x.TrangThai == "da_duyet" || x.TrangThai == "dang_thue") &&
                            x.NgayKetThucDuKien < today)
                .ToListAsync();

            foreach (var order in overdueOrders)
            {
                order.TrangThai = "qua_han";
                changed = true;
            }

            var expiredContracts = await context.HopDongs
                .Include(x => x.DonThue)
                .Where(x => x.TrangThai == "hieu_luc" &&
                            x.DonThue != null &&
                            (x.DonThue.NgayKetThucThucTe ?? x.DonThue.NgayKetThucDuKien) < today)
                .ToListAsync();

            foreach (var contract in expiredContracts)
            {
                contract.TrangThai = "het_hieu_luc";
                changed = true;
            }

            if (changed)
                await context.SaveChangesAsync();
        }
    }
}
