using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GorevTakipSistemi.Models
{
    public class Bildirim
    {
        [Key]
        public int Id { get; set; }

        public int KullaniciId { get; set; }
        
        [ForeignKey("KullaniciId")]
        public virtual Kullanici Kullanici { get; set; }

        [Required]
        public string Mesaj { get; set; }

        public string Url { get; set; } // Bildirime tıklayınca gidilecek sayfa (örn: /Gorev/Details/5)

        public bool OkunduMu { get; set; } = false;

        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    }
}
