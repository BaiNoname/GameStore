using Microsoft.EntityFrameworkCore;

namespace GameStore.Models
{
    public class GameStoreContext : DbContext
    {
        public GameStoreContext(DbContextOptions<GameStoreContext> options) : base(options)
        {
        }

        public DbSet<Game> Games => Set<Game>();
        public DbSet<TheLoaiGame> TheLoaiGames => Set<TheLoaiGame>();
        public DbSet<NguoiDung> NguoiDungs => Set<NguoiDung>();
        public DbSet<GiaoDich> GiaoDiches => Set<GiaoDich>();
        public DbSet<DanhGia> DanhGias => Set<DanhGia>();
        public DbSet<GioHang> GioHangs => Set<GioHang>();
        public DbSet<ChiTietGioHang> ChiTietGioHangs => Set<ChiTietGioHang>();
        public DbSet<ChiTietGiaoDich> ChiTietGiaoDiches => Set<ChiTietGiaoDich>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================
            // TheLoaiGame
            // =========================
            modelBuilder.Entity<TheLoaiGame>(entity =>
            {
                entity.ToTable("theloaigame");
                entity.HasKey(e => e.MaTheLoai);

                entity.Property(e => e.MaTheLoai).HasColumnName("matheloai");
                entity.Property(e => e.TenLoaiGame).HasColumnName("tenloaigame");
            });

            // =========================
            // Game
            // =========================
            modelBuilder.Entity<Game>(entity =>
            {
                entity.ToTable("game");
                entity.HasKey(e => e.MaGame);

                entity.Property(e => e.MaGame).HasColumnName("magame");
                entity.Property(e => e.TenGame).HasColumnName("tengame");
                entity.Property(e => e.MoTa).HasColumnName("mota");
                entity.Property(e => e.MaTheLoai).HasColumnName("matheloai");
                entity.Property(e => e.Gia).HasColumnName("gia");
                entity.Property(e => e.NgayRaMat).HasColumnName("ngayramat");
                entity.Property(e => e.Hinh).HasColumnName("hinh");
                entity.Property(e => e.SoLuotTai).HasColumnName("soluottai");

                entity.HasOne(e => e.TheLoaiGame)
                      .WithMany(t => t.Games)
                      .HasForeignKey(e => e.MaTheLoai)
                      .HasConstraintName("fk_game_theloaigame");
            });

            // =========================
            // NguoiDung
            // =========================
            modelBuilder.Entity<NguoiDung>(entity =>
            {
                entity.ToTable("nguoidung");
                entity.HasKey(e => e.MaNguoiDung);

                entity.Property(e => e.MaNguoiDung).HasColumnName("manguoidung");
                entity.Property(e => e.TenNguoiDung).HasColumnName("tennguoidung");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.MatKhau).HasColumnName("matkhau");
                entity.Property(e => e.NgayDangKy).HasColumnName("ngaydangky");
                entity.Property(e => e.Quyen).HasColumnName("quyen");
                entity.Property(e => e.SoDu).HasColumnName("sodu");

                entity.HasIndex(e => e.Email).IsUnique();
            });

            // =========================
            // GiaoDich
            // =========================
            modelBuilder.Entity<GiaoDich>(entity =>
            {
                entity.ToTable("giaodich");
                entity.HasKey(e => e.MaGD);

                entity.Property(e => e.MaGD).HasColumnName("magd");
                entity.Property(e => e.MaNguoiDung).HasColumnName("manguoidung");
                entity.Property(e => e.NgayMua).HasColumnName("ngaymua");
                entity.Property(e => e.ThanhTien).HasColumnName("thanhtien");
                entity.Property(e => e.TrangThai).HasColumnName("trangthai");
                entity.Property(e => e.PhuongThuc).HasColumnName("phuongthuc");

                entity.HasOne(e => e.NguoiDung)
                      .WithMany(n => n.GiaoDiches)
                      .HasForeignKey(e => e.MaNguoiDung)
                      .HasConstraintName("fk_giaodich_nguoidung");
            });

            // =========================
            // DanhGia
            // =========================
            modelBuilder.Entity<DanhGia>(entity =>
            {
                entity.ToTable("danhgia");
                entity.HasKey(e => e.MaDG);

                entity.Property(e => e.MaDG).HasColumnName("madg");
                entity.Property(e => e.MaNguoiDung).HasColumnName("manguoidung");
                entity.Property(e => e.MaGame).HasColumnName("magame");
                entity.Property(e => e.MucDiem).HasColumnName("mucdiem");
                entity.Property(e => e.NhanXet).HasColumnName("nhanxet");

                entity.HasOne(e => e.NguoiDung)
                      .WithMany(n => n.DanhGias)
                      .HasForeignKey(e => e.MaNguoiDung)
                      .HasConstraintName("fk_danhgia_nguoidung");

                entity.HasOne(e => e.Game)
                      .WithMany(g => g.DanhGias)
                      .HasForeignKey(e => e.MaGame)
                      .HasConstraintName("fk_danhgia_game");
                entity.HasIndex(e => new { e.MaNguoiDung, e.MaGame }).IsUnique();
            });

            // =========================
            // GioHang
            // =========================
            modelBuilder.Entity<GioHang>(entity =>
            {
                entity.ToTable("giohang");
                entity.HasKey(e => e.MaGH);

                entity.Property(e => e.MaGH).HasColumnName("magh");
                entity.Property(e => e.MaNguoiDung).HasColumnName("manguoidung");

                entity.HasOne(e => e.NguoiDung)
                      .WithOne(n => n.GioHang)
                      .HasForeignKey<GioHang>(e => e.MaNguoiDung)
                      .HasConstraintName("fk_giohang_nguoidung");
                entity.HasIndex(e => e.MaNguoiDung).IsUnique();
            });

            // =========================
            // ChiTietGioHang
            // =========================
            modelBuilder.Entity<ChiTietGioHang>(entity =>
            {
                entity.ToTable("chitietgiohang");
                entity.HasKey(e => new { e.MaGH, e.MaGame });

                entity.Property(e => e.MaGH).HasColumnName("magh");
                entity.Property(e => e.MaGame).HasColumnName("magame");
                entity.Property(e => e.DonGiaHienTai).HasColumnName("dongiahientai");

                entity.HasOne(e => e.GioHang)
                      .WithMany(g => g.ChiTietGioHangs)
                      .HasForeignKey(e => e.MaGH)
                      .HasConstraintName("fk_chitietgiohang_giohang");

                entity.HasOne(e => e.Game)
                      .WithMany(g => g.ChiTietGioHangs)
                      .HasForeignKey(e => e.MaGame)
                      .HasConstraintName("fk_chitietgiohang_game");
            });

            // =========================
            // ChiTietGiaoDich
            // =========================
            modelBuilder.Entity<ChiTietGiaoDich>(entity =>
            {
                entity.ToTable("chitietgiaodich");

                entity.HasKey(e => new { e.MaGD, e.MaGame });

                entity.Property(e => e.MaGD).HasColumnName("magd");
                entity.Property(e => e.MaGame).HasColumnName("magame");
                entity.Property(e => e.DonGia).HasColumnName("dongia");

                entity.HasOne(e => e.GiaoDich)
                      .WithMany(g => g.ChiTietGiaoDiches)
                      .HasForeignKey(e => e.MaGD)
                      .HasConstraintName("fk_ctgd_giaodich");

                entity.HasOne(e => e.Game)
                      .WithMany()
                      .HasForeignKey(e => e.MaGame)
                      .HasConstraintName("fk_ctgd_game");
            });
        }
    }
}