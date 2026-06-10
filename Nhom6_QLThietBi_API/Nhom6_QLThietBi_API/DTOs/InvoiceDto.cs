using System;

namespace Nhom6_QLThietBi_API.DTOs
{
    public class InvoiceDto
    {
        public int Id { get; set; }
        public int DonThueId { get; set; }
        public string MaDonThue { get; set; } = string.Empty;
        public string MaHoaDon { get; set; } = string.Empty;
        public decimal TienThue { get; set; }
        public decimal TienDatCoc { get; set; }
        public decimal TienDenBu { get; set; }
        public decimal TongThanhToan { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public DateTime NgayLap { get; set; }
    }
}