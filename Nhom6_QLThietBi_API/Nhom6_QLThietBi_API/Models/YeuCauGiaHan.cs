using System;

namespace Nhom6_QLThietBi_API.Models
{
    public class YeuCauGiaHan
    {
        public int Id { get; set; }
        public int DonThueId { get; set; }
        public DateTime NgayKetThucMoi { get; set; }
        public string? LyDo { get; set; }
        public string TrangThai { get; set; } = "cho_duyet";
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public int? NguoiDuyetId { get; set; }
        public DateTime? NgayDuyet { get; set; }

        public DonThue? DonThue { get; set; }
        public NguoiDung? NguoiDuyet { get; set; }
    }
}