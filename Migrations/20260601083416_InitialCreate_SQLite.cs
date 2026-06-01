using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GorevTakipSistemi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_SQLite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ad = table.Column<string>(type: "TEXT", nullable: false),
                    Soyad = table.Column<string>(type: "TEXT", nullable: false),
                    KullaniciAdi = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    SifreHash = table.Column<string>(type: "TEXT", nullable: false),
                    Rol = table.Column<int>(type: "INTEGER", nullable: false),
                    IsBanned = table.Column<bool>(type: "INTEGER", nullable: false),
                    ResetToken = table.Column<string>(type: "TEXT", nullable: true),
                    ResetTokenBitisSuresi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProfilResmi = table.Column<string>(type: "TEXT", nullable: true),
                    KvkkOnay = table.Column<bool>(type: "INTEGER", nullable: false),
                    BanBitisTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BanNedeni = table.Column<string>(type: "TEXT", nullable: true),
                    IsEmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailConfirmationToken = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SistemLoglari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KullaniciAdi = table.Column<string>(type: "TEXT", nullable: true),
                    YapilanIslem = table.Column<string>(type: "TEXT", nullable: true),
                    IpAdresi = table.Column<string>(type: "TEXT", nullable: true),
                    IslemTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SistemLoglari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bildirimler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KullaniciId = table.Column<int>(type: "INTEGER", nullable: false),
                    Mesaj = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    OkunduMu = table.Column<bool>(type: "INTEGER", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "DestekMesajlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KullaniciId = table.Column<int>(type: "INTEGER", nullable: false),
                    Konu = table.Column<string>(type: "TEXT", nullable: false),
                    Mesaj = table.Column<string>(type: "TEXT", nullable: false),
                    Cevap = table.Column<string>(type: "TEXT", nullable: true),
                    IsCevaplandi = table.Column<bool>(type: "INTEGER", nullable: false),
                    Tarih = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestekMesajlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestekMesajlari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ekipler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ad = table.Column<string>(type: "TEXT", nullable: false),
                    Aciklama = table.Column<string>(type: "TEXT", nullable: false),
                    KurulusTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    KurucuId = table.Column<int>(type: "INTEGER", nullable: false)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EkipId = table.Column<int>(type: "INTEGER", nullable: false),
                    GonderenId = table.Column<int>(type: "INTEGER", nullable: false),
                    AliciId = table.Column<int>(type: "INTEGER", nullable: false),
                    Durum = table.Column<string>(type: "TEXT", nullable: false),
                    DavetTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EkipId = table.Column<int>(type: "INTEGER", nullable: false),
                    KullaniciId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rol = table.Column<string>(type: "TEXT", nullable: false),
                    KatilmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Gorevler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GorevAdi = table.Column<string>(type: "TEXT", nullable: false),
                    Aciklama = table.Column<string>(type: "TEXT", nullable: false),
                    Oncelik = table.Column<string>(type: "TEXT", nullable: false),
                    Tarih = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DurumAktifMi = table.Column<bool>(type: "INTEGER", nullable: false),
                    KullaniciId = table.Column<int>(type: "INTEGER", nullable: false),
                    AtayanKullaniciId = table.Column<int>(type: "INTEGER", nullable: true),
                    EkipId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gorevler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gorevler_Ekipler_EkipId",
                        column: x => x.EkipId,
                        principalTable: "Ekipler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Gorevler_Kullanicilar_AtayanKullaniciId",
                        column: x => x.AtayanKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gorevler_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GorevTamamlamalari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GorevId = table.Column<int>(type: "INTEGER", nullable: false),
                    KullaniciId = table.Column<int>(type: "INTEGER", nullable: false),
                    TamamlamaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                name: "IX_Bildirimler_KullaniciId",
                table: "Bildirimler",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_DestekMesajlari_KullaniciId",
                table: "DestekMesajlari",
                column: "KullaniciId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Gorevler_AtayanKullaniciId",
                table: "Gorevler",
                column: "AtayanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Gorevler_EkipId",
                table: "Gorevler",
                column: "EkipId");

            migrationBuilder.CreateIndex(
                name: "IX_Gorevler_KullaniciId",
                table: "Gorevler",
                column: "KullaniciId");

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
                name: "Bildirimler");

            migrationBuilder.DropTable(
                name: "DestekMesajlari");

            migrationBuilder.DropTable(
                name: "EkipDavetleri");

            migrationBuilder.DropTable(
                name: "EkipUyeleri");

            migrationBuilder.DropTable(
                name: "GorevTamamlamalari");

            migrationBuilder.DropTable(
                name: "SistemLoglari");

            migrationBuilder.DropTable(
                name: "Gorevler");

            migrationBuilder.DropTable(
                name: "Ekipler");

            migrationBuilder.DropTable(
                name: "Kullanicilar");
        }
    }
}
