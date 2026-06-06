namespace Nhom6_QLThietBi_API.Models
{
    public class DongMayTinh
    {
        public int Id { get; set; }
        public string TenDong { get; set; } = string.Empty;
        public string Hang { get; set; } = string.Empty;
        public string? MoTa { get; set; }

        public ICollection<MayTinh> MayTinhs { get; set; } = new List<MayTinh>();
    }
}
