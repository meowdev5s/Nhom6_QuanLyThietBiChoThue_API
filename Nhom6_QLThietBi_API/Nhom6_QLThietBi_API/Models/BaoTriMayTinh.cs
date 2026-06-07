namespace Nhom6_QLThietBi_API.Models
{
    public class BaoTriMayTinh
    {
        public int Id { get; set; }
        public int MayTinhId { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public decimal ChiPhi { get; set; }
        public string TrangThai { get; set; } = string.Empty;

        public MayTinh? MayTinh { get; set; }
    }
}