using System;

namespace GorevTakipSistemi.Models
{
    public class EkipUyesi
    {
        public int Id { get; set; }
        
        public int EkipId { get; set; }
        public virtual Ekip Ekip { get; set; }

        public int KullaniciId { get; set; }
        public virtual Kullanici Kullanici { get; set; }

        public string Rol { get; set; } // "Lider", "Uye"
        public DateTime KatilmaTarihi { get; set; } = DateTime.Now;
    }
}