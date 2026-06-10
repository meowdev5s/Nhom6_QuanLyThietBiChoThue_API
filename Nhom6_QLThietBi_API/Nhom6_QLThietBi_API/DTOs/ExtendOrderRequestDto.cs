using System;

namespace Nhom6_QLThietBi_API.DTOs
{
    public class ExtendOrderRequestDto
    {
        public int DonThueId { get; set; }
        public DateTime NgayKetThucMoi { get; set; }
        public string LyDo { get; set; } = string.Empty;
    }
}