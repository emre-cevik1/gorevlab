using System;

namespace GorevTakipSistemi.Models
{
    /// <summary>
    /// Kullanicilarin yonetim ekibine gonderdigi destek taleplerini temsil eden model sinifi.
    /// Kullanici mesajini ve yonetici cevabini icerir.
    /// </summary>
    public class DestekMesaji
    {
        /// <summary>
        /// Destek mesajinin benzersiz tanimlayicisi.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Destek mesajini gonderen kullanicinin benzersiz tanimlayicisi.
        /// </summary>
        public int KullaniciId { get; set; }

        /// <summary>
        /// Destek talebinin konu basligi.
        /// </summary>
        public string Konu { get; set; }

        /// <summary>
        /// Kullanicinin destek talebi mesaj icerigi.
        /// </summary>
        public string Mesaj { get; set; }

        /// <summary>
        /// Yonetici tarafindan verilen cevap metni. Henuz cevaplanmamissa null degerini alir.
        /// </summary>
        public string? Cevap { get; set; }

        /// <summary>
        /// Destek talebinin yonetici tarafindan cevaplanip cevaplanmadigini belirtir. Varsayilan deger: cevaplanmamis.
        /// </summary>
        public bool IsCevaplandi { get; set; } = false;

        /// <summary>
        /// Destek mesajinin gonderilme tarih ve saat bilgisi. Varsayilan deger: olusturulma ani.
        /// </summary>
        public DateTime Tarih { get; set; } = DateTime.Now;

        /// <summary>
        /// Mesaji gonderen kullanici nesnesi (navigasyon ozeligi).
        /// </summary>
        public virtual Kullanici Kullanici { get; set; }
    }
}