using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GorevTakipSistemi.Models
{
    /// <summary>
    /// Gorevlere atanabilecek etiket bilgilerini tutan model sinifi.
    /// Her etiket bir isme, renge ve opsiyonel olarak bir ekip aidiyetine sahiptir.
    /// </summary>
    public class Etiket
    {
        /// <summary>
        /// Etiketin benzersiz tanimlayicisi.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Etiketin goruntulenen adi. Maksimum 50 karakter uzunlugunda olabilir.
        /// </summary>
        [Required(ErrorMessage = "Etiket adı zorunludur.")]
        [StringLength(50)]
        public string Ad { get; set; }

        /// <summary>
        /// Etiketin gorunum rengi (HEX formati, ornegin: #FF0000). Varsayilan deger: indigo (#4f46e5).
        /// </summary>
        [StringLength(7)]
        public string RenkHex { get; set; } = "#4f46e5";

        /// <summary>
        /// Etiketin ait oldugu ekibin benzersiz tanimlayicisi. Null ise tum ekiplerde kullanilabilir.
        /// </summary>
        public int? EkipId { get; set; }

        /// <summary>
        /// Etiketin ait oldugu ekip nesnesi (navigasyon ozeligi).
        /// </summary>
        public virtual Ekip? Ekip { get; set; }

        /// <summary>
        /// Bu etikete sahip gorev-etiket iliskilerinin koleksiyonu.
        /// </summary>
        public virtual ICollection<GorevEtiket>? GorevEtiketleri { get; set; }
    }
}
