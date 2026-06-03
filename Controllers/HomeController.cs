using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using GorevTakipSistemi.Data;
using GorevTakipSistemi.Models;
using System.Linq;
using System;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; 

namespace GorevTakipSistemi.Controllers
{
    public class HomeController : Controller
    {
        // --- BAKIM SAYFASI ---
        [HttpGet]
        public IActionResult Bakim()
        {
            if (!GorevTakipSistemi.Models.SiteSettings.BakimModuAktif)
                return RedirectToAction("Index"); // Eğer bakım modu kapalıysa, ana sayfaya at

            return View();
        }
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public HomeController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // --- 1. ANA SAYFA ---
        public IActionResult Index()
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciId");
            if (kullaniciId == null) return RedirectToAction("Login", "Auth");
            
            // Eğer giriş yapmışsa direkt Dashboard'a yönlendirebiliriz, 
            // ya da kullanıcıya özel çok basit bir özet sunabiliriz.
            // Kullanıcı ayrı bir sayfa istediği için şimdilik basit bir karşılama sayfası yapalım.
            return View();
        }

        // --- 1.1 DASHBOARD (GELİŞMİŞ İSTATİSTİKLER) ---
        public IActionResult Dashboard()
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciId");
            if (kullaniciId == null) return RedirectToAction("Login", "Auth");

            var kullanicininGorevleri = _context.Gorevler
                                        .Where(g => g.KullaniciId == kullaniciId && g.EkipId == null)
                                        .ToList();

            var ekipIdleri = _context.EkipUyeleri
                                     .Where(u => u.KullaniciId == kullaniciId)
                                     .Select(u => u.EkipId)
                                     .ToList();

            var ekipGorevleri = _context.Gorevler
                                        .Include(g => g.Ekip) 
                                        .Where(g => g.EkipId != null && ekipIdleri.Contains(g.EkipId.Value) && g.DurumAktifMi)
                                        .OrderBy(g => g.Tarih)
                                        .ToList();

            ViewBag.AktifSayisi = kullanicininGorevleri.Where(g => g.DurumAktifMi).Count();
            ViewBag.TamamlananSayisi = kullanicininGorevleri.Where(g => !g.DurumAktifMi).Count();
            ViewBag.BekleyenSayisi = kullanicininGorevleri.Where(g => g.DurumAktifMi && g.Tarih.Date > DateTime.Now.Date).Count();
            ViewBag.BugunSayisi = kullanicininGorevleri.Where(g => g.DurumAktifMi && g.Tarih.Date == DateTime.Now.Date).Count();

            ViewBag.GunlukGorevler = kullanicininGorevleri
                                    .OrderBy(g => g.Tarih)
                                    .Take(5)
                                    .ToList();
                                    
            ViewBag.EkipGorevleri = ekipGorevleri;

            // --- GRAFİK VERİLERİ (CHART.JS) ---
            
            // 1. Çizgi Grafiği: Son 7 günün / Gelecek 7 günün görev hedefleri
            var baslangicTarihi = DateTime.Now.Date.AddDays(-3); // Geçmiş 3 gün ve Gelecek 3 gün toplam 7 gün
            var chartLabels = new List<string>();
            var chartActiveData = new List<int>();
            var chartCompletedData = new List<int>();

            for (int i = 0; i < 7; i++)
            {
                var gun = baslangicTarihi.AddDays(i);
                chartLabels.Add(gun.ToString("dd MMM"));
                chartActiveData.Add(kullanicininGorevleri.Count(g => g.Tarih.Date == gun && g.DurumAktifMi));
                chartCompletedData.Add(kullanicininGorevleri.Count(g => g.Tarih.Date == gun && !g.DurumAktifMi));
            }

            ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(chartLabels);
            ViewBag.ChartActiveData = System.Text.Json.JsonSerializer.Serialize(chartActiveData);
            ViewBag.ChartCompletedData = System.Text.Json.JsonSerializer.Serialize(chartCompletedData);

            // 2. Pasta Grafiği: Aktif Görevlerin Öncelik Dağılımı
            var aktifGorevler = kullanicininGorevleri.Where(g => g.DurumAktifMi).ToList();
            var pieLabels = new List<string> { "Yüksek", "Orta", "Düşük" };
            var pieData = new List<int> {
                aktifGorevler.Count(g => g.Oncelik == "Yüksek"),
                aktifGorevler.Count(g => g.Oncelik == "Orta"),
                aktifGorevler.Count(g => g.Oncelik == "Düşük")
            };

            ViewBag.PieLabels = System.Text.Json.JsonSerializer.Serialize(pieLabels);
            ViewBag.PieData = System.Text.Json.JsonSerializer.Serialize(pieData);

            return View(kullanicininGorevleri);
        }
       
        // --- 2. SİSTEM LOGLARI ---
        public IActionResult Loglar()
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciId");
            var kullaniciRol = HttpContext.Session.GetInt32("KullaniciRol");

            if (kullaniciId == null) return RedirectToAction("Login", "Auth");

            if (kullaniciRol != (int)GorevTakipSistemi.Models.KullaniciRol.Owner)
            {
                TempData["Error"] = "Yetkisiz Erişim: Bu sayfayı sadece sistem kurucusu görüntüleyebilir!";
                return RedirectToAction("Index");
            }

            var sistemLoglari = _context.SistemLoglari
                                        .OrderByDescending(l => l.IslemTarihi)
                                        .Take(100)
                                        .ToList();

            return View(sistemLoglari);
        }
        

        // --- 3. PROFİL SAYFASINI GÖRÜNTÜLE (GET) ---
        [HttpGet]
        public IActionResult Profil()
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciId");
            if (kullaniciId == null) return RedirectToAction("Login", "Auth");

            var kullanici = _context.Kullanicilar.Find(kullaniciId);
            if (kullanici == null) return RedirectToAction("Login", "Auth");

            return View(kullanici);
        }

        // --- 4. PROFİL SAYFASINDA BİLGİ GÜNCELLEME (POST) ---
        [HttpPost]
        public IActionResult Profil(string? Email, string? KullaniciAdi, string? eskisifre, string? yenisifre, string? yenisifretekrar)
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciId");
            if (kullaniciId == null) return RedirectToAction("Login", "Auth");

            var kullanici = _context.Kullanicilar.Find(kullaniciId);
            if (kullanici == null) return RedirectToAction("Login", "Auth");

            bool degisiklikYapildi = false;

            // ==========================================
            // 1. KİŞİSEL BİLGİ GÜNCELLEME KONTROLÜ
            // ==========================================
            if (!string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(KullaniciAdi))
            {
                // Eğer farklı bir kullanıcı bu e-posta veya kullanıcı adını kullanmıyorsa güncelle
                bool emailKullaniliyor = _context.Kullanicilar.Any(k => k.Email == Email && k.Id != kullaniciId);
                bool kullaniciAdiKullaniliyor = _context.Kullanicilar.Any(k => k.KullaniciAdi == KullaniciAdi && k.Id != kullaniciId);

                if (emailKullaniliyor)
                {
                    TempData["Error"] = "Bu E-Posta adresi başka bir hesap tarafından kullanılıyor.";
                    return RedirectToAction("Profil");
                }
                
                if (kullaniciAdiKullaniliyor)
                {
                    TempData["Error"] = "Bu Kullanıcı Adı alınmış. Lütfen başka bir tane deneyin.";
                    return RedirectToAction("Profil");
                }

                if (kullanici.Email != Email || kullanici.KullaniciAdi != KullaniciAdi)
                {
                    kullanici.Email = Email;
                    kullanici.KullaniciAdi = KullaniciAdi;
                    
                    // Session'ı da güncelle ki sağ üst köşedeki isim değişsin
                    HttpContext.Session.SetString("KullaniciAdSoyad", $"{kullanici.Ad} {kullanici.Soyad}");
                    
                    degisiklikYapildi = true;
                }
            }

            // ==========================================
            // 2. ŞİFRE GÜNCELLEME KONTROLÜ
            // ==========================================
            if (!string.IsNullOrEmpty(eskisifre) || !string.IsNullOrEmpty(yenisifre) || !string.IsNullOrEmpty(yenisifretekrar))
            {
                if (string.IsNullOrEmpty(eskisifre) || string.IsNullOrEmpty(yenisifre) || string.IsNullOrEmpty(yenisifretekrar))
                {
                    TempData["Error"] = "Şifre değiştirmek için lütfen tüm şifre alanlarını eksiksiz doldurun!";
                    return RedirectToAction("Profil");
                }

                if (yenisifre != yenisifretekrar)
                {
                    TempData["Error"] = "Yeni şifreler birbiriyle uyuşmuyor!";
                    return RedirectToAction("Profil");
                }

                if (!GecerliSifreMi(yenisifre))
                {
                    TempData["Error"] = "Yeni şifreniz en az 8 karakter olmalı; büyük/küçük harf, rakam ve özel karakter (?, @, !, #, %, +, -, *) içermelidir.";
                    return RedirectToAction("Profil");
                }

                var eskiSifreBytes = System.Text.Encoding.UTF8.GetBytes(eskisifre);
                string eskiSifreHash = Convert.ToBase64String(eskiSifreBytes);

                if (kullanici.SifreHash != eskiSifreHash)
                {
                    TempData["Error"] = "Mevcut (eski) şifrenizi yanlış girdiniz!";
                    return RedirectToAction("Profil");
                }

                var yeniSifreBytes = System.Text.Encoding.UTF8.GetBytes(yenisifre);
                kullanici.SifreHash = Convert.ToBase64String(yeniSifreBytes);

                _context.SistemLoglari.Add(new SistemLog {
                    KullaniciAdi = kullanici.KullaniciAdi,
                    YapilanIslem = "Profil panelinden şifresini güncelledi.",
                    IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    IslemTarihi = DateTime.Now
                });

                degisiklikYapildi = true;
            }

            // ==========================================
            // SONUÇLARI KAYDET
            // ==========================================
            if (degisiklikYapildi)
            {
                _context.SaveChanges();
                TempData["Success"] = "Profil bilgileriniz başarıyla güncellendi.";
            }

            return RedirectToAction("Profil");
        }

        private bool GecerliSifreMi(string sifre)
        {
            if (string.IsNullOrEmpty(sifre) || sifre.Length < 8) return false;
            if (!sifre.Any(char.IsUpper)) return false;
            if (!sifre.Any(char.IsLower)) return false;
            if (!sifre.Any(char.IsDigit)) return false;
            
            char[] ozelKarakterler = { '?', '@', '!', '#', '%', '+', '-', '*' };
            if (!sifre.Any(c => ozelKarakterler.Contains(c))) return false;

            return true;
        }
    }
}