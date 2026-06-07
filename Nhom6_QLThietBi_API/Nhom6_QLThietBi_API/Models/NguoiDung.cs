namespace Nhom6_QLThietBi_API.Models
{
    public class NguoiDung
    {
        public int Id { get; set; }
        public int? DonViId { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string TenDangNhap { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? SoDienThoai { get; set; }
        public string MatKhauHash { get; set; } = string.Empty;
        public string VaiTro { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public DateTime NgayTao { get; set; }

        public DonVi? DonVi { get; set; }
    }
}