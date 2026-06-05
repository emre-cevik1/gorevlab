using System;

namespace GorevTakipSistemi.Models
{
    /// <summary>
    /// Ekip uyeligine davet bilgilerini tutan model sinifi.
    /// Gonderen, alici ve davetin mevcut durumunu icerir.
    /// </summary>
    public class EkipDavet
    {
        /// <summary>
        /// Davetin benzersiz tanimlayicisi.
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Davetin ait oldugu ekibin benzersiz tanimlayicisi.
        /// </summary>
        public int EkipId { get; set; }

        /// <summary>
        /// Davetin ait oldugu ekip nesnesi (navigasyon ozeligi).
        /// </summary>
        public virtual Ekip Ekip { get; set; }

        /// <summary>
        /// Daveti gonderen kullanicinin benzersiz tanimlayicisi.
        /// </summary>
        public int GonderenId { get; set; }

        /// <summary>
        /// Daveti gonderen kullanici nesnesi (navigasyon ozeligi).
        /// </summary>
        public virtual Kullanici Gonderen { get; set; }

        /// <summary>
        /// Davetin gonderildigi hedef kullanicinin benzersiz tanimlayicisi.
        /// </summary>
        public int AliciId { get; set; }

        /// <summary>
        /// Davetin gonderildigi hedef kullanici nesnesi (navigasyon ozeligi).
        /// </summary>
        public virtual Kullanici Alici { get; set; }

        /// <summary>
        /// Davetin mevcut durumu. Gecerli degerler: "Bekliyor", "Kabul", "Red". Varsayilan deger: "Bekliyor".
        /// </summary>
        public string Durum { get; set; } = "Bekliyor";

        /// <summary>
        /// Davetin gonderilme tarih ve saat bilgisi. Varsayilan deger: olusturulma ani.
        /// </summary>
        public DateTime DavetTarihi { get; set; } = DateTime.Now;
    }
}