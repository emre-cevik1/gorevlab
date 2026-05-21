using Microsoft.EntityFrameworkCore;
using GorevTakipSistemi.Models;

namespace GorevTakipSistemi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<Gorev> Gorevler { get; set; }
        public DbSet<DestekMesaji> DestekMesajlari { get; set; }
        public DbSet<SistemLog> SistemLoglari { get; set; }
        public DbSet<Bildirim> Bildirimler { get; set; }
        
        // YENİ EKLENEN EKİP TABLOLARI
        public DbSet<Ekip> Ekipler { get; set; }
        public DbSet<EkipUyesi> EkipUyeleri { get; set; }
        public DbSet<EkipDavet> EkipDavetleri { get; set; }
        public DbSet<GorevTamamlama> GorevTamamlamalari { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🛡️ SQL HATALARINI ÖNLEYEN SENIOR DOKUNUŞU (CASCADE KORUMALARI)
            
            // Gorev -> AtayanKullanici ilişkisi (Cascade çakışmasını önlemek için Restrict yapıyoruz)
            modelBuilder.Entity<Gorev>()
                .HasOne(g => g.AtayanKullanici)
                .WithMany()
                .HasForeignKey(g => g.AtayanKullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            // GorevTamamlama -> Kullanici ilişkisi (Cascade çakışmasını önlemek için Restrict yapıyoruz)
            modelBuilder.Entity<GorevTamamlama>()
                .HasOne(gt => gt.Kullanici)
                .WithMany()
                .HasForeignKey(gt => gt.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            // EkipDavet -> Gonderen ilişkisi
            modelBuilder.Entity<EkipDavet>()
                .HasOne(d => d.Gonderen)
                .WithMany()
                .HasForeignKey(d => d.GonderenId)
                .OnDelete(DeleteBehavior.Restrict);

            // EkipDavet -> Alici ilişkisi
            modelBuilder.Entity<EkipDavet>()
                .HasOne(d => d.Alici)
                .WithMany()
                .HasForeignKey(d => d.AliciId)
                .OnDelete(DeleteBehavior.Restrict);

            // EkipUyesi -> Kullanici ilişkisi
            modelBuilder.Entity<EkipUyesi>()
                .HasOne(e => e.Kullanici)
                .WithMany()
                .HasForeignKey(e => e.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}