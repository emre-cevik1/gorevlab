using System;
using System.ComponentModel.DataAnnotations;

namespace GorevTakipSistemi.Models
{
    /// <summary>
    /// Sistemdeki kullanici hesap bilgilerini temsil eden model sinifi.
    /// Kimlik dogrulama, rol yonetimi, ban sistemi ve e-posta onay islemlerini icerir.
    /// </summary>
    public class Kullanici
    {
        /// <summary>
        /// Kullanicinin benzersiz tanimlayicisi.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Kullanicinin adi.
        /// </summary>
        [Required]
        public string Ad { get; set; }

        /// <summary>
        /// Kullanicinin soyadi.
        /// </summary>
        [Required]
        public string Soyad { get; set; }

        /// <summary>
        /// Kullanicinin sisteme giris icin kullandigi benzersiz kullanici adi.
        /// </summary>
        [Required]
        public string KullaniciAdi { get; set; }

        /// <summary>
        /// Kullanicinin e-posta adresi. Gecerli bir e-posta formati gerektirir.
        /// </summary>
        [Required(ErrorMessage = "E-Posta zorunludur")]
        [EmailAddress]
        [RegularExpression(@"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$", ErrorMessage = "Lütfen geçerli bir e-posta formatı girin.")]
        public string Email { get; set; }

        /// <summary>
        /// Kullanicinin sifresinin hash olarak saklanmis degeri.
        /// </summary>
        [Required]
        public string SifreHash { get; set; }

        /// <summary>
        /// Kullanicinin sistemdeki rolunu belirler (NormalKullanici, Admin, Owner).
        /// </summary>
        public KullaniciRol Rol { get; set; }

        /// <summary>
        /// Kullanicinin sisteme girisinin yasaklanip yasaklanmadigini belirtir. Varsayilan deger: yasaklanmamis.
        /// </summary>
        public bool IsBanned { get; set; } 

        /// <summary>
        /// Sifre sifirlama islemi icin uretilen tek kullanimlik guvenlik jetonu.
        /// </summary>
        public string? ResetToken { get; set; } 

        /// <summary>
        /// Sifre sifirlama jetonunun gecerlilik bitis tarihi.
        /// </summary>
        public DateTime? ResetTokenBitisSuresi { get; set; } 

        /// <summary>
        /// Kullanicinin profil fotografinin dosya yolu. Null ise varsayilan profil resmi kullanilir.
        /// </summary>
        public string? ProfilResmi { get; set; }

        /// <summary>
        /// Kullanicinin Kisisel Verilerin Korunmasi Kanunu (KVKK) onayi verip vermedigini belirtir.
        /// </summary>
        public bool KvkkOnay { get; set; }

        /// <summary>
        /// Sureli ban uygulamasinin sona erme tarih ve saat bilgisi. Null ise ban suresiz veya ban yoktur.
        /// </summary>
        public DateTime? BanBitisTarihi { get; set; }

        /// <summary>
        /// Kullaniciya uygulanan ban isleminin gerekce aciklamasi.
        /// </summary>
        public string? BanNedeni { get; set; }

        /// <summary>
        /// Kullanicinin e-posta adresinin dogrulanip dogrulanmadigini belirtir. Varsayilan deger: dogrulanmamis.
        /// </summary>
        public bool IsEmailConfirmed { get; set; } = false;

        /// <summary>
        /// E-posta dogrulama islemi icin uretilen aktivasyon jetonu.
        /// </summary>
        public string? EmailConfirmationToken { get; set; }
    }
}