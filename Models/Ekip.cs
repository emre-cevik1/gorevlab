using System;
using System.Collections.Generic;

namespace GorevTakipSistemi.Models
{
    /// <summary>
    /// Sistemdeki ekip bilgilerini temsil eden model sinifi.
    /// Bir ekip; uyeleri, gorevleri ve davetleri icerir.
    /// </summary>
    public class Ekip
    {
        /// <summary>
        /// Ekibin benzersiz tanimlayicisi.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Ekibin adi.
        /// </summary>
        public string Ad { get; set; }

        /// <summary>
        /// Ekibin amacini veya faaliyet alanini belirten aciklama metni.
        /// </summary>
        public string Aciklama { get; set; }

        /// <summary>
        /// Ekibin olusturulma tarih ve saat bilgisi. Varsayilan deger: olusturulma ani.
        /// </summary>
        public DateTime KurulusTarihi { get; set; } = DateTime.Now;

        /// <summary>
        /// Ekibi olusturan kullanicinin benzersiz tanimlayicisi.
        /// </summary>
        public int KurucuId { get; set; }

        /// <summary>
        /// Ekibi olusturan kullanici nesnesi (navigasyon ozeligi).
        /// </summary>
        public virtual Kullanici Kurucu { get; set; }

        /// <summary>
        /// Ekibe ait uyelerin koleksiyonu.
        /// </summary>
        public virtual ICollection<EkipUyesi> Uyeler { get; set; }

        /// <summary>
        /// Ekibe atanmis gorevlerin koleksiyonu.
        /// </summary>
        public virtual ICollection<Gorev> Gorevler { get; set; }

        /// <summary>
        /// Ekibe gonderilmis davetlerin koleksiyonu.
        /// </summary>
        public virtual ICollection<EkipDavet> Davetler { get; set; }
    }
}