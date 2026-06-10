using System;

namespace Nhom6_QLThietBi_API.DTOs
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public int CuocTroChuyenId { get; set; }
        public int NguoiGuiId { get; set; }
        public string TenNguoiGui { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
        public string LoaiTinNhan { get; set; } = "text";
        public bool DaDoc { get; set; }
        public DateTime NgayGui { get; set; }
    }
}