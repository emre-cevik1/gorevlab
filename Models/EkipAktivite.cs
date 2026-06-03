using System;
using System.ComponentModel.DataAnnotations;

namespace GorevTakipSistemi.Models
{
    public class EkipAktivite
    {
        [Key]
        public int Id { get; set; }

        public int EkipId { get; set; }
        public virtual Ekip Ekip { get; set; }

        public int KullaniciId { get; set; }
        public virtual Kullanici Kullanici { get; set; }

        [Required]
        public string Aksiyon { get; set; } // "Oluşturdu", "Tamamladı", "Durum Değiştirdi"

        public string Mesaj { get; set; } // "Frontend arayüzü görevini 'Yapılıyor' sütununa taşıdı."

        public DateTime Tarih { get; set; } = DateTime.Now;
    }
}
