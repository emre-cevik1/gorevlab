using Microsoft.AspNetCore.Mvc;
using GorevTakipSistemi.Data;
using GorevTakipSistemi.Filters;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace GorevTakipSistemi.Controllers
{
    [YetkiKontrol]
    public class BildirimController : Controller
    {
        private readonly AppDbContext _context;

        public BildirimController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Getir()
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            
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

        [HttpPost]
        public IActionResult TumunuOkunduIsaretle()
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var bildirimler = _context.Bildirimler.Where(b => b.KullaniciId == kullaniciId && !b.OkunduMu).ToList();
            
            foreach (var b in bildirimler)
            {
                b.OkunduMu = true;
            }
            
            if (bildirimler.Any())
            {
                _context.SaveChanges();
            }

            return Ok();
        }
    }
}
