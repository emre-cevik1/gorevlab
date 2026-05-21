using System;
using System.ComponentModel.DataAnnotations;

namespace GorevTakipSistemi.Models
{
    public class Kullanici
    {
        public int Id { get; set; }

        [Required]
        public string Ad { get; set; }

        [Required]
        public string Soyad { get; set; }

        [Required]
        public string KullaniciAdi { get; set; }

        [Required(ErrorMessage = "E-Posta zorunludur")]
        [EmailAddress]
        [RegularExpression(@"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$", ErrorMessage = "Lütfen geçerli bir e-posta formatı girin.")]
        public string Email { get; set; }

        [Required]
        public string SifreHash { get; set; }

        public KullaniciRol Rol { get; set; }

        // --- YENİ EKLENEN: BAN SİSTEMİ ---
        // Kullanıcının sisteme girişinin yasaklanıp yasaklanmadığını tutar. Default: false
        public bool IsBanned { get; set; } 

        // --- PROFESYONEL ŞİFRE SIFIRLAMA İÇİN EKLENENLER ---
        public string? ResetToken { get; set; } 
        public DateTime? ResetTokenBitisSuresi { get; set; } 
        // Varsayılan olarak null olabilir, eğer boşsa "default.png" gibi bir şey gösterebiliriz.
public string? ProfilResmi { get; set; }
// --- KVKK ONAYI ---
        public bool KvkkOnay { get; set; }

        // Süreli Ban Sistemi İçin Yeni Özellikler
        public DateTime? BanBitisTarihi { get; set; } // Soru işareti (?) boş olabileceğini belirtir
        public string? BanNedeni { get; set; }

        // --- E-POSTA ONAY SİSTEMİ ---
        public bool IsEmailConfirmed { get; set; } = false; // Başlangıçta herkes onaylanmamış
        public string? EmailConfirmationToken { get; set; } // Aktivasyon linki için özel kod
    }
}