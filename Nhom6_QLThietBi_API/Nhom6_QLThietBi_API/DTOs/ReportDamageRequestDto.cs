namespace Nhom6_QLThietBi_API.DTOs
{
    public class ReportDamageRequestDto
    {
        public int ChiTietDonThueId { get; set; }
        public int NguoiBaoCaoId { get; set; }
        public string MoTa { get; set; } = string.Empty;
        public string HinhAnhUrl { get; set; } = string.Empty;
    }
}