using Microsoft.EntityFrameworkCore;
using Nhom6_QLThietBi_API.Models;

namespace Nhom6_QLThietBi_API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<DongMayTinh> DongMayTinhs => Set<DongMayTinh>();
        public DbSet<MayTinh> MayTinhs => Set<MayTinh>();
        public DbSet<DonThue> DonThues => Set<DonThue>();
        public DbSet<HoaDon> HoaDons => Set<HoaDon>();

        public DbSet<DonVi> DonVis => Set<DonVi>();
        public DbSet<ChiTietDonThue> ChiTietDonThues => Set<ChiTietDonThue>();

        public DbSet<NguoiDung> NguoiDungs => Set<NguoiDung>();

        public DbSet<BaoTriMayTinh> BaoTriMayTinhs => Set<BaoTriMayTinh>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<BaoTriMayTinh>(entity =>
            {
                entity.ToTable("BaoTriMayTinh");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.MayTinhId).HasColumnName("mayTinhId");
                entity.Property(e => e.NgayBatDau).HasColumnName("ngayBatDau");
                entity.Property(e => e.NgayKetThuc).HasColumnName("ngayKetThuc");
                entity.Property(e => e.NoiDung).HasColumnName("noiDung");
                entity.Property(e => e.ChiPhi).HasColumnName("chiPhi");
                entity.Property(e => e.TrangThai).HasColumnName("trangThai");

                entity.HasOne(e => e.MayTinh)
                    .WithMany()
                    .HasForeignKey(e => e.MayTinhId);
            });

            modelBuilder.Entity<NguoiDung>(entity =>
            {
                entity.ToTable("NguoiDung");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.DonViId).HasColumnName("donViId");
                entity.Property(e => e.HoTen).HasColumnName("hoTen");
                entity.Property(e => e.TenDangNhap).HasColumnName("tenDangNhap");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.SoDienThoai).HasColumnName("soDienThoai");
                entity.Property(e => e.MatKhauHash).HasColumnName("matKhauHash");
                entity.Property(e => e.VaiTro).HasColumnName("vaiTro");
                entity.Property(e => e.TrangThai).HasColumnName("trangThai");
                entity.Property(e => e.NgayTao).HasColumnName("ngayTao");

                entity.HasOne(e => e.DonVi)
                    .WithMany(e => e.NguoiDungs)
                    .HasForeignKey(e => e.DonViId);
            });

            modelBuilder.Entity<DongMayTinh>(entity =>
            {
                entity.ToTable("DongMayTinh");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TenDong).HasColumnName("tenDong");
                entity.Property(e => e.Hang).HasColumnName("hang");
                entity.Property(e => e.MoTa).HasColumnName("moTa");
            });

            modelBuilder.Entity<MayTinh>(entity =>
            {
                entity.ToTable("MayTinh");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.DongMayTinhId).HasColumnName("dongMayTinhId");
                entity.Property(e => e.MaTaiSan).HasColumnName("maTaiSan");
                entity.Property(e => e.SerialNumber).HasColumnName("serialNumber");
                entity.Property(e => e.LoaiMay).HasColumnName("loaiMay");
                entity.Property(e => e.Cpu).HasColumnName("cpu");
                entity.Property(e => e.Ram).HasColumnName("ram");
                entity.Property(e => e.OCung).HasColumnName("oCung");
                entity.Property(e => e.Gpu).HasColumnName("gpu");
                entity.Property(e => e.ManHinh).HasColumnName("manHinh");
                entity.Property(e => e.HeDieuHanh).HasColumnName("heDieuHanh");
                entity.Property(e => e.GiaTriMay).HasColumnName("giaTriMay");
                entity.Property(e => e.GiaThueNgay).HasColumnName("giaThueNgay");
                entity.Property(e => e.TiLeDatCoc).HasColumnName("tiLeDatCoc");
                entity.Property(e => e.TinhTrang).HasColumnName("tinhTrang");
                entity.Property(e => e.NgayNhap).HasColumnName("ngayNhap");
                entity.Property(e => e.GhiChu).HasColumnName("ghiChu");
                entity.Property(e => e.NgayTao).HasColumnName("ngayTao");

                entity.HasOne(e => e.DongMayTinh)
                    .WithMany(e => e.MayTinhs)
                    .HasForeignKey(e => e.DongMayTinhId);
            });

            modelBuilder.Entity<DonVi>(entity =>
            {
                entity.ToTable("DonVi");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TenDonVi).HasColumnName("tenDonVi");
                entity.Property(e => e.DiaChi).HasColumnName("diaChi");
                entity.Property(e => e.MaSoThue).HasColumnName("maSoThue");
                entity.Property(e => e.NguoiDaiDien).HasColumnName("nguoiDaiDien");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.SoDienThoai).HasColumnName("soDienThoai");
                entity.Property(e => e.TrangThai).HasColumnName("trangThai");
                entity.Property(e => e.NgayTao).HasColumnName("ngayTao");
            });

            modelBuilder.Entity<DonThue>(entity =>
            {
                entity.ToTable("DonThue");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.MaDonThue).HasColumnName("maDonThue");
                entity.Property(e => e.DonViId).HasColumnName("donViId");
                entity.Property(e => e.NguoiTaoId).HasColumnName("nguoiTaoId");
                entity.Property(e => e.NgayBatDau).HasColumnName("ngayBatDau");
                entity.Property(e => e.NgayKetThucDuKien).HasColumnName("ngayKetThucDuKien");
                entity.Property(e => e.NgayKetThucThucTe).HasColumnName("ngayKetThucThucTe");
                entity.Property(e => e.MucDichSuDung).HasColumnName("mucDichSuDung");
                entity.Property(e => e.TrangThai).HasColumnName("trangThai");
                entity.Property(e => e.TongTienThue).HasColumnName("tongTienThue");
                entity.Property(e => e.TongTienDatCoc).HasColumnName("tongTienDatCoc");
                entity.Property(e => e.TongTienDenBu).HasColumnName("tongTienDenBu");
                entity.Property(e => e.GhiChu).HasColumnName("ghiChu");
                entity.Property(e => e.NgayTao).HasColumnName("ngayTao");
                entity.HasOne(e => e.DonVi)
                    .WithMany(e => e.DonThues)
                    .HasForeignKey(e => e.DonViId);
            });

            modelBuilder.Entity<ChiTietDonThue>(entity =>
            {
                entity.ToTable("ChiTietDonThue");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.DonThueId).HasColumnName("donThueId");
                entity.Property(e => e.MayTinhId).HasColumnName("mayTinhId");
                entity.Property(e => e.GiaThueNgay).HasColumnName("giaThueNgay");
                entity.Property(e => e.GiaTriMayTaiThoiDiemThue).HasColumnName("giaTriMayTaiThoiDiemThue");
                entity.Property(e => e.TiLeDatCoc).HasColumnName("tiLeDatCoc");
                entity.Property(e => e.TienDatCoc).HasColumnName("tienDatCoc");
                entity.Property(e => e.SoNgayThue).HasColumnName("soNgayThue");
                entity.Property(e => e.ThanhTien).HasColumnName("thanhTien");
                entity.Property(e => e.TinhTrangBanGiao).HasColumnName("tinhTrangBanGiao");
                entity.Property(e => e.TinhTrangTraMay).HasColumnName("tinhTrangTraMay");
                entity.Property(e => e.TrangThai).HasColumnName("trangThai");

                entity.HasOne(e => e.DonThue)
                    .WithMany(e => e.ChiTietDonThues)
                    .HasForeignKey(e => e.DonThueId);

                entity.HasOne(e => e.MayTinh)
                    .WithMany(e => e.ChiTietDonThues)
                    .HasForeignKey(e => e.MayTinhId);
            });

            modelBuilder.Entity<HoaDon>(entity =>
            {
                entity.ToTable("HoaDon");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.DonThueId).HasColumnName("donThueId");
                entity.Property(e => e.MaHoaDon).HasColumnName("maHoaDon");
                entity.Property(e => e.TienThue).HasColumnName("tienThue");
                entity.Property(e => e.TienDatCoc).HasColumnName("tienDatCoc");
                entity.Property(e => e.TienDenBu).HasColumnName("tienDenBu");
                entity.Property(e => e.TongThanhToan).HasColumnName("tongThanhToan");
                entity.Property(e => e.TrangThai).HasColumnName("trangThai");
                entity.Property(e => e.NgayLap).HasColumnName("ngayLap");
            });
        }
    }
}
