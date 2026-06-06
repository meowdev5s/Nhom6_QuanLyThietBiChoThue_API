namespace Nhom6_QLThietBi_API.Models
{
    public class MayTinh
    {
        public int Id { get; set; }
        public int DongMayTinhId { get; set; }
        public string MaTaiSan { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string LoaiMay { get; set; } = string.Empty;
        public string? Cpu { get; set; }
        public string? Ram { get; set; }
        public string? OCung { get; set; }
        public string? Gpu { get; set; }
        public string? ManHinh { get; set; }
        public string? HeDieuHanh { get; set; }
        public decimal GiaTriMay { get; set; }
        public decimal GiaThueNgay { get; set; }
        public decimal TiLeDatCoc { get; set; }
        public string TinhTrang { get; set; } = string.Empty;
        public DateTime? NgayNhap { get; set; }
        public string? GhiChu { get; set; }
        public DateTime NgayTao { get; set; }

        public DongMayTinh? DongMayTinh { get; set; }

        public ICollection<ChiTietDonThue> ChiTietDonThues { get; set; } = new List<ChiTietDonThue>();
    }
}
