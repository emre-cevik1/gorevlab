using System;

namespace GorevTakipSistemi.Models
{
    /// <summary>
    /// Yonetim panelinde kullanici listesini gosterirken kullanilan gorunum modeli.
    /// Kullanici bilgileri, gorev istatistikleri ve ban durumunu icerir.
    /// </summary>
    public class KullaniciYonetimViewModel
    {
        /// <summary>
        /// Kullanicinin benzersiz tanimlayicisi.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Kullanicinin ad ve soyad bilgisinin birlestirilmis hali.
        /// </summary>
        public string AdSoyad { get; set; }

        /// <summary>
        /// Kullanicinin sisteme giris icin kullandigi kullanici adi.
        /// </summary>
        public string KullaniciAdi { get; set; }

        /// <summary>
        /// Kullanicinin e-posta adresi.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Kullanicinin sistemdeki rolü.
        /// </summary>
        public KullaniciRol Rol { get; set; }

        /// <summary>
        /// Kullaniciya atanmis toplam gorev sayisi.
        /// </summary>
        public int ToplamGorevSayisi { get; set; }

        /// <summary>
        /// Kullanicinin tamamladigi gorev sayisi.
        /// </summary>
        public int TamamlananGorevSayisi { get; set; }
        
        /// <summary>
        /// Kullanicinin ban durumunu belirtir. True ise kullanici yasaklanmistir.
        /// </summary>
        public bool IsBanned { get; set; } 

        /// <summary>
        /// Kullaniciya uygulanan ban isleminin gerekce aciklamasi.
        /// </summary>
        public string? BanNedeni { get; set; }

        /// <summary>
        /// Sureli ban uygulamasinin sona erme tarih ve saat bilgisi.
        /// </summary>
        public DateTime? BanBitisTarihi { get; set; }
    }
}