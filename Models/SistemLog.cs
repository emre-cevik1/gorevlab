using System;

namespace GorevTakipSistemi.Models
{
    public class SistemLog
    {
        public int Id { get; set; }
        public string? KullaniciAdi { get; set; } // İşlemi yapan kişi
        public string? YapilanIslem { get; set; } // Ne yaptı? (Örn: Ban attı)
        public string? IpAdresi { get; set; } // Hangi IP'den yaptı?
        public DateTime IslemTarihi { get; set; } = DateTime.Now; // Ne zaman yaptı?
    }
}