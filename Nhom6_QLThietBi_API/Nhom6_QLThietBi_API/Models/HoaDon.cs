namespace Nhom6_QLThietBi_API.Models
{
    public class HoaDon
    {
        public int Id { get; set; }
        public int DonThueId { get; set; }
        public string MaHoaDon { get; set; } = string.Empty;
        public decimal TienThue { get; set; }
        public decimal TienDatCoc { get; set; }
        public decimal TienDenBu { get; set; }
        public decimal TongThanhToan { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public DateTime NgayLap { get; set; }

        public DonThue? DonThue { get; set; }
    }
}
