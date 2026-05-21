using System;
using System.ComponentModel.DataAnnotations;

namespace GorevTakipSistemi.Models
{
    public class Gorev
    {
        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Görev adı zorunludur.")]
        public string GorevAdi { get; set; }
        
        public string Aciklama { get; set; }
        
        public string Oncelik { get; set; } // Yüksek, Orta, Düşük
        
        public DateTime Tarih { get; set; }
        
        public bool DurumAktifMi { get; set; } = true; // True: Aktif/Bekleyen, False: Tamamlandı
        
        // Bu görev hangi kullanıcıya ait? (Kime atandı?)
        public int KullaniciId { get; set; }
        public virtual Kullanici? Kullanici { get; set; }
        
        // Bu görevi kim atadı? (Null ise kendisi atamıştır)
        public int? AtayanKullaniciId { get; set; }
        public virtual Kullanici? AtayanKullanici { get; set; }

        public int? EkipId { get; set; }
        public virtual Ekip Ekip { get; set; }

        // Bu görevi kim tamamladı?
        public int? TamamlayanKullaniciId { get; set; }
        public virtual Kullanici? TamamlayanKullanici { get; set; }
    }
}