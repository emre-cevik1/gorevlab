using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GorevTakipSistemi.Migrations
{
    /// <inheritdoc />
    public partial class BanSistemiV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BanBitisTarihi",
                table: "Kullanicilar",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BanNedeni",
                table: "Kullanicilar",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BanBitisTarihi",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "BanNedeni",
                table: "Kullanicilar");
        }
    }
}
