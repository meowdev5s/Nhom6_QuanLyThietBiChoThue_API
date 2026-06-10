using System;

namespace Nhom6_QLThietBi_API.Models
{
    public class YeuCauTraMay
    {
        public int Id { get; set; }
        public int DonThueId { get; set; }
        public DateTime NgayYeuCau { get; set; } = DateTime.Now;
        public string? LyDo { get; set; }
        public string? GhiChu { get; set; }
        public string TrangThai { get; set; } = "cho_xu_ly";
        public int? NguoiXuLyId { get; set; }
        public DateTime? NgayXuLy { get; set; }

        public DonThue? DonThue { get; set; }
        public NguoiDung? NguoiXuLy { get; set; }
    }
}