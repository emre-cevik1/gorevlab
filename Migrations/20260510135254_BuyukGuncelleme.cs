using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GorevTakipSistemi.Migrations
{
    /// <inheritdoc />
    public partial class BuyukGuncelleme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Gorevler_KullaniciId",
                table: "Gorevler",
                column: "KullaniciId");

            migrationBuilder.AddForeignKey(
                name: "FK_Gorevler_Kullanicilar_KullaniciId",
                table: "Gorevler",
                column: "KullaniciId",
                principalTable: "Kullanicilar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Gorevler_Kullanicilar_KullaniciId",
                table: "Gorevler");

            migrationBuilder.DropIndex(
                name: "IX_Gorevler_KullaniciId",
                table: "Gorevler");
        }
    }
}
