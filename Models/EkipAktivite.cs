using System;
using System.ComponentModel.DataAnnotations;

namespace GorevTakipSistemi.Models
{
    /// <summary>
    /// Ekip icerisinde gerceklestirilen aktivitelerin kayitlarini tutan model sinifi.
    /// Gorev olusturma, tamamlama ve durum degisikligi gibi islemleri loglar.
    /// </summary>
    public class EkipAktivite
    {
        /// <summary>
        /// Aktivite kaydinin benzersiz tanimlayicisi.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Aktivitenin ait oldugu ekibin benzersiz tanimlayicisi.
        /// </summary>
        public int EkipId { get; set; }

        /// <summary>
        /// Aktivitenin ait oldugu ekip nesnesi (navigasyon ozeligi).
        /// </summary>
        public virtual Ekip Ekip { get; set; }

        /// <summary>
        /// Aktiviteyi gerceklestiren kullanicinin benzersiz tanimlayicisi.
        /// </summary>
        public int KullaniciId { get; set; }

        /// <summary>
        /// Aktiviteyi gerceklestiren kullanici nesnesi (navigasyon ozeligi).
        /// </summary>
        public virtual Kullanici Kullanici { get; set; }

        /// <summary>
        /// Gerceklestirilen aksiyonun turu (ornegin: "Olusturdu", "Tamamladi", "Durum Degistirdi").
        /// </summary>
        [Required]
        public string Aksiyon { get; set; }

        /// <summary>
        /// Aktiviteyi detayli olarak aciklayan mesaj metni (ornegin: "Frontend arayuzu gorevini 'Yapiliyor' sutununa tasidi.").
        /// </summary>
        public string Mesaj { get; set; }

        /// <summary>
        /// Aktivitenin gerceklestirildigi tarih ve saat bilgisi. Varsayilan deger: olusturulma ani.
        /// </summary>
        public DateTime Tarih { get; set; } = DateTime.Now;
    }
}
