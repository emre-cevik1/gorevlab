using System;

namespace GorevTakipSistemi.Models
{
    public class GorevTamamlama
    {
        public int Id { get; set; }

        public int GorevId { get; set; }
        public virtual Gorev Gorev { get; set; }

        public int KullaniciId { get; set; }
        public virtual Kullanici Kullanici { get; set; }

        public DateTime TamamlamaTarihi { get; set; } = DateTime.Now;
    }
}
