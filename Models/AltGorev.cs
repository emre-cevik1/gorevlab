using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GorevTakipSistemi.Models
{
    public class AltGorev
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Görev seçimi zorunludur.")]
        public int GorevId { get; set; }

        [ForeignKey("GorevId")]
        public virtual Gorev? Gorev { get; set; }

        [Required(ErrorMessage = "Başlık alanı boş bırakılamaz.")]
        [StringLength(200)]
        public string Baslik { get; set; }

        public bool TamamlandiMi { get; set; } = false;
    }
}
