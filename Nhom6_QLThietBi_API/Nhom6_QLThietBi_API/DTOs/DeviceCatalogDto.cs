namespace Nhom6_QLThietBi_API.Models
{
    public class DeviceCatalogDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Cpu { get; set; } = string.Empty;
        public string Ram { get; set; } = string.Empty;
        public string Ssd { get; set; } = string.Empty;
        public string Gpu { get; set; } = string.Empty;
        public string Display { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal MachineValue { get; set; }
        public decimal DepositRate { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}