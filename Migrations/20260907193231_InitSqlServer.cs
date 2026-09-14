using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameStore.Migrations
{
    /// <inheritdoc />
    public partial class InitSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "iconeffect",
                columns: table => new
                {
                    effectid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    effectname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    effectcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    effecttype = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    cssclass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    rarity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    isactive = table.Column<bool>(type: "bit", nullable: false),
                    createdat = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iconeffect", x => x.effectid);
                });

            migrationBuilder.CreateTable(
                name: "nguoidung",
                columns: table => new
                {
                    manguoidung = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tennguoidung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    matkhau = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ngaydangky = table.Column<DateOnly>(type: "date", nullable: false),
                    quyen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sodu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    isactive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    resetcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    resetcodeexpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    isverified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nguoidung", x => x.manguoidung);
                });

            migrationBuilder.CreateTable(
                name: "theloaigame",
                columns: table => new
                {
                    matheloai = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tenloaigame = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_theloaigame", x => x.matheloai);
                });

            migrationBuilder.CreateTable(
                name: "giohang",
                columns: table => new
                {
                    magh = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    manguoidung = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_giohang", x => x.magh);
                    table.ForeignKey(
                        name: "fk_giohang_nguoidung",
                        column: x => x.manguoidung,
                        principalTable: "nguoidung",
                        principalColumn: "manguoidung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "game",
                columns: table => new
                {
                    magame = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    tengame = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    mota = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    matheloai = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    gia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ngayramat = table.Column<DateOnly>(type: "date", nullable: true),
                    hinh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    soluottai = table.Column<int>(type: "int", nullable: false),
                    linkgame = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game", x => x.magame);
                    table.ForeignKey(
                        name: "fk_game_theloaigame",
                        column: x => x.matheloai,
                        principalTable: "theloaigame",
                        principalColumn: "matheloai");
                });

            migrationBuilder.CreateTable(
                name: "chitietgiohang",
                columns: table => new
                {
                    magh = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    magame = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    dongiahientai = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chitietgiohang", x => new { x.magh, x.magame });
                    table.ForeignKey(
                        name: "fk_chitietgiohang_game",
                        column: x => x.magame,
                        principalTable: "game",
                        principalColumn: "magame",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_chitietgiohang_giohang",
                        column: x => x.magh,
                        principalTable: "giohang",
                        principalColumn: "magh",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "danhgia",
                columns: table => new
                {
                    madg = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    manguoidung = table.Column<int>(type: "int", nullable: false),
                    magame = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    mucdiem = table.Column<int>(type: "int", nullable: false),
                    nhanxet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ngaydanhgia = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_danhgia", x => x.madg);
                    table.ForeignKey(
                        name: "fk_danhgia_game",
                        column: x => x.magame,
                        principalTable: "game",
                        principalColumn: "magame",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_danhgia_nguoidung",
                        column: x => x.manguoidung,
                        principalTable: "nguoidung",
                        principalColumn: "manguoidung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event",
                columns: table => new
                {
                    eventid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    banner = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    relatedgameid = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    eventtype = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    accesstype = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    maxparticipants = table.Column<int>(type: "int", nullable: true),
                    currentparticipants = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    prizeinfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    startat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    endat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    createdby = table.Column<int>(type: "int", nullable: false),
                    createdat = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updatedat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    prizetype = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    prizevalue = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    prizecondition = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event", x => x.eventid);
                    table.ForeignKey(
                        name: "fk_event_game",
                        column: x => x.relatedgameid,
                        principalTable: "game",
                        principalColumn: "magame");
                    table.ForeignKey(
                        name: "fk_event_nguoidung",
                        column: x => x.createdby,
                        principalTable: "nguoidung",
                        principalColumn: "manguoidung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "news",
                columns: table => new
                {
                    newsid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    thumbnail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    authoruserid = table.Column<int>(type: "int", nullable: false),
                    relatedgameid = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    newstype = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    isfeatured = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    viewcount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    publishedat = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    expiredat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdat = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updatedat = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_news", x => x.newsid);
                    table.ForeignKey(
                        name: "fk_news_game",
                        column: x => x.relatedgameid,
                        principalTable: "game",
                        principalColumn: "magame");
                    table.ForeignKey(
                        name: "fk_news_nguoidung",
                        column: x => x.authoruserid,
                        principalTable: "nguoidung",
                        principalColumn: "manguoidung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "thuviengame",
                columns: table => new
                {
                    manguoidung = table.Column<int>(type: "int", nullable: false),
                    magame = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    datai = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ngaymua = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_thuviengame", x => new { x.manguoidung, x.magame });
                    table.ForeignKey(
                        name: "fk_thuvien_game",
                        column: x => x.magame,
                        principalTable: "game",
                        principalColumn: "magame",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_thuvien_nguoidung",
                        column: x => x.manguoidung,
                        principalTable: "nguoidung",
                        principalColumn: "manguoidung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "eventannouncement",
                columns: table => new
                {
                    announcementid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eventid = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    createdat = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    createdby = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventannouncement", x => x.announcementid);
                    table.ForeignKey(
                        name: "fk_eventannouncement_event",
                        column: x => x.eventid,
                        principalTable: "event",
                        principalColumn: "eventid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_eventannouncement_nguoidung",
                        column: x => x.createdby,
                        principalTable: "nguoidung",
                        principalColumn: "manguoidung",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "eventmessage",
                columns: table => new
                {
                    messageid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eventid = table.Column<int>(type: "int", nullable: false),
                    userid = table.Column<int>(type: "int", nullable: false),
                    content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    createdat = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    isdeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventmessage", x => x.messageid);
                    table.ForeignKey(
                        name: "fk_eventmessage_event",
                        column: x => x.eventid,
                        principalTable: "event",
                        principalColumn: "eventid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_eventmessage_nguoidung",
                        column: x => x.userid,
                        principalTable: "nguoidung",
                        principalColumn: "manguoidung",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "eventparticipant",
                columns: table => new
                {
                    participantid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eventid = table.Column<int>(type: "int", nullable: false),
                    userid = table.Column<int>(type: "int", nullable: false),
                    joinstatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Joined"),
                    paidamount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    joinedat = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ischeckedin = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    checkedinat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    rewardgranted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    rewardgrantedat = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventparticipant", x => x.participantid);
                    table.ForeignKey(
                        name: "fk_eventparticipant_event",
                        column: x => x.eventid,
                        principalTable: "event",
                        principalColumn: "eventid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_eventparticipant_nguoidung",
                        column: x => x.userid,
                        principalTable: "nguoidung",
                        principalColumn: "manguoidung",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "giaodich",
                columns: table => new
                {
                    magd = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    manguoidung = table.Column<int>(type: "int", nullable: false),
                    eventid = table.Column<int>(type: "int", nullable: true),
                    ngaymua = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    thanhtien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    trangthai = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValue: "Pending"),
                    phuongthuc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    loaigiaodich = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValue: "GamePurchase"),
                    createdat = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    vnptransactionno = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_giaodich", x => x.magd);
                    table.ForeignKey(
                        name: "fk_giaodich_event",
                        column: x => x.eventid,
                        principalTable: "event",
                        principalColumn: "eventid");
                    table.ForeignKey(
                        name: "fk_giaodich_nguoidung",
                        column: x => x.manguoidung,
                        principalTable: "nguoidung",
                        principalColumn: "manguoidung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usericoneffect",
                columns: table => new
                {
                    usericoneffectid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    manguoidung = table.Column<int>(type: "int", nullable: false),
                    effectid = table.Column<int>(type: "int", nullable: false),
                    eventid = table.Column<int>(type: "int", nullable: true),
                    isequipped = table.Column<bool>(type: "bit", nullable: false),
                    grantedat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expiredat = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usericoneffect", x => x.usericoneffectid);
                    table.ForeignKey(
                        name: "fk_usericoneffect_effect",
                        column: x => x.effectid,
                        principalTable: "iconeffect",
                        principalColumn: "effectid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_usericoneffect_event",
                        column: x => x.eventid,
                        principalTable: "event",
                        principalColumn: "eventid");
                    table.ForeignKey(
                        name: "fk_usericoneffect_user",
                        column: x => x.manguoidung,
                        principalTable: "nguoidung",
                        principalColumn: "manguoidung",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chitietgiaodich",
                columns: table => new
                {
                    magd = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    magame = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    dongia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chitietgiaodich", x => new { x.magd, x.magame });
                    table.ForeignKey(
                        name: "fk_ctgd_game",
                        column: x => x.magame,
                        principalTable: "game",
                        principalColumn: "magame",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ctgd_giaodich",
                        column: x => x.magd,
                        principalTable: "giaodich",
                        principalColumn: "magd",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chitietgiaodich_magame",
                table: "chitietgiaodich",
                column: "magame");

            migrationBuilder.CreateIndex(
                name: "IX_chitietgiohang_magame",
                table: "chitietgiohang",
                column: "magame");

            migrationBuilder.CreateIndex(
                name: "IX_danhgia_magame",
                table: "danhgia",
                column: "magame");

            migrationBuilder.CreateIndex(
                name: "IX_danhgia_manguoidung_magame",
                table: "danhgia",
                columns: new[] { "manguoidung", "magame" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_event_createdby",
                table: "event",
                column: "createdby");

            migrationBuilder.CreateIndex(
                name: "IX_event_relatedgameid",
                table: "event",
                column: "relatedgameid");

            migrationBuilder.CreateIndex(
                name: "IX_event_slug",
                table: "event",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_eventannouncement_createdby",
                table: "eventannouncement",
                column: "createdby");

            migrationBuilder.CreateIndex(
                name: "IX_eventannouncement_eventid",
                table: "eventannouncement",
                column: "eventid");

            migrationBuilder.CreateIndex(
                name: "IX_eventmessage_eventid",
                table: "eventmessage",
                column: "eventid");

            migrationBuilder.CreateIndex(
                name: "IX_eventmessage_userid",
                table: "eventmessage",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_eventparticipant_eventid_userid",
                table: "eventparticipant",
                columns: new[] { "eventid", "userid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_eventparticipant_userid",
                table: "eventparticipant",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_game_matheloai",
                table: "game",
                column: "matheloai");

            migrationBuilder.CreateIndex(
                name: "IX_giaodich_eventid",
                table: "giaodich",
                column: "eventid");

            migrationBuilder.CreateIndex(
                name: "IX_giaodich_loaigiaodich",
                table: "giaodich",
                column: "loaigiaodich");

            migrationBuilder.CreateIndex(
                name: "IX_giaodich_manguoidung",
                table: "giaodich",
                column: "manguoidung");

            migrationBuilder.CreateIndex(
                name: "IX_giaodich_trangthai",
                table: "giaodich",
                column: "trangthai");

            migrationBuilder.CreateIndex(
                name: "IX_giohang_manguoidung",
                table: "giohang",
                column: "manguoidung",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iconeffect_effectcode",
                table: "iconeffect",
                column: "effectcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_news_authoruserid",
                table: "news",
                column: "authoruserid");

            migrationBuilder.CreateIndex(
                name: "IX_news_relatedgameid",
                table: "news",
                column: "relatedgameid");

            migrationBuilder.CreateIndex(
                name: "IX_news_slug",
                table: "news",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nguoidung_email",
                table: "nguoidung",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_thuviengame_magame",
                table: "thuviengame",
                column: "magame");

            migrationBuilder.CreateIndex(
                name: "IX_thuviengame_manguoidung_magame",
                table: "thuviengame",
                columns: new[] { "manguoidung", "magame" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usericoneffect_effectid",
                table: "usericoneffect",
                column: "effectid");

            migrationBuilder.CreateIndex(
                name: "IX_usericoneffect_eventid",
                table: "usericoneffect",
                column: "eventid");

            migrationBuilder.CreateIndex(
                name: "IX_usericoneffect_manguoidung",
                table: "usericoneffect",
                column: "manguoidung");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chitietgiaodich");

            migrationBuilder.DropTable(
                name: "chitietgiohang");

            migrationBuilder.DropTable(
                name: "danhgia");

            migrationBuilder.DropTable(
                name: "eventannouncement");

            migrationBuilder.DropTable(
                name: "eventmessage");

            migrationBuilder.DropTable(
                name: "eventparticipant");

            migrationBuilder.DropTable(
                name: "news");

            migrationBuilder.DropTable(
                name: "thuviengame");

            migrationBuilder.DropTable(
                name: "usericoneffect");

            migrationBuilder.DropTable(
                name: "giaodich");

            migrationBuilder.DropTable(
                name: "giohang");

            migrationBuilder.DropTable(
                name: "iconeffect");

            migrationBuilder.DropTable(
                name: "event");

            migrationBuilder.DropTable(
                name: "game");

            migrationBuilder.DropTable(
                name: "nguoidung");

            migrationBuilder.DropTable(
                name: "theloaigame");
        }
    }
}
