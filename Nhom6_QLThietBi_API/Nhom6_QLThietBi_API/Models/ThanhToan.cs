namespace Nhom6_QLThietBi_API.Models
{
    public class ThanhToan
    {
        public int Id { get; set; }
        public int HoaDonId { get; set; }
        public decimal SoTien { get; set; }
        public string PhuongThuc { get; set; } = string.Empty;
        public string? MaGiaoDich { get; set; }
        public string? GhiChu { get; set; }
        public DateTime NgayThanhToan { get; set; }

        public HoaDon? HoaDon { get; set; }
    }
}