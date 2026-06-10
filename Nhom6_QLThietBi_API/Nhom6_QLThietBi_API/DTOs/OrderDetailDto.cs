using Nhom6_QLThietBi_API.Models;
using System;
using System.Collections.Generic;

namespace Nhom6_QLThietBi_API.DTOs
{
    public class OrderDetailDto
    {
        public int Id { get; set; }
        public string MaDonThue { get; set; } = string.Empty;
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThucDuKien { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public decimal TongTienThue { get; set; }
        public decimal TongTienDatCoc { get; set; }
        public string MucDichSuDung { get; set; } = string.Empty;
        public List<DeviceCatalogDto> DanhSachMay { get; set; } = new List<DeviceCatalogDto>();
        public string? MaHopDong { get; set; }
        public string? MaHoaDon { get; set; }
    }
}