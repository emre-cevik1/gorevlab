using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GorevTakipSistemi.Migrations
{
    /// <inheritdoc />
    public partial class AltGorevVeEtiketler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AltGorevler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GorevId = table.Column<int>(type: "int", nullable: false),
                    Baslik = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TamamlandiMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AltGorevler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AltGorevler_Gorevler_GorevId",
                        column: x => x.GorevId,
                        principalTable: "Gorevler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Etiketler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RenkHex = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    EkipId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etiketler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Etiketler_Ekipler_EkipId",
                        column: x => x.EkipId,
                        principalTable: "Ekipler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GorevEtiketleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GorevId = table.Column<int>(type: "int", nullable: false),
                    EtiketId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GorevEtiketleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GorevEtiketleri_Etiketler_EtiketId",
                        column: x => x.EtiketId,
                        principalTable: "Etiketler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GorevEtiketleri_Gorevler_GorevId",
                        column: x => x.GorevId,
                        principalTable: "Gorevler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AltGorevler_GorevId",
                table: "AltGorevler",
                column: "GorevId");

            migrationBuilder.CreateIndex(
                name: "IX_Etiketler_EkipId",
                table: "Etiketler",
                column: "EkipId");

            migrationBuilder.CreateIndex(
                name: "IX_GorevEtiketleri_EtiketId",
                table: "GorevEtiketleri",
                column: "EtiketId");

            migrationBuilder.CreateIndex(
                name: "IX_GorevEtiketleri_GorevId",
                table: "GorevEtiketleri",
                column: "GorevId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AltGorevler");

            migrationBuilder.DropTable(
                name: "GorevEtiketleri");

            migrationBuilder.DropTable(
                name: "Etiketler");
        }
    }
}
