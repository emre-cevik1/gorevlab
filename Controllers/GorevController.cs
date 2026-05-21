using Microsoft.AspNetCore.Mvc;
using GorevTakipSistemi.Data;
using GorevTakipSistemi.Models;
using GorevTakipSistemi.Filters;
using Microsoft.AspNetCore.Http;    
using System.Text.RegularExpressions;   
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace GorevTakipSistemi.Controllers
{
    [YetkiKontrol] // Giriş yapmayan kullanıcılar bu Controller'a erişemez.
    public class GorevController : Controller
    {
        private readonly AppDbContext _context;

        public GorevController(AppDbContext context)
        {
            _context = context;
        }

        // --- 1. TÜM GÖREVLERİ LİSTELE (ANA SAYFA İÇİN OPTİMİZE EDİLDİ) ---
        public IActionResult Index(string arama)
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;

            // Sadece bu kullanıcıya ait VEYA bu kullanıcının atadığı görevleri çekiyoruz
            var query = _context.Gorevler
                                .Include(g => g.Kullanici)
                                .Include(g => g.AtayanKullanici)
                                .Where(g => g.KullaniciId == kullaniciId || g.AtayanKullaniciId == kullaniciId);

            // Arama kutusu doluysa isme göre filtrele (Madde 7)
            if (!string.IsNullOrEmpty(arama))
            {
                query = query.Where(x => x.GorevAdi.Contains(arama));
            }

            // Önce aktiflik durumuna, sonra tarihe göre sırala
            var gorevler = query.OrderByDescending(g => g.DurumAktifMi)
                                .ThenBy(g => g.Tarih)
                                .ToList();

            return View("Index", gorevler);
        }

        // --- 2. AKTİF GÖREVLER (Bugün ve Gecikenler) ---
        public IActionResult Aktifler()
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;

            var gorevler = _context.Gorevler
                                   .Include(g => g.Kullanici)
                                   .Include(g => g.AtayanKullanici)
                                   .Where(g => (g.KullaniciId == kullaniciId || g.AtayanKullaniciId == kullaniciId) && 
                                               g.DurumAktifMi == true && 
                                               g.Tarih <= DateTime.Now)
                                   .OrderBy(g => g.Tarih)
                                   .ToList();

            return View("Index", gorevler);
        }

        // --- 3. TAMAMLANAN GÖREVLER ---
        public IActionResult Tamamlananlar()
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            
            var gorevler = _context.Gorevler
                                   .Include(g => g.Kullanici)
                                   .Include(g => g.AtayanKullanici)
                                   .Where(g => (g.KullaniciId == kullaniciId || g.AtayanKullaniciId == kullaniciId) && g.DurumAktifMi == false)
                                   .OrderByDescending(g => g.Tarih)
                                   .ToList();

            return View("Index", gorevler);
        }

        // --- 4. BEKLEYEN GÖREVLER (İleri Tarihliler) ---
        public IActionResult Bekleyenler()
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            
            var gorevler = _context.Gorevler
                                   .Include(g => g.Kullanici)
                                   .Include(g => g.AtayanKullanici)
                                   .Where(g => (g.KullaniciId == kullaniciId || g.AtayanKullaniciId == kullaniciId) && 
                                               g.DurumAktifMi == true && 
                                               g.Tarih > DateTime.Now)
                                   .OrderBy(g => g.Tarih)
                                   .ToList();

            return View("Index", gorevler);
        }

        // --- 5. YENİ GÖREV EKLEME EKRANI (GET) ---
        public IActionResult Create()
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            
            // Kullanıcının ait olduğu veya kurduğu ekibi bul
            var ekip = _context.Ekipler.FirstOrDefault(e => e.KurucuId == kullaniciId || _context.EkipUyeleri.Any(eu => eu.EkipId == e.Id && eu.KullaniciId == kullaniciId));
            
            if (ekip != null)
            {
                var uyeler = _context.EkipUyeleri
                                     .Where(eu => eu.EkipId == ekip.Id && eu.Durum == "KabulEdildi")
                                     .Select(eu => eu.Kullanici)
                                     .ToList();
                
                var kurucu = _context.Kullanicilar.Find(ekip.KurucuId);
                if (kurucu != null && !uyeler.Any(u => u.Id == kurucu.Id)) uyeler.Add(kurucu);

                ViewBag.EkipUyeleri = uyeler;
                ViewBag.EkipId = ekip.Id;
            }

            return View();
        }

        // --- 6. YENİ GÖREV EKLEME İŞLEMİ (POST) ---
        [HttpPost]
        public IActionResult Create(Gorev gorev, int atananKullaniciId)
        {
            string zararliKodDeseni = @"<[^>]+>"; 
            if (Regex.IsMatch(gorev.GorevAdi ?? "", zararliKodDeseni) || Regex.IsMatch(gorev.Aciklama ?? "", zararliKodDeseni))
            {
                TempData["Error"] = "Güvenlik İhlali: Görev bilgilerinde HTML, JS veya CSS kodları kullanılamaz!";
                return View(gorev);
            }

            int me = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            
            if (atananKullaniciId > 0)
            {
                gorev.KullaniciId = atananKullaniciId;
                if (atananKullaniciId != me) 
                {
                    gorev.AtayanKullaniciId = me;
                }
            }
            else
            {
                gorev.KullaniciId = me;
            }

            gorev.DurumAktifMi = true; // Yeni görev eklendiğinde aktiftir
            
            _context.Gorevler.Add(gorev);
            _context.SaveChanges();

            // Eğer başkasına atandıysa BİLDİRİM GÖNDER
            if (gorev.KullaniciId != me)
            {
                string adSoyad = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Biri";
                _context.Bildirimler.Add(new Bildirim {
                    KullaniciId = gorev.KullaniciId,
                    Mesaj = $"{adSoyad} sana '{gorev.GorevAdi}' görevini atadı.",
                    Url = $"/Gorev/Details/{gorev.Id}"
                });
                _context.SaveChanges();
            }

            // LOG SİSTEMİ
            _context.SistemLoglari.Add(new SistemLog {
                KullaniciAdi = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Bilinmeyen Kullanıcı",
                YapilanIslem = $"Yeni görev oluşturuldu: {gorev.GorevAdi}",
                IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP",
                IslemTarihi = DateTime.Now
            });

            _context.SaveChanges();

            TempData["Success"] = "Yeni görev başarıyla eklendi!";
            return RedirectToAction("Index");
        }

        // --- 7. GÖREV DETAYLARI ---
        public IActionResult Details(int id)
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var gorev = _context.Gorevler.FirstOrDefault(g => g.Id == id && g.KullaniciId == kullaniciId);
            
            if (gorev == null) return NotFound("Görev bulunamadı!");

            return View(gorev);
        }

        // --- 8. GÖREV DÜZENLEME EKRANI (GET) ---
        public IActionResult Edit(int id)
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var gorev = _context.Gorevler.FirstOrDefault(g => g.Id == id && g.KullaniciId == kullaniciId);
            
            if (gorev == null) return NotFound("Düzenlenecek görev bulunamadı!");

            return View(gorev);
        }

        // --- 9. GÖREV DÜZENLEME İŞLEMİ (POST) ---
        [HttpPost]
        public IActionResult Edit(Gorev guncelGorev)
        {
            string zararliKodDeseni = @"<[^>]+>";
            if (Regex.IsMatch(guncelGorev.GorevAdi ?? "", zararliKodDeseni) || Regex.IsMatch(guncelGorev.Aciklama ?? "", zararliKodDeseni))
            {
                TempData["Error"] = "Güvenlik İhlali: Görev bilgilerinde HTML, JS veya CSS kodları kullanılamaz!";
                return View(guncelGorev);
            }

            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var asilGorev = _context.Gorevler.FirstOrDefault(g => g.Id == guncelGorev.Id && g.KullaniciId == kullaniciId);
            
            if (asilGorev != null)
            {
                asilGorev.GorevAdi = guncelGorev.GorevAdi;
                asilGorev.Aciklama = guncelGorev.Aciklama;
                asilGorev.Oncelik = guncelGorev.Oncelik;
                asilGorev.DurumAktifMi = guncelGorev.DurumAktifMi;
                asilGorev.Tarih = guncelGorev.Tarih;

                // LOG SİSTEMİ
                _context.SistemLoglari.Add(new SistemLog {
                    KullaniciAdi = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Bilinmeyen Kullanıcı",
                    YapilanIslem = $"Görev güncellendi: {asilGorev.GorevAdi}",
                    IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP",
                    IslemTarihi = DateTime.Now
                });

                _context.SaveChanges();
                TempData["Success"] = "Görev başarıyla güncellendi!";
            }

            return RedirectToAction("Index");
        }

        // --- 10. GÖREVİ TAMAMLA İŞLEMİ ---
        public IActionResult Tamamla(int id)
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var gorev = _context.Gorevler.FirstOrDefault(g => g.Id == id && g.KullaniciId == kullaniciId);

            if (gorev != null)
            {
                gorev.DurumAktifMi = false; 

                // LOG SİSTEMİ
                _context.SistemLoglari.Add(new SistemLog {
                    KullaniciAdi = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Bilinmeyen Kullanıcı",
                    YapilanIslem = $"Görev tamamlandı: {gorev.GorevAdi}",
                    IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP",
                    IslemTarihi = DateTime.Now
                });

                _context.SaveChanges();
                TempData["Success"] = "Görev başarıyla tamamlandı!";
            }

            return RedirectToAction("Index");
        }

        // --- 11. GÖREV SİLME İŞLEMİ ---
        public IActionResult Delete(int id, string sayfa = "Index")
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var gorev = _context.Gorevler.FirstOrDefault(g => g.Id == id && g.KullaniciId == kullaniciId);
            
            if (gorev != null)
            {
                // LOG SİSTEMİ
                _context.SistemLoglari.Add(new SistemLog {
                    KullaniciAdi = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Bilinmeyen Kullanıcı",
                    YapilanIslem = $"Görev silindi: {gorev.GorevAdi}",
                    IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP",
                    IslemTarihi = DateTime.Now
                });

                _context.Gorevler.Remove(gorev);
                _context.SaveChanges();
                TempData["Success"] = "Görev başarıyla silindi.";
            }

            return RedirectToAction(sayfa);
        }

        // --- 12. GÖREV CHECKLIST: TEK TIKLA DURUM DEĞİŞTİRME ---
        public IActionResult DurumDegistir(int id)
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var gorev = _context.Gorevler.FirstOrDefault(g => g.Id == id && g.KullaniciId == kullaniciId);
            if (gorev != null)
            {
                gorev.DurumAktifMi = !gorev.DurumAktifMi;
                _context.SaveChanges();
            }
            
            string oncekiSayfa = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(oncekiSayfa))
            {
                return Redirect(oncekiSayfa);
            }
            
            return RedirectToAction("Index");
        }

        // --- 13. GÖREV DETAY GETİR (AJAX MODAL İÇİN) - GÜVENLİĞİ ARTIRILDI ---
        public IActionResult DetayGetir(int id)
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            
            // Sadece giriş yapan kullanıcı kendi görevini görüntüleyebilir!
            var gorev = _context.Gorevler.FirstOrDefault(g => g.Id == id && g.KullaniciId == kullaniciId);
            
            if (gorev == null) 
            {
                return NotFound("<div class='p-4 text-center text-red-500 font-bold'>Bu görevi görüntülemeye yetkiniz yok veya görev bulunamadı!</div>");
            }
            
            return PartialView("_GorevDetayPartial", gorev);
        }
    }
}