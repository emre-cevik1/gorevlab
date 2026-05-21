using System;

namespace GorevTakipSistemi.Models
{
    public class EkipDavet
    {
        public int Id { get; set; }
        
        public int EkipId { get; set; }
        public virtual Ekip Ekip { get; set; }

        public int GonderenId { get; set; }
        public virtual Kullanici Gonderen { get; set; }

        public int AliciId { get; set; }
        public virtual Kullanici Alici { get; set; }

        public string Durum { get; set; } = "Bekliyor"; // "Bekliyor", "Kabul", "Red"
        public DateTime DavetTarihi { get; set; } = DateTime.Now;
    }
}