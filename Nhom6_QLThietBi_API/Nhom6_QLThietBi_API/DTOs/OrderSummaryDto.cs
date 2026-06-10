using System;

namespace Nhom6_QLThietBi_API.DTOs
{
    public class OrderSummaryDto
    {
        public int Id { get; set; }
        public string MaDonThue { get; set; } = string.Empty;
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThucDuKien { get; set; }
        public decimal TongTien { get; set; }
        public string TrangThai { get; set; } = string.Empty;
    }
}