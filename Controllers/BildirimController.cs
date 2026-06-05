using Microsoft.AspNetCore.Mvc;
using GorevTakipSistemi.Data;
using GorevTakipSistemi.Filters;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace GorevTakipSistemi.Controllers
{
    /// <summary>
    /// Kullanici bildirimlerini yoneten controller.
    /// Bildirimlerin listelenmesi, okundu olarak isaretlenmesi ve silinmesi islemlerini icerir.
    /// Tum islemler yetki kontrolu filtresi ile korunmaktadir.
    /// </summary>
    [YetkiKontrol]
    public class BildirimController : Controller
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// BildirimController yapilandirici metodu. Veritabani baglamini bagimlilik enjeksiyonu ile alir.
        /// </summary>
        /// <param name="context">Veritabani erisim baglami.</param>
        public BildirimController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Oturum acmis kullanicinin son 10 bildirimini getirir.
        /// Her bildirimin okunma durumu ve olusturulma suresini icerir.
        /// Okunmamis bildirim sayisini da yanit icinde dondurur.
        /// </summary>
        /// <returns>Okunmamis bildirim sayisi ve bildirim listesini JSON formatinda dondurur.</returns>
        [HttpGet]
        public IActionResult Getir()
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            
            // Son 10 bildirimi tarihe gore azalan sirada getir ve zaman bilgisini hesapla
            var bildirimler = _context.Bildirimler
                                      .Where(b => b.KullaniciId == kullaniciId)
                                      .OrderByDescending(b => b.OlusturmaTarihi)
                                      .Take(10)
                                      .Select(b => new {
                                          b.Id,
                                          b.Mesaj,
                                          b.Url,
                                          b.OkunduMu,
                                          Sure = (System.DateTime.Now - b.OlusturmaTarihi).TotalMinutes < 60 
                                                 ? $"{(int)(System.DateTime.Now - b.OlusturmaTarihi).TotalMinutes} dk önce" 
                                                 : $"{(int)(System.DateTime.Now - b.OlusturmaTarihi).TotalHours} saat önce"
                                      })
                                      .ToList();

            var okunmamisSayisi = _context.Bildirimler.Count(b => b.KullaniciId == kullaniciId && !b.OkunduMu);

            return Json(new { sayi = okunmamisSayisi, liste = bildirimler });
        }

        /// <summary>
        /// Belirtilen bildirimi okundu olarak isaretler.
        /// Yalnizca oturum acmis kullanicinin kendi bildirimi uzerinde islem yapilabilir.
        /// </summary>
        /// <param name="id">Okundu isaretlenecek bildirimin benzersiz kimlik numarasi.</param>
        /// <returns>Basarili islem sonucu HTTP 200 dondurur.</returns>
        [HttpPost]
        public IActionResult OkunduIsaretle(int id)
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var bildirim = _context.Bildirimler.FirstOrDefault(b => b.Id == id && b.KullaniciId == kullaniciId);
            
            if (bildirim != null)
            {
                bildirim.OkunduMu = true;
                _context.SaveChanges();
            }

            return Ok();
        }

        /// <summary>
        /// Oturum acmis kullanicinin tum okunmamis bildirimlerini toplu olarak okundu isaretler.
        /// </summary>
        /// <returns>Basarili islem sonucu HTTP 200 dondurur.</returns>
        [HttpPost]
        public IActionResult TumunuOkunduIsaretle()
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var bildirimler = _context.Bildirimler.Where(b => b.KullaniciId == kullaniciId && !b.OkunduMu).ToList();
            
            foreach (var b in bildirimler)
            {
                b.OkunduMu = true;
            }
            
            // Degisiklik varsa veritabanina kaydet
            if (bildirimler.Any())
            {
                _context.SaveChanges();
            }

            return Ok();
        }

        /// <summary>
        /// Belirtilen bildirimi kalici olarak siler.
        /// Yalnizca oturum acmis kullanicinin kendi bildirimi silinebilir.
        /// </summary>
        /// <param name="id">Silinecek bildirimin benzersiz kimlik numarasi.</param>
        /// <returns>Basarili islem sonucu HTTP 200 dondurur.</returns>
        [HttpPost]
        public IActionResult Sil(int id)
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var bildirim = _context.Bildirimler.FirstOrDefault(b => b.Id == id && b.KullaniciId == kullaniciId);
            
            if (bildirim != null)
            {
                _context.Bildirimler.Remove(bildirim);
                _context.SaveChanges();
            }

            return Ok();
        }

        /// <summary>
        /// Oturum acmis kullanicinin tum bildirimlerini toplu olarak kalici siler.
        /// </summary>
        /// <returns>Basarili islem sonucu HTTP 200 dondurur.</returns>
        [HttpPost]
        public IActionResult TumunuSil()
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var bildirimler = _context.Bildirimler.Where(b => b.KullaniciId == kullaniciId).ToList();
            
            // Silinecek bildirim varsa toplu silme islemi yap
            if (bildirimler.Any())
            {
                _context.Bildirimler.RemoveRange(bildirimler);
                _context.SaveChanges();
            }

            return Ok();
        }
    }
}
