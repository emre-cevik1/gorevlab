using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GorevTakipSistemi.Migrations
{
    /// <inheritdoc />
    public partial class AddTamamlayanKullanici : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
