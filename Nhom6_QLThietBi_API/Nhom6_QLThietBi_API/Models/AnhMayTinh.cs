using System;

namespace Nhom6_QLThietBi_API.Models
{
    public class AnhMayTinh
    {
        public int Id { get; set; }
        public int MayTinhId { get; set; }
        public string DuongDanAnh { get; set; } = string.Empty;
        public bool LaAnhDaiDien { get; set; } = false;
        public DateTime NgayTao { get; set; } = DateTime.Now;

        public MayTinh? MayTinh { get; set; }
    }
}