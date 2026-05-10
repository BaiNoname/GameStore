using Microsoft.EntityFrameworkCore;

namespace GameStore.Models
{
    public class GameStoreContext : DbContext
    {
        // DbContextOptions được truyền vào qua constructor để cấu hình kết nối database
        public GameStoreContext(DbContextOptions<GameStoreContext> options) : base(options)
        {
        }

        // Định nghĩa DbSet cho mỗi entity để EF Core biết cách ánh xạ các bảng trong database
        public DbSet<Game> Games => Set<Game>();
        public DbSet<TheLoaiGame> TheLoaiGames => Set<TheLoaiGame>();
        public DbSet<NguoiDung> NguoiDungs => Set<NguoiDung>();
        public DbSet<GiaoDich> GiaoDiches => Set<GiaoDich>();
        public DbSet<DanhGia> DanhGias => Set<DanhGia>();
        public DbSet<GioHang> GioHangs => Set<GioHang>();
        public DbSet<ChiTietGioHang> ChiTietGioHangs => Set<ChiTietGioHang>();
        public DbSet<ChiTietGiaoDich> ChiTietGiaoDiches => Set<ChiTietGiaoDich>();
        public DbSet<ThuVienGame> ThuVienGames => Set<ThuVienGame>();
        public DbSet<News> News => Set<News>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();
        public DbSet<EventMessage> EventMessages => Set<EventMessage>();
        public DbSet<EventAnnouncement> EventAnnouncements => Set<EventAnnouncement>();
        public DbSet<IconEffect> IconEffects => Set<IconEffect>();
        public DbSet<UserIconEffect> UserIconEffects => Set<UserIconEffect>();

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
                entity.Property(e => e.LinkGame).HasColumnName("linkgame");

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
                entity.Property(e => e.IsActive).HasColumnName("isactive").HasDefaultValue(true);

                // 🔥 reset password
                entity.Property(e => e.ResetCode).HasColumnName("resetcode");
                entity.Property(e => e.ResetCodeExpiry).HasColumnName("resetcodeexpiry");
                entity.Property(e => e.IsVerified)
                      .HasColumnName("isverified")
                      .HasDefaultValue(false);

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
                entity.Property(e => e.EventId).HasColumnName("eventid");
                entity.Property(e => e.NgayMua).HasColumnName("ngaymua").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.ThanhTien).HasColumnName("thanhtien");

                entity.Property(e => e.TrangThai)
                      .HasColumnName("trangthai")
                      .HasDefaultValue("Pending");

                entity.Property(e => e.PhuongThuc).HasColumnName("phuongthuc");

                entity.Property(e => e.LoaiGiaoDich)
                      .HasColumnName("loaigiaodich")
                      .HasDefaultValue("GamePurchase");

                entity.Property(e => e.CreatedAt)
                      .HasColumnName("createdat")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.VnpTransactionNo).HasColumnName("vnptransactionno");

                entity.HasIndex(e => e.MaNguoiDung);
                entity.HasIndex(e => e.TrangThai);
                entity.HasIndex(e => e.LoaiGiaoDich);
                entity.HasIndex(e => e.EventId);

                entity.HasOne(e => e.NguoiDung)
                      .WithMany(n => n.GiaoDiches)
                      .HasForeignKey(e => e.MaNguoiDung)
                      .HasConstraintName("fk_giaodich_nguoidung");
                entity.HasOne(e => e.Event)
                      .WithMany()
                      .HasForeignKey(e => e.EventId)
                      .HasConstraintName("fk_giaodich_event");
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
                entity.Property(e => e.NgayDanhGia)
                      .HasColumnName("ngaydanhgia")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

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
            // =========================
            // ThuVienGame (Library)
            // =========================
            modelBuilder.Entity<ThuVienGame>(entity =>
            {
                entity.ToTable("thuviengame");

                entity.HasKey(e => new { e.MaNguoiDung, e.MaGame });

                entity.Property(e => e.MaNguoiDung).HasColumnName("manguoidung");
                entity.Property(e => e.MaGame).HasColumnName("magame");
                entity.Property(e => e.NgayMua)
                      .HasColumnName("ngaymua")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.DaTai)
                      .HasColumnName("datai")
                      .HasDefaultValue(false);

                entity.HasOne(e => e.NguoiDung)
                      .WithMany()
                      .HasForeignKey(e => e.MaNguoiDung)
                      .HasConstraintName("fk_thuvien_nguoidung");

                entity.HasOne(e => e.Game)
                      .WithMany()
                      .HasForeignKey(e => e.MaGame)
                      .HasConstraintName("fk_thuvien_game");

                // 🔥 chống mua trùng ở DB level
                entity.HasIndex(e => new { e.MaNguoiDung, e.MaGame }).IsUnique();
            });

            // =========================
            // News
            // =========================
            modelBuilder.Entity<News>(entity =>
            {
                entity.ToTable("news");
                entity.HasKey(e => e.NewsId);

                entity.Property(e => e.NewsId).HasColumnName("newsid");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.Slug).HasColumnName("slug");
                entity.Property(e => e.Summary).HasColumnName("summary");
                entity.Property(e => e.Content).HasColumnName("content");
                entity.Property(e => e.Thumbnail).HasColumnName("thumbnail");
                entity.Property(e => e.AuthorUserId).HasColumnName("authoruserid");
                entity.Property(e => e.RelatedGameId).HasColumnName("relatedgameid");
                entity.Property(e => e.NewsType).HasColumnName("newstype");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.IsFeatured).HasColumnName("isfeatured").HasDefaultValue(false);
                entity.Property(e => e.ViewCount).HasColumnName("viewcount").HasDefaultValue(0);
                entity.Property(e => e.PublishedAt).HasColumnName("publishedat").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.ExpiredAt).HasColumnName("expiredat");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasColumnName("updatedat");

                entity.HasIndex(e => e.Slug).IsUnique();

                entity.HasOne(e => e.NguoiDung)
                      .WithMany()
                      .HasForeignKey(e => e.AuthorUserId)
                      .HasConstraintName("fk_news_nguoidung");

                entity.HasOne(e => e.Game)
                      .WithMany()
                      .HasForeignKey(e => e.RelatedGameId)
                      .HasConstraintName("fk_news_game");
            });

            // =========================
            // Event
            // =========================
            modelBuilder.Entity<Event>(entity =>
            {
                entity.ToTable("event");
                entity.HasKey(e => e.EventId);

                entity.Property(e => e.EventId).HasColumnName("eventid");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.Slug).HasColumnName("slug");
                entity.Property(e => e.Summary).HasColumnName("summary");
                entity.Property(e => e.Content).HasColumnName("content");
                entity.Property(e => e.Banner).HasColumnName("banner");
                entity.Property(e => e.RelatedGameId).HasColumnName("relatedgameid");
                entity.Property(e => e.EventType).HasColumnName("eventtype");
                entity.Property(e => e.AccessType).HasColumnName("accesstype");
                entity.Property(e => e.Price).HasColumnName("price");
                entity.Property(e => e.MaxParticipants).HasColumnName("maxparticipants");
                entity.Property(e => e.CurrentParticipants).HasColumnName("currentparticipants").HasDefaultValue(0);
                entity.Property(e => e.PrizeInfo).HasColumnName("prizeinfo");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.StartAt).HasColumnName("startat");
                entity.Property(e => e.EndAt).HasColumnName("endat");
                entity.Property(e => e.CreatedBy).HasColumnName("createdby");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasColumnName("updatedat");
                entity.Property(e => e.PrizeType).HasColumnName("prizetype");
                entity.Property(e => e.PrizeValue).HasColumnName("prizevalue");
                entity.Property(e => e.PrizeCondition).HasColumnName("prizecondition");

                entity.HasIndex(e => e.Slug).IsUnique();

                entity.HasOne(e => e.Game)
                      .WithMany()
                      .HasForeignKey(e => e.RelatedGameId)
                      .HasConstraintName("fk_event_game");

                entity.HasOne(e => e.NguoiDung)
                      .WithMany()
                      .HasForeignKey(e => e.CreatedBy)
                      .HasConstraintName("fk_event_nguoidung");
            });

            // =========================
            // EventParticipant
            // =========================
            modelBuilder.Entity<EventParticipant>(entity =>
            {
                entity.ToTable("eventparticipant");
                entity.HasKey(e => e.ParticipantId);

                entity.Property(e => e.ParticipantId).HasColumnName("participantid");
                entity.Property(e => e.EventId).HasColumnName("eventid");
                entity.Property(e => e.UserId).HasColumnName("userid");
                entity.Property(e => e.JoinStatus).HasColumnName("joinstatus").HasDefaultValue("Joined");
                entity.Property(e => e.PaidAmount).HasColumnName("paidamount");
                entity.Property(e => e.JoinedAt).HasColumnName("joinedat").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.IsCheckedIn).HasColumnName("ischeckedin").HasDefaultValue(false);
                entity.Property(e => e.CheckedInAt).HasColumnName("checkedinat");
                entity.Property(e => e.RewardGranted).HasColumnName("rewardgranted").HasDefaultValue(false);
                entity.Property(e => e.RewardGrantedAt).HasColumnName("rewardgrantedat");

                entity.HasIndex(e => new { e.EventId, e.UserId }).IsUnique();

                entity.HasOne(e => e.Event)
                      .WithMany(e => e.EventParticipants)
                      .HasForeignKey(e => e.EventId)
                      .HasConstraintName("fk_eventparticipant_event");

                entity.HasOne(e => e.NguoiDung)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .HasConstraintName("fk_eventparticipant_nguoidung");
            });

            // =========================
            // EventMessage
            // =========================
            modelBuilder.Entity<EventMessage>(entity =>
            {
                entity.ToTable("eventmessage");
                entity.HasKey(e => e.MessageId);

                entity.Property(e => e.MessageId).HasColumnName("messageid");
                entity.Property(e => e.EventId).HasColumnName("eventid");
                entity.Property(e => e.UserId).HasColumnName("userid");
                entity.Property(e => e.Content).HasColumnName("content");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.IsDeleted).HasColumnName("isdeleted").HasDefaultValue(false);

                entity.HasOne(e => e.Event)
                      .WithMany(e => e.EventMessages)
                      .HasForeignKey(e => e.EventId)
                      .HasConstraintName("fk_eventmessage_event");

                entity.HasOne(e => e.NguoiDung)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .HasConstraintName("fk_eventmessage_nguoidung");
            });

            // =========================
            // EventAnnouncement
            // =========================
            modelBuilder.Entity<EventAnnouncement>(entity =>
            {
                entity.ToTable("eventannouncement");
                entity.HasKey(e => e.AnnouncementId);

                entity.Property(e => e.AnnouncementId).HasColumnName("announcementid");
                entity.Property(e => e.EventId).HasColumnName("eventid");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.Content).HasColumnName("content");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.CreatedBy).HasColumnName("createdby");

                entity.HasOne(e => e.Event)
                      .WithMany(e => e.EventAnnouncements)
                      .HasForeignKey(e => e.EventId)
                      .HasConstraintName("fk_eventannouncement_event");

                entity.HasOne(e => e.NguoiDung)
                      .WithMany()
                      .HasForeignKey(e => e.CreatedBy)
                      .HasConstraintName("fk_eventannouncement_nguoidung");
            });

            modelBuilder.Entity<IconEffect>(entity =>
            {
                entity.ToTable("iconeffect");

                entity.HasKey(e => e.EffectId);

                entity.Property(e => e.EffectId).HasColumnName("effectid");
                entity.Property(e => e.EffectName).HasColumnName("effectname");
                entity.Property(e => e.EffectCode).HasColumnName("effectcode");
                entity.Property(e => e.EffectType).HasColumnName("effecttype");
                entity.Property(e => e.CssClass).HasColumnName("cssclass");
                entity.Property(e => e.Rarity).HasColumnName("rarity");
                entity.Property(e => e.IsActive).HasColumnName("isactive");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat");

                entity.HasIndex(e => e.EffectCode).IsUnique();
            });

            modelBuilder.Entity<UserIconEffect>(entity =>
            {
                entity.ToTable("usericoneffect");

                entity.HasKey(e => e.UserIconEffectId);

                entity.Property(e => e.UserIconEffectId).HasColumnName("usericoneffectid");
                entity.Property(e => e.MaNguoiDung).HasColumnName("manguoidung");
                entity.Property(e => e.EffectId).HasColumnName("effectid");
                entity.Property(e => e.EventId).HasColumnName("eventid");
                entity.Property(e => e.IsEquipped).HasColumnName("isequipped");
                entity.Property(e => e.GrantedAt).HasColumnName("grantedat");
                entity.Property(e => e.ExpiredAt).HasColumnName("expiredat");

                entity.HasOne(e => e.NguoiDung)
                      .WithMany(u => u.UserIconEffects)
                      .HasForeignKey(e => e.MaNguoiDung)
                      .HasConstraintName("fk_usericoneffect_user");

                entity.HasOne(e => e.IconEffect)
                      .WithMany(i => i.UserIconEffects)
                      .HasForeignKey(e => e.EffectId)
                      .HasConstraintName("fk_usericoneffect_effect");

                entity.HasOne(e => e.Event)
                      .WithMany(ev => ev.UserIconEffects)
                      .HasForeignKey(e => e.EventId)
                      .HasConstraintName("fk_usericoneffect_event");
            });
        }
    }
}