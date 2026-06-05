using System;

namespace GorevTakipSistemi.Models
{
    /// <summary>
    /// Ekip uyelik bilgilerini temsil eden model sinifi.
    /// Bir kullanicinin hangi ekipte hangi rolle yer aldigini ve katilma tarihini icerir.
    /// </summary>
    public class EkipUyesi
    {
        /// <summary>
        /// Ekip uyeliginin benzersiz tanimlayicisi.
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Uyenin ait oldugu ekibin benzersiz tanimlayicisi.
        /// </summary>
        public int EkipId { get; set; }

        /// <summary>
        /// Uyenin ait oldugu ekip nesnesi (navigasyon ozeligi).
        /// </summary>
        public virtual Ekip Ekip { get; set; }

        /// <summary>
        /// Ekip uyesi olan kullanicinin benzersiz tanimlayicisi.
        /// </summary>
        public int KullaniciId { get; set; }

        /// <summary>
        /// Ekip uyesi olan kullanici nesnesi (navigasyon ozeligi).
        /// </summary>
        public virtual Kullanici Kullanici { get; set; }

        /// <summary>
        /// Uyenin ekip icerisindeki rolu. Gecerli degerler: "Lider", "Uye".
        /// </summary>
        public string Rol { get; set; }

        /// <summary>
        /// Uyenin ekibe katildigi tarih ve saat bilgisi. Varsayilan deger: katilma ani.
        /// </summary>
        public DateTime KatilmaTarihi { get; set; } = DateTime.Now;
    }
}