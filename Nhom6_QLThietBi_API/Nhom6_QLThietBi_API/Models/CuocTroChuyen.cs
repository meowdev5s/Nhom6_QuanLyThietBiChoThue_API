using System;
using System.Collections.Generic;

namespace Nhom6_QLThietBi_API.Models
{
    public class CuocTroChuyen
    {
        public int Id { get; set; }
        public int? DonViId { get; set; }
        public int KhachHangId { get; set; }
        public int? NhanVienPhuTrachId { get; set; }
        public string? TieuDe { get; set; }
        public string TrangThai { get; set; } = "dang_mo";
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public DateTime NgayCapNhat { get; set; } = DateTime.Now;

        public DonVi? DonVi { get; set; }
        public NguoiDung? KhachHang { get; set; }
        public NguoiDung? NhanVienPhuTrach { get; set; }
        public ICollection<TinNhan> TinNhans { get; set; } = new List<TinNhan>();
    }
}