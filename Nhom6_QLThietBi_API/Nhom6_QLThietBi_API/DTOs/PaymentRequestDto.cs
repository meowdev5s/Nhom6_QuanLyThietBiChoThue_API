namespace Nhom6_QLThietBi_API.DTOs
{
    public class PaymentRequestDto
    {
        public int HoaDonId { get; set; }
        public decimal SoTien { get; set; }
        public string PhuongThuc { get; set; } = string.Empty; // tien_mat, chuyen_khoan, vi_dien_tu
        public string MaGiaoDich { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;
    }
}