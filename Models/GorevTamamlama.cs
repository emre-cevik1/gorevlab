using System;

namespace GorevTakipSistemi.Models
{
    /// <summary>
    /// Bir gorevin hangi kullanici tarafindan ne zaman tamamlandigini kaydeden model sinifi.
    /// Coklu tamamlama sistemi icin gorev-kullanici iliskisini temsil eder.
    /// </summary>
    public class GorevTamamlama
    {
        /// <summary>
        /// Tamamlama kaydinin benzersiz tanimlayicisi.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Tamamlanan gorevin benzersiz tanimlayicisi.
        /// </summary>
        public int GorevId { get; set; }

        /// <summary>
        /// Tamamlanan gorev nesnesi (navigasyon ozeligi).
        /// </summary>
        public virtual Gorev Gorev { get; set; }

        /// <summary>
        /// Gorevi tamamlayan kullanicinin benzersiz tanimlayicisi.
        /// </summary>
        public int KullaniciId { get; set; }

        /// <summary>
        /// Gorevi tamamlayan kullanici nesnesi (navigasyon ozeligi).
        /// </summary>
        public virtual Kullanici Kullanici { get; set; }

        /// <summary>
        /// Gorevin tamamlandigi tarih ve saat bilgisi. Varsayilan deger: olusturulma ani.
        /// </summary>
        public DateTime TamamlamaTarihi { get; set; } = DateTime.Now;
    }
}
