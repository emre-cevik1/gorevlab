using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GorevTakipSistemi.Models
{
    public class Etiket
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Etiket adı zorunludur.")]
        [StringLength(50)]
        public string Ad { get; set; }

        [StringLength(7)] // Örn: #FF0000
        public string RenkHex { get; set; } = "#4f46e5"; // Varsayılan indigo

        // Bu etiket hangi ekibe ait? (Sadece o ekibin görevlerinde kullanılabilir)
        public int? EkipId { get; set; }
        public virtual Ekip? Ekip { get; set; }

        public virtual ICollection<GorevEtiket>? GorevEtiketleri { get; set; }
    }
}
