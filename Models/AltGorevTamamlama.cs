using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GorevTakipSistemi.Models
{
    /// <summary>
    /// Bir alt gorevin hangi kullanici tarafindan ne zaman tamamlandigini kaydeden model sinifi.
    /// Alt gorev ile kullanici arasindaki tamamlama iliskisini temsil eder.
    /// </summary>
    public class AltGorevTamamlama
    {
        /// <summary>
        /// Tamamlama kaydinin benzersiz tanimlayicisi.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Tamamlanan alt gorevin benzersiz tanimlayicisi.
        /// </summary>
        [Required]
        public int AltGorevId { get; set; }

        /// <summary>
        /// Tamamlanan alt gorev nesnesi (navigasyon ozeligi).
        /// </summary>
        [ForeignKey("AltGorevId")]
        public virtual AltGorev? AltGorev { get; set; }

        /// <summary>
        /// Alt gorevi tamamlayan kullanicinin benzersiz tanimlayicisi.
        /// </summary>
        [Required]
        public int KullaniciId { get; set; }

        /// <summary>
        /// Alt gorevi tamamlayan kullanici nesnesi (navigasyon ozeligi).
        /// </summary>
        [ForeignKey("KullaniciId")]
        public virtual Kullanici? Kullanici { get; set; }

        /// <summary>
        /// Alt gorevin tamamlandigi tarih ve saat bilgisi. Varsayilan deger: olusturulma ani.
        /// </summary>
        public DateTime TamamlamaTarihi { get; set; } = DateTime.Now;
    }
}
