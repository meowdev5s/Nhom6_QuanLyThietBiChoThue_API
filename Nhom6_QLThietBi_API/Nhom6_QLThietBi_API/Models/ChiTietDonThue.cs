namespace Nhom6_QLThietBi_API.Models
{
    public class ChiTietDonThue
    {
        public int Id { get; set; }
        public int DonThueId { get; set; }
        public int MayTinhId { get; set; }
        public decimal GiaThueNgay { get; set; }
        public decimal GiaTriMayTaiThoiDiemThue { get; set; }
        public decimal TiLeDatCoc { get; set; }
        public decimal TienDatCoc { get; set; }
        public int SoNgayThue { get; set; }
        public decimal ThanhTien { get; set; }
        public string? TinhTrangBanGiao { get; set; }
        public string? TinhTrangTraMay { get; set; }
        public string TrangThai { get; set; } = string.Empty;

        public DonThue? DonThue { get; set; }
        public MayTinh? MayTinh { get; set; }
    }
}