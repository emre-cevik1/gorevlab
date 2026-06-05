using System;

namespace GorevTakipSistemi.Models
{
    /// <summary>
    /// Sistemde gerceklestirilen islemlerin log kayitlarini tutan model sinifi.
    /// Kullanici adi, yapilan islem, IP adresi ve islem tarihi bilgilerini icerir.
    /// </summary>
    public class SistemLog
    {
        /// <summary>
        /// Log kaydinin benzersiz tanimlayicisi.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Islemi gerceklestiren kullanicinin ad ve soyad bilgisi.
        /// </summary>
        public string? KullaniciAdi { get; set; }

        /// <summary>
        /// Gerceklestirilen islemin aciklama metni (ornegin: "Kullanici yasaklandi").
        /// </summary>
        public string? YapilanIslem { get; set; }

        /// <summary>
        /// Islemin gerceklestirildigi istemcinin IP adresi.
        /// </summary>
        public string? IpAdresi { get; set; }

        /// <summary>
        /// Islemin gerceklestirildigi tarih ve saat bilgisi. Varsayilan deger: olusturulma ani.
        /// </summary>
        public DateTime IslemTarihi { get; set; } = DateTime.Now;
    }
}