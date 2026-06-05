using Microsoft.AspNetCore.Mvc;
using GorevTakipSistemi.Data;
using GorevTakipSistemi.Models;
using GorevTakipSistemi.Filters;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace GorevTakipSistemi.Controllers
{
    /// <summary>
    /// Iletisim ve destek talepleri islemlerini yoneten controller.
    /// Kullanicilarin destek talebi olusturmasini ve gondermesini saglar.
    /// Tum islemler yetki kontrolu filtresi ile korunmaktadir.
    /// </summary>
    [YetkiKontrol]
    public class IletisimController : Controller
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// IletisimController yapilandirici metodu. Veritabani baglamini bagimlilik enjeksiyonu ile alir.
        /// </summary>
        /// <param name="context">Veritabani erisim baglami.</param>
        public IletisimController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Iletisim ve destek talebi formunun goruntulendigi sayfayi dondurur.
        /// </summary>
        /// <returns>Iletisim formu gorunumunu dondurur.</returns>
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Yeni destek talebi olusturur ve veritabanina kaydeder.
        /// Kullanici basina en fazla 5 cevapsiz destek talebi bulunabilir (spam onleme).
        /// Destek talebi olusturma islemi sistem loguna kaydedilir.
        /// </summary>
        /// <param name="konu">Destek talebinin konu basligi.</param>
        /// <param name="mesaj">Destek talebinin detayli aciklama metni.</param>
        /// <returns>Basarili gonderim sonrasi anasayfaya yonlendirir.</returns>
        [HttpPost]
        public async Task<IActionResult> Gonder(string konu, string mesaj)
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            if (kullaniciId == 0) return RedirectToAction("Login", "Auth");

            // Spam onleme: Kullanici basina en fazla 5 cevapsiz talep sinirligi
            int bekleyenTalepSayisi = _context.DestekMesajlari.Count(d => d.KullaniciId == kullaniciId && d.IsCevaplandi == false);
            if (bekleyenTalepSayisi >= 5)
            {
                TempData["Error"] = "Limit Aşıldı: Şu anda bekleyen 5 adet destek talebiniz bulunuyor. Taleplerinizden en az biri cevaplanana kadar yenisini gönderemezsiniz.";
                return RedirectToAction("Index");
            }

            // Yeni destek mesaji olustur
            var yeniMesaj = new DestekMesaji
            {
                KullaniciId = kullaniciId,
                Konu = konu,
                Mesaj = mesaj,
                Tarih = DateTime.Now,
                IsCevaplandi = false
            };

            _context.DestekMesajlari.Add(yeniMesaj);

            // Destek talebi acma islemini sistem loguna kaydet
            _context.SistemLoglari.Add(new SistemLog {
                KullaniciAdi = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Bilinmeyen Kullanıcı",
                YapilanIslem = "Yeni destek talebi açıldı",
                IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP",
                IslemTarihi = DateTime.Now
            });

            // Destek mesaji ve log kaydini veritabanina kaydet
            await _context.SaveChangesAsync();

            TempData["Success"] = "Destek talebiniz alındı! En kısa sürede e-posta adresiniz üzerinden dönüş yapılacaktır.";
            return RedirectToAction("Index", "Home");
        }
    }
}
