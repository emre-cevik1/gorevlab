using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GorevTakipSistemi.Migrations
{
    /// <inheritdoc />
    public partial class AddAltGorevTamamlama : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AltGorevTamamlamalari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AltGorevId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    TamamlamaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AltGorevTamamlamalari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AltGorevTamamlamalari_AltGorevler_AltGorevId",
                        column: x => x.AltGorevId,
                        principalTable: "AltGorevler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AltGorevTamamlamalari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AltGorevTamamlamalari_AltGorevId",
                table: "AltGorevTamamlamalari",
                column: "AltGorevId");

            migrationBuilder.CreateIndex(
                name: "IX_AltGorevTamamlamalari_KullaniciId",
                table: "AltGorevTamamlamalari",
                column: "KullaniciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AltGorevTamamlamalari");
        }
    }
}
