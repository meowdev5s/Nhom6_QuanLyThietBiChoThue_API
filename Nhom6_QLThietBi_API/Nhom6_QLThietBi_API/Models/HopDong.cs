using System;

namespace Nhom6_QLThietBi_API.Models
{
    public class HopDong
    {
        public int Id { get; set; }
        public int DonThueId { get; set; }
        public string MaHopDong { get; set; } = string.Empty;
        public DateTime NgayLap { get; set; }
        public string? NoiDung { get; set; }
        public string? FileUrl { get; set; }
        public string TrangThai { get; set; } = "hieu_luc";

        // Navigation property
        public DonThue? DonThue { get; set; }
    }
}