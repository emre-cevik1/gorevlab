using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GorevTakipSistemi.Models
{
    public class AltGorevTamamlama
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AltGorevId { get; set; }

        [ForeignKey("AltGorevId")]
        public virtual AltGorev? AltGorev { get; set; }

        [Required]
        public int KullaniciId { get; set; }

        [ForeignKey("KullaniciId")]
        public virtual Kullanici? Kullanici { get; set; }

        public DateTime TamamlamaTarihi { get; set; } = DateTime.Now;
    }
}
