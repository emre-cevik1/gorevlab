using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GorevTakipSistemi.Models
{
    /// <summary>
    /// Kullanicilara gonderilen sistem bildirimlerini temsil eden model sinifi.
    /// Her bildirim belirli bir kullaniciya aittir ve okunma durumu takip edilir.
    /// </summary>
    public class Bildirim
    {
        /// <summary>
        /// Bildirimin benzersiz tanimlayicisi.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Bildirimin gonderildigi kullanicinin benzersiz tanimlayicisi.
        /// </summary>
        public int KullaniciId { get; set; }
        
        /// <summary>
        /// Bildirimin ait oldugu kullanici nesnesi (navigasyon ozeligi).
        /// </summary>
        [ForeignKey("KullaniciId")]
        public virtual Kullanici Kullanici { get; set; }

        /// <summary>
        /// Bildirim mesajinin icerigi.
        /// </summary>
        [Required]
        public string Mesaj { get; set; }

        /// <summary>
        /// Bildirime tiklandiginda yonlendirilecek hedef sayfa adresi (ornegin: /Gorev/Details/5).
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Bildirimin kullanici tarafindan okunup okunmadigini belirtir. Varsayilan deger: okunmamis.
        /// </summary>
        public bool OkunduMu { get; set; } = false;

        /// <summary>
        /// Bildirimin olusturuldugu tarih ve saat bilgisi. Varsayilan deger: olusturulma ani.
        /// </summary>
        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    }
}
