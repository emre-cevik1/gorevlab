using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GorevTakipSistemi.Migrations
{
    /// <inheritdoc />
    public partial class AddGorevTamamlamalari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Gorevler_Kullanicilar_TamamlayanKullaniciId",
                table: "Gorevler");

            migrationBuilder.DropIndex(
                name: "IX_Gorevler_TamamlayanKullaniciId",
                table: "Gorevler");

            migrationBuilder.DropColumn(
                name: "TamamlayanKullaniciId",
                table: "Gorevler");

            migrationBuilder.CreateTable(
                name: "GorevTamamlamalari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GorevId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    TamamlamaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GorevTamamlamalari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GorevTamamlamalari_Gorevler_GorevId",
                        column: x => x.GorevId,
                        principalTable: "Gorevler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GorevTamamlamalari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GorevTamamlamalari_GorevId",
                table: "GorevTamamlamalari",
                column: "GorevId");

            migrationBuilder.CreateIndex(
                name: "IX_GorevTamamlamalari_KullaniciId",
                table: "GorevTamamlamalari",
                column: "KullaniciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GorevTamamlamalari");

            migrationBuilder.AddColumn<int>(
                name: "TamamlayanKullaniciId",
                table: "Gorevler",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gorevler_TamamlayanKullaniciId",
                table: "Gorevler",
                column: "TamamlayanKullaniciId");

            migrationBuilder.AddForeignKey(
                name: "FK_Gorevler_Kullanicilar_TamamlayanKullaniciId",
                table: "Gorevler",
                column: "TamamlayanKullaniciId",
                principalTable: "Kullanicilar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
