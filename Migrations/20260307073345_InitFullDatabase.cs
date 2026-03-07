using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameStore.Migrations
{
    /// <inheritdoc />
    public partial class InitFullDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "nguoidung",
                columns: table => new
                {
                    manguoidung = table.Column<string>(type: "text", nullable: false),
                    tennguoidung = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    matkhau = table.Column<string>(type: "text", nullable: false),
                    ngaydangky = table.Column<DateOnly>(type: "date", nullable: false),
                    quyen = table.Column<string>(type: "text", nullable: false),
                    sodu = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nguoidung", x => x.manguoidung);
                });

            migrationBuilder.CreateTable(
                name: "theloaigame",
                columns: table => new
                {
                    matheloai = table.Column<string>(type: "text", nullable: false),
                    tenloaigame = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_theloaigame", x => x.matheloai);
                });

            migrationBuilder.CreateTable(
                name: "giaodich",
                columns: table => new
                {
                    magd = table.Column<string>(type: "text", nullable: false),
                    manguoidung = table.Column<string>(type: "text", nullable: false),
                    ngaymua = table.Column<DateOnly>(type: "date", nullable: false),
                    thanhtien = table.Column<decimal>(type: "numeric", nullable: false),
                    trangthai = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_giaodich", x => x.magd);
                    table.ForeignKey(
                        name: "fk_giaodich_nguoidung",
                        column: x => x.manguoidung,
                        principalTable: "nguoidung",
                        principalColumn: "manguoidung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "giohang",
                columns: table => new
                {
                    magh = table.Column<string>(type: "text", nullable: false),
                    manguoidung = table.Column<string>(type: "text", nullable: false)
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
                    magame = table.Column<string>(type: "text", nullable: false),
                    tengame = table.Column<string>(type: "text", nullable: false),
                    mota = table.Column<string>(type: "text", nullable: true),
                    matheloai = table.Column<string>(type: "text", nullable: true),
                    gia = table.Column<decimal>(type: "numeric", nullable: false),
                    ngayramat = table.Column<DateOnly>(type: "date", nullable: true),
                    hinh = table.Column<string>(type: "text", nullable: true),
                    soluottai = table.Column<int>(type: "integer", nullable: false)
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
                    magh = table.Column<string>(type: "text", nullable: false),
                    magame = table.Column<string>(type: "text", nullable: false),
                    soluong = table.Column<int>(type: "integer", nullable: false),
                    dongiahientai = table.Column<decimal>(type: "numeric", nullable: false)
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
                    madg = table.Column<string>(type: "text", nullable: false),
                    manguoidung = table.Column<string>(type: "text", nullable: false),
                    magame = table.Column<string>(type: "text", nullable: false),
                    mucdiem = table.Column<int>(type: "integer", nullable: false),
                    nhanxet = table.Column<string>(type: "text", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_chitietgiohang_magame",
                table: "chitietgiohang",
                column: "magame");

            migrationBuilder.CreateIndex(
                name: "IX_danhgia_magame",
                table: "danhgia",
                column: "magame");

            migrationBuilder.CreateIndex(
                name: "IX_danhgia_manguoidung",
                table: "danhgia",
                column: "manguoidung");

            migrationBuilder.CreateIndex(
                name: "IX_game_matheloai",
                table: "game",
                column: "matheloai");

            migrationBuilder.CreateIndex(
                name: "IX_giaodich_manguoidung",
                table: "giaodich",
                column: "manguoidung");

            migrationBuilder.CreateIndex(
                name: "IX_giohang_manguoidung",
                table: "giohang",
                column: "manguoidung");

            migrationBuilder.CreateIndex(
                name: "IX_nguoidung_email",
                table: "nguoidung",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chitietgiohang");

            migrationBuilder.DropTable(
                name: "danhgia");

            migrationBuilder.DropTable(
                name: "giaodich");

            migrationBuilder.DropTable(
                name: "giohang");

            migrationBuilder.DropTable(
                name: "game");

            migrationBuilder.DropTable(
                name: "nguoidung");

            migrationBuilder.DropTable(
                name: "theloaigame");
        }
    }
}
