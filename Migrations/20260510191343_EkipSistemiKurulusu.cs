using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GorevTakipSistemi.Migrations
{
    /// <inheritdoc />
    public partial class EkipSistemiKurulusu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EkipId",
                table: "Gorevler",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Ekipler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KurulusTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KurucuId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ekipler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ekipler_Kullanicilar_KurucuId",
                        column: x => x.KurucuId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EkipDavetleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EkipId = table.Column<int>(type: "int", nullable: false),
                    GonderenId = table.Column<int>(type: "int", nullable: false),
                    AliciId = table.Column<int>(type: "int", nullable: false),
                    Durum = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DavetTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EkipDavetleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EkipDavetleri_Ekipler_EkipId",
                        column: x => x.EkipId,
                        principalTable: "Ekipler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EkipDavetleri_Kullanicilar_AliciId",
                        column: x => x.AliciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EkipDavetleri_Kullanicilar_GonderenId",
                        column: x => x.GonderenId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EkipUyeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EkipId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KatilmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EkipUyeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EkipUyeleri_Ekipler_EkipId",
                        column: x => x.EkipId,
                        principalTable: "Ekipler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EkipUyeleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Gorevler_EkipId",
                table: "Gorevler",
                column: "EkipId");

            migrationBuilder.CreateIndex(
                name: "IX_EkipDavetleri_AliciId",
                table: "EkipDavetleri",
                column: "AliciId");

            migrationBuilder.CreateIndex(
                name: "IX_EkipDavetleri_EkipId",
                table: "EkipDavetleri",
                column: "EkipId");

            migrationBuilder.CreateIndex(
                name: "IX_EkipDavetleri_GonderenId",
                table: "EkipDavetleri",
                column: "GonderenId");

            migrationBuilder.CreateIndex(
                name: "IX_Ekipler_KurucuId",
                table: "Ekipler",
                column: "KurucuId");

            migrationBuilder.CreateIndex(
                name: "IX_EkipUyeleri_EkipId",
                table: "EkipUyeleri",
                column: "EkipId");

            migrationBuilder.CreateIndex(
                name: "IX_EkipUyeleri_KullaniciId",
                table: "EkipUyeleri",
                column: "KullaniciId");

            migrationBuilder.AddForeignKey(
                name: "FK_Gorevler_Ekipler_EkipId",
                table: "Gorevler",
                column: "EkipId",
                principalTable: "Ekipler",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Gorevler_Ekipler_EkipId",
                table: "Gorevler");

            migrationBuilder.DropTable(
                name: "EkipDavetleri");

            migrationBuilder.DropTable(
                name: "EkipUyeleri");

            migrationBuilder.DropTable(
                name: "Ekipler");

            migrationBuilder.DropIndex(
                name: "IX_Gorevler_EkipId",
                table: "Gorevler");

            migrationBuilder.DropColumn(
                name: "EkipId",
                table: "Gorevler");
        }
    }
}
