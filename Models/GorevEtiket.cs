using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GorevTakipSistemi.Models
{
    /// <summary>
    /// Gorev ile etiket arasindaki coka-cok iliskiyi temsil eden ara tablo model sinifi.
    /// Her kayit bir gorevin belirli bir etikete sahip oldugunu belirtir.
    /// </summary>
    public class GorevEtiket
    {
        /// <summary>
        /// Gorev-etiket iliskisinin benzersiz tanimlayicisi.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Iliskili gorevin benzersiz tanimlayicisi.
        /// </summary>
        public int GorevId { get; set; }

        /// <summary>
        /// Iliskili gorev nesnesi (navigasyon ozeligi).
        /// </summary>
        [ForeignKey("GorevId")]
        public virtual Gorev? Gorev { get; set; }

        /// <summary>
        /// Iliskili etiketin benzersiz tanimlayicisi.
        /// </summary>
        public int EtiketId { get; set; }

        /// <summary>
        /// Iliskili etiket nesnesi (navigasyon ozeligi).
        /// </summary>
        [ForeignKey("EtiketId")]
        public virtual Etiket? Etiket { get; set; }
    }
}
