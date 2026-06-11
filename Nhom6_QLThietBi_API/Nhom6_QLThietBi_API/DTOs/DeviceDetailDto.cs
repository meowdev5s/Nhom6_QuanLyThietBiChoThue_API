namespace Nhom6_QLThietBi_API.DTOs
{
    public class DeviceDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Hang { get; set; } = string.Empty;
        public string MaTaiSan { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string LoaiMay { get; set; } = string.Empty;
        public string Cpu { get; set; } = string.Empty;
        public string Ram { get; set; } = string.Empty;
        public string Ssd { get; set; } = string.Empty;
        public string Gpu { get; set; } = string.Empty;
        public string Display { get; set; } = string.Empty;
        public string HeDieuHanh { get; set; } = string.Empty;
        public decimal GiaThueNgay { get; set; }
        public decimal GiaTriMay { get; set; }
        public decimal TiLeDatCoc { get; set; }
        public decimal TienDatCocDuKien { get; set; }
        public string TinhTrang { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new();
    }
}
