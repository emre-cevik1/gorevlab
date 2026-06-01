using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GorevTakipSistemi.Migrations
{
    /// <inheritdoc />
    public partial class AddBildirimlerVeAtamaSistemi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AtayanKullaniciId",
                table: "Gorevler",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Bildirimler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    Mesaj = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OkunduMu = table.Column<bool>(type: "bit", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bildirimler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bildirimler_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Gorevler_AtayanKullaniciId",
                table: "Gorevler",
                column: "AtayanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_KullaniciId",
                table: "Bildirimler",
                column: "KullaniciId");

            migrationBuilder.AddForeignKey(
                name: "FK_Gorevler_Kullanicilar_AtayanKullaniciId",
                table: "Gorevler",
                column: "AtayanKullaniciId",
                principalTable: "Kullanicilar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Gorevler_Kullanicilar_AtayanKullaniciId",
                table: "Gorevler");

            migrationBuilder.DropTable(
                name: "Bildirimler");

            migrationBuilder.DropIndex(
                name: "IX_Gorevler_AtayanKullaniciId",
                table: "Gorevler");

            migrationBuilder.DropColumn(
                name: "AtayanKullaniciId",
                table: "Gorevler");
        }
    }
}
