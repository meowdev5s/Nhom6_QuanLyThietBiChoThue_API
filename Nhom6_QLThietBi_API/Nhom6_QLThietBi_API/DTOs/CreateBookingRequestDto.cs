using System;
using System.Collections.Generic;

namespace Nhom6_QLThietBi_API.DTOs
{
    public class CreateBookingRequestDto
    {
        public int DonViId { get; set; }
        public int NguoiTaoId { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThucDuKien { get; set; }
        public string MucDichSuDung { get; set; } = string.Empty;
        public List<int> MayTinhIds { get; set; } = new List<int>();
    }
}