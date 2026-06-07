namespace Nhom6_QLThietBi_API.Models
{
    public class MucDoHuHong
    {
        public int Id { get; set; }
        public string TenMucDo { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public decimal PhanTramDenBu { get; set; }
    }
}