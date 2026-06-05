using Microsoft.EntityFrameworkCore;
using GorevTakipSistemi.Models;

namespace GorevTakipSistemi.Data
{
    /// <summary>
    /// Uygulamanin ana veritabani baglam sinifi.
    /// Entity Framework Core uzerinden veritabani islemlerini yonetir.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Veritabani baglam sinifinin yapici metodu.
        /// </summary>
        /// <param name="options">Veritabani yapilandirma secenekleri.</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        /// <summary>
        /// Kullanicilar tablosuna erisim saglayan DbSet ozeligi.
        /// </summary>
        public DbSet<Kullanici> Kullanicilar { get; set; }

        /// <summary>
        /// Gorevler tablosuna erisim saglayan DbSet ozeligi.
        /// </summary>
        public DbSet<Gorev> Gorevler { get; set; }

        /// <summary>
        /// Destek mesajlari tablosuna erisim saglayan DbSet ozeligi.
        /// </summary>
        public DbSet<DestekMesaji> DestekMesajlari { get; set; }

        /// <summary>
        /// Sistem loglari tablosuna erisim saglayan DbSet ozeligi.
        /// </summary>
        public DbSet<SistemLog> SistemLoglari { get; set; }

        /// <summary>
        /// Bildirimler tablosuna erisim saglayan DbSet ozeligi.
        /// </summary>
        public DbSet<Bildirim> Bildirimler { get; set; }
        
        /// <summary>
        /// Ekipler tablosuna erisim saglayan DbSet ozeligi.
        /// </summary>
        public DbSet<Ekip> Ekipler { get; set; }

        /// <summary>
        /// Ekip uyeleri tablosuna erisim saglayan DbSet ozeligi.
        /// </summary>
        public DbSet<EkipUyesi> EkipUyeleri { get; set; }

        /// <summary>
        /// Ekip davetleri tablosuna erisim saglayan DbSet ozeligi.
        /// </summary>
        public DbSet<EkipDavet> EkipDavetleri { get; set; }

        /// <summary>
        /// Gorev tamamlama kayitlari tablosuna erisim saglayan DbSet ozeligi.
        /// </summary>
        public DbSet<GorevTamamlama> GorevTamamlamalari { get; set; }

        /// <summary>
        /// Ekip aktiviteleri tablosuna erisim saglayan DbSet ozeligi.
        /// </summary>
        public DbSet<EkipAktivite> EkipAktiviteleri { get; set; }
        
        /// <summary>
        /// Alt gorevler tablosuna erisim saglayan DbSet ozeligi.
        /// </summary>
        public DbSet<AltGorev> AltGorevler { get; set; }

        /// <summary>
        /// Alt gorev tamamlama kayitlari tablosuna erisim saglayan DbSet ozeligi.
        /// </summary>
        public DbSet<AltGorevTamamlama> AltGorevTamamlamalari { get; set; }

        /// <summary>
        /// Etiketler tablosuna erisim saglayan DbSet ozeligi.
        /// </summary>
        public DbSet<Etiket> Etiketler { get; set; }

        /// <summary>
        /// Gorev-etiket iliski tablosuna erisim saglayan DbSet ozeligi.
        /// </summary>
        public DbSet<GorevEtiket> GorevEtiketleri { get; set; }

        /// <summary>
        /// Veritabani model yapilandirmasini ve tablo iliskilerini tanimlar.
        /// Cascade silme cakismalarini onlemek icin ilgili iliskiler Restrict olarak yapilandirilmistir.
        /// </summary>
        /// <param name="modelBuilder">Model yapilandirma araci.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Gorev - AtayanKullanici iliskisi: Cascade silme cakismasini onlemek icin Restrict olarak tanimlanir
            modelBuilder.Entity<Gorev>()
                .HasOne(g => g.AtayanKullanici)
                .WithMany()
                .HasForeignKey(g => g.AtayanKullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            // AltGorevTamamlama - Kullanici iliskisi: Cascade silme cakismasini onlemek icin Restrict olarak tanimlanir
            modelBuilder.Entity<AltGorevTamamlama>()
                .HasOne(t => t.Kullanici)
                .WithMany()
                .HasForeignKey(t => t.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            // GorevTamamlama - Kullanici iliskisi: Cascade silme cakismasini onlemek icin Restrict olarak tanimlanir
            modelBuilder.Entity<GorevTamamlama>()
                .HasOne(gt => gt.Kullanici)
                .WithMany()
                .HasForeignKey(gt => gt.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            // EkipAktivite - Kullanici iliskisi: Cascade silme cakismasini onlemek icin Restrict olarak tanimlanir
            modelBuilder.Entity<EkipAktivite>()
                .HasOne(e => e.Kullanici)
                .WithMany()
                .HasForeignKey(e => e.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            // EkipDavet - Gonderen iliskisi: Cascade silme cakismasini onlemek icin Restrict olarak tanimlanir
            modelBuilder.Entity<EkipDavet>()
                .HasOne(d => d.Gonderen)
                .WithMany()
                .HasForeignKey(d => d.GonderenId)
                .OnDelete(DeleteBehavior.Restrict);

            // EkipDavet - Alici iliskisi: Cascade silme cakismasini onlemek icin Restrict olarak tanimlanir
            modelBuilder.Entity<EkipDavet>()
                .HasOne(d => d.Alici)
                .WithMany()
                .HasForeignKey(d => d.AliciId)
                .OnDelete(DeleteBehavior.Restrict);

            // EkipUyesi - Kullanici iliskisi: Cascade silme cakismasini onlemek icin Restrict olarak tanimlanir
            modelBuilder.Entity<EkipUyesi>()
                .HasOne(e => e.Kullanici)
                .WithMany()
                .HasForeignKey(e => e.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}