using System;

namespace Nhom6_QLThietBi_API.Models
{
    public class TinNhan
    {
        public int Id { get; set; }
        public int CuocTroChuyenId { get; set; }
        public int NguoiGuiId { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public string LoaiTinNhan { get; set; } = "text";
        public bool DaDoc { get; set; } = false;
        public DateTime NgayGui { get; set; } = DateTime.Now;

        public CuocTroChuyen? CuocTroChuyen { get; set; }
        public NguoiDung? NguoiGui { get; set; }
    }
}