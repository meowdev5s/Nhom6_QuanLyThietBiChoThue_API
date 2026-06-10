using System;

namespace Nhom6_QLThietBi_API.Models
{
    public class BaoCaoHuHong
    {
        public int Id { get; set; }
        public int ChiTietDonThueId { get; set; }
        public int NguoiBaoCaoId { get; set; }
        public string MoTa { get; set; } = string.Empty;
        public string? HinhAnhUrl { get; set; }
        public int? MucDoHuHongId { get; set; }
        public decimal TienDenBu { get; set; }
        public string TrangThai { get; set; } = "cho_xu_ly";
        public DateTime NgayBaoCao { get; set; } = DateTime.Now;

        public ChiTietDonThue? ChiTietDonThue { get; set; }
        public NguoiDung? NguoiBaoCao { get; set; }
        public MucDoHuHong? MucDoHuHong { get; set; }
    }
}