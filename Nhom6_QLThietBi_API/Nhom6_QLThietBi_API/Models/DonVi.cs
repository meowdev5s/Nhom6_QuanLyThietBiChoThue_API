namespace Nhom6_QLThietBi_API.Models
{
    public class DonVi
    {
        public int Id { get; set; }
        public string TenDonVi { get; set; } = string.Empty;
        public string? DiaChi { get; set; }
        public string? MaSoThue { get; set; }
        public string? NguoiDaiDien { get; set; }
        public string? Email { get; set; }
        public string? SoDienThoai { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public DateTime NgayTao { get; set; }

        public ICollection<DonThue> DonThues { get; set; } = new List<DonThue>();

        public ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();
    }
}