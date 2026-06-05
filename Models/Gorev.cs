using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace GorevTakipSistemi.Models
{
    /// <summary>
    /// Sistemdeki gorev bilgilerini temsil eden ana model sinifi.
    /// Gorev atama, oncelik, Kanban durumu ve alt gorev iliskilerini icerir.
    /// </summary>
    public class Gorev
    {
        /// <summary>
        /// Gorevin benzersiz tanimlayicisi.
        /// </summary>
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Gorevin adi veya basligi.
        /// </summary>
        [Required(ErrorMessage = "Görev adı zorunludur.")]
        public string GorevAdi { get; set; }
        
        /// <summary>
        /// Gorevle ilgili detayli aciklama metni.
        /// </summary>
        public string Aciklama { get; set; }
        
        /// <summary>
        /// Gorevin oncelik seviyesi. Gecerli degerler: "Yuksek", "Orta", "Dusuk".
        /// </summary>
        public string Oncelik { get; set; }
        
        /// <summary>
        /// Gorevin son teslim tarihi.
        /// </summary>
        public DateTime Tarih { get; set; }
        
        /// <summary>
        /// Gorevin aktiflik durumunu belirtir. True: aktif veya bekleyen, False: tamamlanmis. Varsayilan deger: aktif.
        /// </summary>
        public bool DurumAktifMi { get; set; } = true;
        
        /// <summary>
        /// Gorevin Kanban panosundaki mevcut durumu. Gecerli degerler: "Bekleyen", "Yapiliyor", "Tamamlandi". Varsayilan deger: "Bekleyen".
        /// </summary>
        public string KanbanDurumu { get; set; } = "Bekleyen";
        
        /// <summary>
        /// Gorevin atandigi kullanicinin benzersiz tanimlayicisi.
        /// </summary>
        public int KullaniciId { get; set; }

        /// <summary>
        /// Gorevin atandigi kullanici nesnesi (navigasyon ozeligi).
        /// </summary>
        public virtual Kullanici? Kullanici { get; set; }
        
        /// <summary>
        /// Gorevi atayan kullanicinin benzersiz tanimlayicisi. Null ise gorev kullanicinin kendisi tarafindan olusturulmustur.
        /// </summary>
        public int? AtayanKullaniciId { get; set; }

        /// <summary>
        /// Gorevi atayan kullanici nesnesi (navigasyon ozeligi).
        /// </summary>
        public virtual Kullanici? AtayanKullanici { get; set; }

        /// <summary>
        /// Gorevin ait oldugu ekibin benzersiz tanimlayicisi. Null ise gorev kisisel bir gorevdir.
        /// </summary>
        public int? EkipId { get; set; }

        /// <summary>
        /// Gorevin ait oldugu ekip nesnesi (navigasyon ozeligi).
        /// </summary>
        public virtual Ekip? Ekip { get; set; }

        /// <summary>
        /// Goreve bagli alt gorevlerin (kontrol listesi) koleksiyonu.
        /// </summary>
        public virtual ICollection<AltGorev>? AltGorevler { get; set; }

        /// <summary>
        /// Goreve atanmis etiketlerin iliski koleksiyonu.
        /// </summary>
        public virtual ICollection<GorevEtiket>? GorevEtiketleri { get; set; }

        /// <summary>
        /// Gorevi tamamlayan kullanicilarin kayit koleksiyonu. Coklu tamamlama sistemi icin kullanilir.
        /// </summary>
        public virtual ICollection<GorevTamamlama> Tamamlamalar { get; set; } = new List<GorevTamamlama>();
    }
}