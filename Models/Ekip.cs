using System;
using System.Collections.Generic;

namespace GorevTakipSistemi.Models
{
    public class Ekip
    {
        public int Id { get; set; }
        public string Ad { get; set; }
        public string Aciklama { get; set; }
        public DateTime KurulusTarihi { get; set; } = DateTime.Now;

        // Ekibi kuran kişi
        public int KurucuId { get; set; }
        public virtual Kullanici Kurucu { get; set; }

        // Ekibe bağlı listeler
        public virtual ICollection<EkipUyesi> Uyeler { get; set; }
        public virtual ICollection<Gorev> Gorevler { get; set; }
        public virtual ICollection<EkipDavet> Davetler { get; set; }
    }
}