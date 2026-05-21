using System;

namespace GorevTakipSistemi.Models
{
    public class KullaniciYonetimViewModel
    {
        public int Id { get; set; }
        public string AdSoyad { get; set; }
        public string KullaniciAdi { get; set; }
        public string Email { get; set; }
        public KullaniciRol Rol { get; set; }
        public int ToplamGorevSayisi { get; set; }
        public int TamamlananGorevSayisi { get; set; }
        
        // YENİ EKLENEN: Arayüze ban durumunu taşıyacak
        public bool IsBanned { get; set; } 
        public string? BanNedeni { get; set; }
        public DateTime? BanBitisTarihi { get; set; }
    }
}