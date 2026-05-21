using System;

namespace GorevTakipSistemi.Models
{
    public class DestekMesaji
    {
        public int Id { get; set; }
        public int KullaniciId { get; set; } // Hangi kullanıcı gönderdi?
        public string Konu { get; set; }
        public string Mesaj { get; set; }
        public string? Cevap { get; set; } // Adminin cevabı
        public bool IsCevaplandi { get; set; } = false;
        public DateTime Tarih { get; set; } = DateTime.Now;

        // Navigation property (Kullanıcı bilgilerine ulaşmak için)
        public virtual Kullanici Kullanici { get; set; }
    }
}