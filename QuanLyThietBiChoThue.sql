/*==============================================================
  Database: QuanLyThietBiChoThue
  De tai: Ung dung ho tro quan ly may tinh cho thue tai cac don vi
  DBMS: SQL Server
==============================================================*/

IF DB_ID(N'QuanLyThietBiChoThue') IS NULL
BEGIN
    CREATE DATABASE QuanLyThietBiChoThue;
END
GO

USE QuanLyThietBiChoThue;
GO

/*==============================================================
  1. Tai khoan, don vi
==============================================================*/

CREATE TABLE DonVi (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tenDonVi NVARCHAR(150) NOT NULL,
    diaChi NVARCHAR(255) NULL,
    maSoThue VARCHAR(50) NULL,
    nguoiDaiDien NVARCHAR(100) NULL,
    email VARCHAR(100) NULL,
    soDienThoai VARCHAR(20) NULL,
    trangThai VARCHAR(30) NOT NULL DEFAULT 'hoat_dong',
    ngayTao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT CK_DonVi_TrangThai CHECK (trangThai IN ('hoat_dong', 'tam_khoa'))
);
GO

CREATE TABLE NguoiDung (
    id INT IDENTITY(1,1) PRIMARY KEY,
    donViId INT NULL,
    hoTen NVARCHAR(100) NOT NULL,
    tenDangNhap VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(100) NULL UNIQUE,
    soDienThoai VARCHAR(20) NULL,
    matKhauHash VARCHAR(255) NOT NULL,
    vaiTro VARCHAR(30) NOT NULL,
    trangThai VARCHAR(30) NOT NULL DEFAULT 'hoat_dong',
    ngayTao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_NguoiDung_DonVi FOREIGN KEY (donViId) REFERENCES DonVi(id),
    CONSTRAINT CK_NguoiDung_VaiTro CHECK (vaiTro IN ('admin', 'nhan_vien', 'khach_hang')),
    CONSTRAINT CK_NguoiDung_TrangThai CHECK (trangThai IN ('hoat_dong', 'tam_khoa'))
);
GO

/*==============================================================
  2. Danh muc dong may va tung may tinh cu the
==============================================================*/

CREATE TABLE DongMayTinh (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tenDong NVARCHAR(100) NOT NULL,
    hang NVARCHAR(80) NOT NULL,
    moTa NVARCHAR(MAX) NULL,
    CONSTRAINT UQ_DongMayTinh UNIQUE (tenDong, hang)
);
GO

CREATE TABLE MayTinh (
    id INT IDENTITY(1,1) PRIMARY KEY,
    dongMayTinhId INT NOT NULL,
    maTaiSan VARCHAR(50) NOT NULL UNIQUE,
    serialNumber VARCHAR(100) NULL UNIQUE,
    loaiMay NVARCHAR(50) NOT NULL,
    cpu NVARCHAR(100) NULL,
    ram NVARCHAR(50) NULL,
    oCung NVARCHAR(100) NULL,
    gpu NVARCHAR(100) NULL,
    manHinh NVARCHAR(100) NULL,
    heDieuHanh NVARCHAR(100) NULL,
    giaTriMay DECIMAL(18,2) NOT NULL,
    giaThueNgay DECIMAL(18,2) NOT NULL,
    tiLeDatCoc DECIMAL(5,2) NOT NULL,
    tinhTrang VARCHAR(50) NOT NULL DEFAULT 'san_sang',
    ngayNhap DATE NULL,
    ghiChu NVARCHAR(MAX) NULL,
    ngayTao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_MayTinh_DongMayTinh FOREIGN KEY (dongMayTinhId) REFERENCES DongMayTinh(id),
    CONSTRAINT CK_MayTinh_GiaTri CHECK (giaTriMay >= 0),
    CONSTRAINT CK_MayTinh_GiaThue CHECK (giaThueNgay >= 0),
    CONSTRAINT CK_MayTinh_TiLeDatCoc CHECK (tiLeDatCoc >= 0 AND tiLeDatCoc <= 100),
    CONSTRAINT CK_MayTinh_TinhTrang CHECK (
        tinhTrang IN ('san_sang', 'dang_thue', 'bao_tri', 'hong', 'ngung_kinh_doanh')
    )
);
GO

CREATE TABLE AnhMayTinh (
    id INT IDENTITY(1,1) PRIMARY KEY,
    mayTinhId INT NOT NULL,
    duongDanAnh VARCHAR(255) NOT NULL,
    laAnhDaiDien BIT NOT NULL DEFAULT 0,
    ngayTao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_AnhMayTinh_MayTinh FOREIGN KEY (mayTinhId) REFERENCES MayTinh(id)
);
GO

CREATE TABLE BaoTriMayTinh (
    id INT IDENTITY(1,1) PRIMARY KEY,
    mayTinhId INT NOT NULL,
    ngayBatDau DATE NOT NULL,
    ngayKetThuc DATE NULL,
    noiDung NVARCHAR(MAX) NOT NULL,
    chiPhi DECIMAL(18,2) NOT NULL DEFAULT 0,
    trangThai VARCHAR(30) NOT NULL DEFAULT 'dang_bao_tri',
    CONSTRAINT FK_BaoTriMayTinh_MayTinh FOREIGN KEY (mayTinhId) REFERENCES MayTinh(id),
    CONSTRAINT CK_BaoTriMayTinh_TrangThai CHECK (trangThai IN ('dang_bao_tri', 'hoan_thanh', 'huy')),
    CONSTRAINT CK_BaoTriMayTinh_ChiPhi CHECK (chiPhi >= 0)
);
GO

/*==============================================================
  3. Don thue, chi tiet thue, hop dong
==============================================================*/

CREATE TABLE DonThue (
    id INT IDENTITY(1,1) PRIMARY KEY,
    maDonThue VARCHAR(50) NOT NULL UNIQUE,
    donViId INT NOT NULL,
    nguoiTaoId INT NOT NULL,
    ngayBatDau DATE NOT NULL,
    ngayKetThucDuKien DATE NOT NULL,
    ngayKetThucThucTe DATE NULL,
    mucDichSuDung NVARCHAR(MAX) NULL,
    trangThai VARCHAR(50) NOT NULL DEFAULT 'cho_duyet',
    tongTienThue DECIMAL(18,2) NOT NULL DEFAULT 0,
    tongTienDatCoc DECIMAL(18,2) NOT NULL DEFAULT 0,
    tongTienDenBu DECIMAL(18,2) NOT NULL DEFAULT 0,
    ghiChu NVARCHAR(MAX) NULL,
    ngayTao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_DonThue_DonVi FOREIGN KEY (donViId) REFERENCES DonVi(id),
    CONSTRAINT FK_DonThue_NguoiTao FOREIGN KEY (nguoiTaoId) REFERENCES NguoiDung(id),
    CONSTRAINT CK_DonThue_Ngay CHECK (ngayKetThucDuKien >= ngayBatDau),
    CONSTRAINT CK_DonThue_Tien CHECK (tongTienThue >= 0 AND tongTienDatCoc >= 0 AND tongTienDenBu >= 0),
    CONSTRAINT CK_DonThue_TrangThai CHECK (
        trangThai IN ('cho_duyet', 'da_duyet', 'dang_thue', 'yeu_cau_tra', 'hoan_thanh', 'qua_han', 'huy', 'tu_choi')
    )
);
GO

CREATE TABLE ChiTietDonThue (
    id INT IDENTITY(1,1) PRIMARY KEY,
    donThueId INT NOT NULL,
    mayTinhId INT NOT NULL,
    giaThueNgay DECIMAL(18,2) NOT NULL,
    giaTriMayTaiThoiDiemThue DECIMAL(18,2) NOT NULL,
    tiLeDatCoc DECIMAL(5,2) NOT NULL,
    tienDatCoc DECIMAL(18,2) NOT NULL,
    soNgayThue INT NOT NULL,
    thanhTien DECIMAL(18,2) NOT NULL,
    tinhTrangBanGiao NVARCHAR(MAX) NULL,
    tinhTrangTraMay NVARCHAR(MAX) NULL,
    trangThai VARCHAR(50) NOT NULL DEFAULT 'dang_xu_ly',
    CONSTRAINT FK_ChiTietDonThue_DonThue FOREIGN KEY (donThueId) REFERENCES DonThue(id),
    CONSTRAINT FK_ChiTietDonThue_MayTinh FOREIGN KEY (mayTinhId) REFERENCES MayTinh(id),
    CONSTRAINT UQ_ChiTietDonThue_Don_May UNIQUE (donThueId, mayTinhId),
    CONSTRAINT CK_ChiTietDonThue_Tien CHECK (
        giaThueNgay >= 0 AND giaTriMayTaiThoiDiemThue >= 0 AND
        tiLeDatCoc >= 0 AND tiLeDatCoc <= 100 AND tienDatCoc >= 0 AND
        soNgayThue > 0 AND thanhTien >= 0
    ),
    CONSTRAINT CK_ChiTietDonThue_TrangThai CHECK (
        trangThai IN ('dang_xu_ly', 'dang_thue', 'da_tra', 'huy')
    )
);
GO

CREATE TABLE HopDong (
    id INT IDENTITY(1,1) PRIMARY KEY,
    donThueId INT NOT NULL UNIQUE,
    maHopDong VARCHAR(50) NOT NULL UNIQUE,
    ngayLap DATE NOT NULL,
    noiDung NVARCHAR(MAX) NULL,
    fileUrl VARCHAR(255) NULL,
    trangThai VARCHAR(30) NOT NULL DEFAULT 'hieu_luc',
    CONSTRAINT FK_HopDong_DonThue FOREIGN KEY (donThueId) REFERENCES DonThue(id),
    CONSTRAINT CK_HopDong_TrangThai CHECK (trangThai IN ('hieu_luc', 'het_hieu_luc', 'huy'))
);
GO

CREATE TABLE YeuCauGiaHan (
    id INT IDENTITY(1,1) PRIMARY KEY,
    donThueId INT NOT NULL,
    ngayKetThucMoi DATE NOT NULL,
    lyDo NVARCHAR(MAX) NULL,
    trangThai VARCHAR(30) NOT NULL DEFAULT 'cho_duyet',
    ngayTao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    nguoiDuyetId INT NULL,
    ngayDuyet DATETIME2 NULL,
    CONSTRAINT FK_YeuCauGiaHan_DonThue FOREIGN KEY (donThueId) REFERENCES DonThue(id),
    CONSTRAINT FK_YeuCauGiaHan_NguoiDuyet FOREIGN KEY (nguoiDuyetId) REFERENCES NguoiDung(id),
    CONSTRAINT CK_YeuCauGiaHan_TrangThai CHECK (trangThai IN ('cho_duyet', 'da_duyet', 'tu_choi', 'huy'))
);
GO

CREATE TABLE YeuCauTraMay (
    id INT IDENTITY(1,1) PRIMARY KEY,
    donThueId INT NOT NULL,
    ngayYeuCau DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    lyDo NVARCHAR(MAX) NULL,
    ghiChu NVARCHAR(MAX) NULL,
    trangThai VARCHAR(30) NOT NULL DEFAULT 'cho_xu_ly',
    nguoiXuLyId INT NULL,
    ngayXuLy DATETIME2 NULL,
    CONSTRAINT FK_YeuCauTraMay_DonThue FOREIGN KEY (donThueId) REFERENCES DonThue(id),
    CONSTRAINT FK_YeuCauTraMay_NguoiXuLy FOREIGN KEY (nguoiXuLyId) REFERENCES NguoiDung(id),
    CONSTRAINT CK_YeuCauTraMay_TrangThai CHECK (trangThai IN ('cho_xu_ly', 'da_nhan_may', 'tu_choi', 'huy'))
);
GO

/*==============================================================
  4. Hu hong, den bu, hoa don, thanh toan
==============================================================*/

CREATE TABLE MucDoHuHong (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tenMucDo NVARCHAR(100) NOT NULL,
    tiLeDenBu DECIMAL(5,2) NOT NULL,
    moTa NVARCHAR(MAX) NULL,
    CONSTRAINT CK_MucDoHuHong_TiLe CHECK (tiLeDenBu >= 0 AND tiLeDenBu <= 100)
);
GO

CREATE TABLE BaoCaoHuHong (
    id INT IDENTITY(1,1) PRIMARY KEY,
    chiTietDonThueId INT NOT NULL,
    nguoiBaoCaoId INT NOT NULL,
    moTa NVARCHAR(MAX) NOT NULL,
    hinhAnhUrl VARCHAR(255) NULL,
    mucDoHuHongId INT NULL,
    tienDenBu DECIMAL(18,2) NOT NULL DEFAULT 0,
    trangThai VARCHAR(30) NOT NULL DEFAULT 'cho_xu_ly',
    ngayBaoCao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_BaoCaoHuHong_ChiTietDonThue FOREIGN KEY (chiTietDonThueId) REFERENCES ChiTietDonThue(id),
    CONSTRAINT FK_BaoCaoHuHong_NguoiBaoCao FOREIGN KEY (nguoiBaoCaoId) REFERENCES NguoiDung(id),
    CONSTRAINT FK_BaoCaoHuHong_MucDo FOREIGN KEY (mucDoHuHongId) REFERENCES MucDoHuHong(id),
    CONSTRAINT CK_BaoCaoHuHong_Tien CHECK (tienDenBu >= 0),
    CONSTRAINT CK_BaoCaoHuHong_TrangThai CHECK (trangThai IN ('cho_xu_ly', 'da_xac_nhan', 'da_tu_choi', 'da_thanh_toan'))
);
GO

CREATE TABLE HoaDon (
    id INT IDENTITY(1,1) PRIMARY KEY,
    donThueId INT NOT NULL,
    maHoaDon VARCHAR(50) NOT NULL UNIQUE,
    tienThue DECIMAL(18,2) NOT NULL DEFAULT 0,
    tienDatCoc DECIMAL(18,2) NOT NULL DEFAULT 0,
    tienDenBu DECIMAL(18,2) NOT NULL DEFAULT 0,
    tongThanhToan DECIMAL(18,2) NOT NULL DEFAULT 0,
    trangThai VARCHAR(30) NOT NULL DEFAULT 'chua_thanh_toan',
    ngayLap DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_HoaDon_DonThue FOREIGN KEY (donThueId) REFERENCES DonThue(id),
    CONSTRAINT CK_HoaDon_Tien CHECK (
        tienThue >= 0 AND tienDatCoc >= 0 AND tienDenBu >= 0 AND tongThanhToan >= 0
    ),
    CONSTRAINT CK_HoaDon_TrangThai CHECK (trangThai IN ('chua_thanh_toan', 'da_thanh_toan', 'thanh_toan_mot_phan', 'huy'))
);
GO

CREATE TABLE ThanhToan (
    id INT IDENTITY(1,1) PRIMARY KEY,
    hoaDonId INT NOT NULL,
    soTien DECIMAL(18,2) NOT NULL,
    phuongThuc VARCHAR(30) NOT NULL,
    maGiaoDich VARCHAR(100) NULL,
    ghiChu NVARCHAR(MAX) NULL,
    ngayThanhToan DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_ThanhToan_HoaDon FOREIGN KEY (hoaDonId) REFERENCES HoaDon(id),
    CONSTRAINT CK_ThanhToan_SoTien CHECK (soTien > 0),
    CONSTRAINT CK_ThanhToan_PhuongThuc CHECK (phuongThuc IN ('tien_mat', 'chuyen_khoan', 'vi_dien_tu'))
);
GO

/*==============================================================
  5. Chat ho tro qua API
==============================================================*/

CREATE TABLE CuocTroChuyen (
    id INT IDENTITY(1,1) PRIMARY KEY,
    donViId INT NULL,
    khachHangId INT NOT NULL,
    nhanVienPhuTrachId INT NULL,
    tieuDe NVARCHAR(150) NULL,
    trangThai VARCHAR(30) NOT NULL DEFAULT 'dang_mo',
    ngayTao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ngayCapNhat DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_CuocTroChuyen_DonVi FOREIGN KEY (donViId) REFERENCES DonVi(id),
    CONSTRAINT FK_CuocTroChuyen_KhachHang FOREIGN KEY (khachHangId) REFERENCES NguoiDung(id),
    CONSTRAINT FK_CuocTroChuyen_NhanVien FOREIGN KEY (nhanVienPhuTrachId) REFERENCES NguoiDung(id),
    CONSTRAINT CK_CuocTroChuyen_TrangThai CHECK (trangThai IN ('dang_mo', 'dang_xu_ly', 'da_dong'))
);
GO

CREATE TABLE TinNhan (
    id INT IDENTITY(1,1) PRIMARY KEY,
    cuocTroChuyenId INT NOT NULL,
    nguoiGuiId INT NOT NULL,
    noiDung NVARCHAR(MAX) NOT NULL,
    loaiTinNhan VARCHAR(30) NOT NULL DEFAULT 'text',
    daDoc BIT NOT NULL DEFAULT 0,
    ngayGui DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_TinNhan_CuocTroChuyen FOREIGN KEY (cuocTroChuyenId) REFERENCES CuocTroChuyen(id),
    CONSTRAINT FK_TinNhan_NguoiGui FOREIGN KEY (nguoiGuiId) REFERENCES NguoiDung(id),
    CONSTRAINT CK_TinNhan_Loai CHECK (loaiTinNhan IN ('text', 'image', 'system'))
);
GO

/*==============================================================
  6. Index goi y cho API
==============================================================*/

CREATE INDEX IX_NguoiDung_DonVi ON NguoiDung(donViId);
CREATE INDEX IX_MayTinh_DongMayTinh ON MayTinh(dongMayTinhId);
CREATE INDEX IX_MayTinh_TinhTrang ON MayTinh(tinhTrang);
CREATE INDEX IX_DonThue_DonVi ON DonThue(donViId);
CREATE INDEX IX_DonThue_TrangThai ON DonThue(trangThai);
CREATE INDEX IX_DonThue_NgayThue ON DonThue(ngayBatDau, ngayKetThucDuKien);
CREATE INDEX IX_ChiTietDonThue_MayTinh ON ChiTietDonThue(mayTinhId);
CREATE INDEX IX_CuocTroChuyen_KhachHang ON CuocTroChuyen(khachHangId);
CREATE INDEX IX_CuocTroChuyen_TrangThai ON CuocTroChuyen(trangThai);
CREATE INDEX IX_TinNhan_CuocTroChuyen_NgayGui ON TinNhan(cuocTroChuyenId, ngayGui);
GO

/*==============================================================
  7. Du lieu mau de demo
  Ghi chu: matKhauHash dang de gia lap, API thuc te nen hash bang BCrypt.
==============================================================*/

INSERT INTO DonVi (tenDonVi, diaChi, maSoThue, nguoiDaiDien, email, soDienThoai)
VALUES
(N'Công ty ABC', N'Quận 1, TP. Hồ Chí Minh', '0312345678', N'Nguyễn Văn An', 'abc@example.com', '0901000001'),
(N'Công ty FPT', N'Quận 7, TP. Hồ Chí Minh', '0398765432', N'Trần Thị Bình', 'fpt@example.com', '0902000002');
GO

INSERT INTO NguoiDung (donViId, hoTen, tenDangNhap, email, soDienThoai, matKhauHash, vaiTro)
VALUES
(NULL, N'Quản trị hệ thống', 'admin', 'admin@example.com', '0900000000', '123456_demo_hash', 'admin'),
(NULL, N'Nhân viên hỗ trợ', 'nhanvien01', 'support@example.com', '0900000001', '123456_demo_hash', 'nhan_vien'),
(1, N'Nguyễn Văn An', 'khach01', 'an@abc.example.com', '0901000001', '123456_demo_hash', 'khach_hang'),
(2, N'Trần Thị Bình', 'khach02', 'binh@fpt.example.com', '0902000002', '123456_demo_hash', 'khach_hang');
GO

INSERT INTO DongMayTinh (tenDong, hang, moTa)
VALUES
(N'ThinkPad T14', N'Lenovo', N'Dòng laptop doanh nghiệp bền, phù hợp văn phòng.'),
(N'MacBook Pro', N'Apple', N'Dòng laptop hiệu năng cao cho thiết kế và lập trình.'),
(N'Dell XPS 13', N'Dell', N'Dòng laptop mỏng nhẹ cao cấp.'),
(N'Mac mini', N'Apple', N'Máy tính mini để bàn.');
GO

INSERT INTO MayTinh (
    dongMayTinhId, maTaiSan, serialNumber, loaiMay, cpu, ram, oCung, gpu, manHinh,
    heDieuHanh, giaTriMay, giaThueNgay, tiLeDatCoc, tinhTrang, ngayNhap, ghiChu
)
VALUES
(1, 'LT-LEN-T14-001', 'SN-T14-001', N'laptop', N'Intel Core i5-1240P', N'8GB', N'256GB SSD', N'Intel Iris Xe', N'14 inch FHD', N'Windows 11 Pro', 18000000, 100000, 30, 'san_sang', '2026-01-10', NULL),
(1, 'LT-LEN-T14-002', 'SN-T14-002', N'laptop', N'Intel Core i7-1260P', N'16GB', N'512GB SSD', N'Intel Iris Xe', N'14 inch FHD', N'Windows 11 Pro', 24000000, 150000, 30, 'san_sang', '2026-01-10', NULL),
(2, 'LT-APP-MBP-001', 'SN-MBP-001', N'laptop', N'Apple M2', N'8GB', N'256GB SSD', N'8-core GPU', N'13.3 inch Retina', N'macOS', 30000000, 250000, 35, 'san_sang', '2026-02-01', NULL),
(3, 'LT-DELL-XPS-001', 'SN-XPS-001', N'laptop', N'Intel Core i7-1250U', N'16GB', N'512GB SSD', N'Intel Iris Xe', N'13.4 inch OLED', N'Windows 11 Pro', 28000000, 180000, 30, 'bao_tri', '2026-02-15', N'Đang kiểm tra pin.'),
(4, 'PC-APP-MM2-001', 'SN-MM2-001', N'mini PC', N'Apple M2', N'16GB', N'512GB SSD', N'10-core GPU', N'Không kèm màn hình', N'macOS', 22000000, 170000, 30, 'san_sang', '2026-03-05', NULL);
GO

INSERT INTO MucDoHuHong (tenMucDo, tiLeDenBu, moTa)
VALUES
(N'Hư hỏng nhẹ', 10, N'Trầy xước, lỗi nhỏ, vẫn sử dụng được.'),
(N'Hư hỏng vừa', 30, N'Lỗi linh kiện cần sửa chữa hoặc thay thế một phần.'),
(N'Hư hỏng nặng', 70, N'Máy không hoạt động bình thường, chi phí sửa chữa cao.'),
(N'Mất máy', 100, N'Khách làm mất thiết bị, bồi thường toàn bộ giá trị máy.');
GO

INSERT INTO DonThue (
    maDonThue, donViId, nguoiTaoId, ngayBatDau, ngayKetThucDuKien,
    mucDichSuDung, trangThai, tongTienThue, tongTienDatCoc
)
VALUES
('DT-20260521-001', 1, 3, '2026-05-25', '2026-05-30', N'Thuê máy cho nhân viên dự án ngắn hạn.', 'cho_duyet', 2000000, 17700000);
GO

INSERT INTO ChiTietDonThue (
    donThueId, mayTinhId, giaThueNgay, giaTriMayTaiThoiDiemThue,
    tiLeDatCoc, tienDatCoc, soNgayThue, thanhTien, tinhTrangBanGiao
)
VALUES
(1, 2, 150000, 24000000, 30, 7200000, 5, 750000, N'Máy hoạt động tốt, kèm sạc.'),
(1, 3, 250000, 30000000, 35, 10500000, 5, 1250000, N'Máy hoạt động tốt, kèm sạc.');
GO

INSERT INTO HoaDon (donThueId, maHoaDon, tienThue, tienDatCoc, tienDenBu, tongThanhToan)
VALUES
(1, 'HD-20260521-001', 2000000, 17700000, 0, 19700000);
GO

INSERT INTO CuocTroChuyen (donViId, khachHangId, nhanVienPhuTrachId, tieuDe, trangThai)
VALUES
(1, 3, 2, N'Hỗ trợ thuê máy ThinkPad', 'dang_xu_ly');
GO

INSERT INTO TinNhan (cuocTroChuyenId, nguoiGuiId, noiDung, daDoc)
VALUES
(1, 3, N'Chào anh/chị, bên em muốn thuê thêm 2 máy laptop cho dự án tuần sau.', 1),
(1, 2, N'Em đã nhận thông tin. Anh/chị cho em biết cấu hình mong muốn và thời gian thuê dự kiến nhé.', 0);
GO

/*==============================================================
  8. Truy van kiem tra nhanh
==============================================================*/

SELECT
    mt.id,
    mt.maTaiSan,
    dmt.hang,
    dmt.tenDong,
    mt.loaiMay,
    mt.cpu,
    mt.ram,
    mt.giaThueNgay,
    mt.tiLeDatCoc,
    mt.tinhTrang
FROM MayTinh mt
JOIN DongMayTinh dmt ON dmt.id = mt.dongMayTinhId
ORDER BY mt.id;
GO
