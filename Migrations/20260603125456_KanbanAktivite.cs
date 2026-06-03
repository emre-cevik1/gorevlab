using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GorevTakipSistemi.Migrations
{
    /// <inheritdoc />
    public partial class KanbanAktivite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KanbanDurumu",
                table: "Gorevler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "EkipAktiviteleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EkipId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    Aksiyon = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mesaj = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EkipAktiviteleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EkipAktiviteleri_Ekipler_EkipId",
                        column: x => x.EkipId,
                        principalTable: "Ekipler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EkipAktiviteleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EkipAktiviteleri_EkipId",
                table: "EkipAktiviteleri",
                column: "EkipId");

            migrationBuilder.CreateIndex(
                name: "IX_EkipAktiviteleri_KullaniciId",
                table: "EkipAktiviteleri",
                column: "KullaniciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EkipAktiviteleri");

            migrationBuilder.DropColumn(
                name: "KanbanDurumu",
                table: "Gorevler");
        }
    }
}
