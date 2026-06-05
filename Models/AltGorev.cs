using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GorevTakipSistemi.Models
{
    /// <summary>
    /// Bir goreve bagli alt gorev (kontrol listesi ogesi) bilgilerini tutan model sinifi.
    /// Her alt gorev bir ana goreve aittir ve bagimsiz olarak tamamlanabilir.
    /// </summary>
    public class AltGorev
    {
        /// <summary>
        /// Alt gorevin benzersiz tanimlayicisi.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Alt gorevin bagli oldugu ana gorevin benzersiz tanimlayicisi.
        /// </summary>
        [Required(ErrorMessage = "Görev seçimi zorunludur.")]
        public int GorevId { get; set; }

        /// <summary>
        /// Alt gorevin bagli oldugu ana gorev nesnesi (navigasyon ozeligi).
        /// </summary>
        [ForeignKey("GorevId")]
        public virtual Gorev? Gorev { get; set; }

        /// <summary>
        /// Alt gorevin basligi. Maksimum 200 karakter uzunlugunda olabilir.
        /// </summary>
        [Required(ErrorMessage = "Başlık alanı boş bırakılamaz.")]
        [StringLength(200)]
        public string Baslik { get; set; }

        /// <summary>
        /// Alt gorevin tamamlanma durumunu belirtir. Varsayilan deger: tamamlanmamis.
        /// </summary>
        public bool TamamlandiMi { get; set; } = false;

        /// <summary>
        /// Alt goreve ait tamamlama kayitlarinin koleksiyonu.
        /// Hangi kullanicinin ne zaman tamamladigini takip eder.
        /// </summary>
        public virtual System.Collections.Generic.ICollection<AltGorevTamamlama> Tamamlamalar { get; set; } = new System.Collections.Generic.List<AltGorevTamamlama>();
    }
}
