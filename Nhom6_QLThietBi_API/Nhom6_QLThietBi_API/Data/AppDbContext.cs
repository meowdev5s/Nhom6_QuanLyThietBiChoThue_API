using Microsoft.EntityFrameworkCore;

namespace Nhom6_QLThietBi_API.Data
{
    public class AppDbContext : DbContext
    {
        // Constructor nhận cấu hình (chuỗi kết nối) từ Program.cs truyền vào
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Sau này khai báo các bảng dữ liệu ở đây, ví dụ:
        // public DbSet<ThietBi> ThietBis { get; set; }
    }
}