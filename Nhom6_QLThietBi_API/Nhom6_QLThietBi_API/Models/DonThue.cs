namespace Nhom6_QLThietBi_API.Models
{
    public class DonThue
    {
        public int Id { get; set; }
        public string MaDonThue { get; set; } = string.Empty;
        public int DonViId { get; set; }
        public int NguoiTaoId { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThucDuKien { get; set; }
        public DateTime? NgayKetThucThucTe { get; set; }
        public string? MucDichSuDung { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public decimal TongTienThue { get; set; }
        public decimal TongTienDatCoc { get; set; }
        public decimal TongTienDenBu { get; set; }
        public string? GhiChu { get; set; }
        public DateTime NgayTao { get; set; }

        public DonVi? DonVi { get; set; }
        public ICollection<ChiTietDonThue> ChiTietDonThues { get; set; } = new List<ChiTietDonThue>();
    }
}
