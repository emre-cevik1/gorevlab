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
        // ===== BAKIM MODU =====

        /// <summary>
        /// Bakim modu sayfasini goruntuleyen metot.
        /// Bakim modu aktif degilse kullaniciyi ana sayfaya yonlendirir.
        /// </summary>
        /// <returns>Bakim sayfasi gorunumu veya ana sayfaya yonlendirme.</returns>
        [HttpGet]
        public IActionResult Bakim()
        {
            if (!GorevTakipSistemi.Models.SiteSettings.BakimModuAktif)
                return RedirectToAction("Index"); // Bakim modu kapali ise ana sayfaya yonlendir

            return View();
        }

        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        /// <summary>
        /// HomeController sinifinin yapilandirici metodu.
        /// Veritabani baglami ve web barindirma ortam bilgisini bagimlilik enjeksiyonu ile alir.
        /// </summary>
        /// <param name="context">Uygulama veritabani baglami.</param>
        /// <param name="env">Web barindirma ortam bilgisi.</param>
        public HomeController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ===== ANA SAYFA =====

        /// <summary>
        /// Uygulamanin ana sayfasini goruntuleyen metot.
        /// Oturum acilmamissa kullaniciyi giris sayfasina yonlendirir.
        /// </summary>
        /// <returns>Ana sayfa gorunumu veya giris sayfasina yonlendirme.</returns>
        public IActionResult Index()
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciId");
            if (kullaniciId == null) return RedirectToAction("Login", "Auth");
            
            return View();
        }

        // ===== KONTROL PANELI =====

        /// <summary>
        /// Kullaniciya ozel gelismis istatistik ve grafik verileri iceren kontrol panelini goruntuleyen metot.
        /// Kisisel gorevler, ekip gorevleri, durum dagilimi, cizgi grafigi ve pasta grafigi verilerini hesaplar.
        /// </summary>
        /// <returns>Kontrol paneli gorunumu veya giris sayfasina yonlendirme.</returns>
        public IActionResult Dashboard()
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciId");
            if (kullaniciId == null) return RedirectToAction("Login", "Auth");

            // Kullanicinin kisisel gorevlerini alt gorevler ve etiketleriyle birlikte getir
            var kullanicininGorevleri = _context.Gorevler
                                        .Include(g => g.AltGorevler)
                                        .Include(g => g.GorevEtiketleri).ThenInclude(ge => ge.Etiket)
                                        .Where(g => g.KullaniciId == kullaniciId && g.EkipId == null)
                                        .ToList();

            // Kullanicinin uye oldugu ekiplerin kimlik numaralarini getir
            var ekipIdleri = _context.EkipUyeleri
                                     .Where(u => u.KullaniciId == kullaniciId)
                                     .Select(u => u.EkipId)
                                     .ToList();

            // Kullanicinin ekiplerine ait aktif gorevleri tarihe gore siralanmis olarak getir
            var ekipGorevleri = _context.Gorevler
                                        .Include(g => g.Ekip)
                                        .Include(g => g.AltGorevler)
                                        .Include(g => g.GorevEtiketleri).ThenInclude(ge => ge.Etiket)
                                        .Where(g => g.EkipId != null && ekipIdleri.Contains(g.EkipId.Value) && g.DurumAktifMi)
                                        .OrderBy(g => g.Tarih)
                                        .ToList();

            // Gorev durum istatistiklerini hesapla
            ViewBag.AktifSayisi = kullanicininGorevleri.Where(g => g.DurumAktifMi).Count();
            ViewBag.TamamlananSayisi = kullanicininGorevleri.Where(g => !g.DurumAktifMi).Count();
            ViewBag.BekleyenSayisi = kullanicininGorevleri.Where(g => g.DurumAktifMi && g.Tarih.Date > DateTime.Now.Date).Count();
            ViewBag.BugunSayisi = kullanicininGorevleri.Where(g => g.DurumAktifMi && g.Tarih.Date == DateTime.Now.Date).Count();

            // Gunluk gorev ozeti: En yakin tarihli 5 gorevi getir
            ViewBag.GunlukGorevler = kullanicininGorevleri
                                    .OrderBy(g => g.Tarih)
                                    .Take(5)
                                    .ToList();
                                    
            ViewBag.EkipGorevleri = ekipGorevleri;

            // ===== GRAFIK VERILERI (CHART.JS) =====
            
            // Cizgi grafigi: Gecmis 3 gun ve gelecek 3 gun olmak uzere toplam 7 gunluk gorev dagilimi
            var baslangicTarihi = DateTime.Now.Date.AddDays(-3);
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

            // Pasta grafigi: Aktif gorevlerin oncelik seviyesine gore dagilimi
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

        // ===== SISTEM LOGLARI =====

        /// <summary>
        /// Sistem log kayitlarini goruntuleyen metot.
        /// Yalnizca Owner (sistem kurucusu) rolu bu sayfaya erisebilir.
        /// En son 100 log kaydi tarih sirasina gore azalan olarak listelenir.
        /// </summary>
        /// <returns>Sistem log listesi gorunumu veya yetkisiz erisimde ana sayfaya yonlendirme.</returns>
        public IActionResult Loglar()
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciId");
            var kullaniciRol = HttpContext.Session.GetInt32("KullaniciRol");

            if (kullaniciId == null) return RedirectToAction("Login", "Auth");

            // Yetki kontrolu: Yalnizca sistem kurucusu (Owner) log kayitlarini goruntuleyebilir
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
        
        // ===== PROFIL YONETIMI =====

        /// <summary>
        /// Kullanicinin profil bilgilerini goruntuleyen GET metodu.
        /// Oturum acilmamissa veya kullanici bulunamazsa giris sayfasina yonlendirir.
        /// </summary>
        /// <returns>Profil gorunumu veya giris sayfasina yonlendirme.</returns>
        [HttpGet]
        public IActionResult Profil()
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciId");
            if (kullaniciId == null) return RedirectToAction("Login", "Auth");

            var kullanici = _context.Kullanicilar.Find(kullaniciId);
            if (kullanici == null) return RedirectToAction("Login", "Auth");

            return View(kullanici);
        }

        /// <summary>
        /// Kullanicinin profil bilgilerini (e-posta, kullanici adi, sifre) guncelleyen POST metodu.
        /// E-posta ve kullanici adi benzersizlik kontrolu yapar.
        /// Sifre degisikligi icin mevcut sifre dogrulamasi ve guvenlik kriterleri uygulanir.
        /// </summary>
        /// <param name="Email">Yeni e-posta adresi.</param>
        /// <param name="KullaniciAdi">Yeni kullanici adi.</param>
        /// <param name="eskisifre">Mevcut sifre (sifre degisikligi icin).</param>
        /// <param name="yenisifre">Yeni sifre.</param>
        /// <param name="yenisifretekrar">Yeni sifre tekrari (dogrulama icin).</param>
        /// <returns>Profil sayfasina yonlendirme.</returns>
        [HttpPost]
        public IActionResult Profil(string? Email, string? KullaniciAdi, string? eskisifre, string? yenisifre, string? yenisifretekrar)
        {
            var kullaniciId = HttpContext.Session.GetInt32("KullaniciId");
            if (kullaniciId == null) return RedirectToAction("Login", "Auth");

            var kullanici = _context.Kullanicilar.Find(kullaniciId);
            if (kullanici == null) return RedirectToAction("Login", "Auth");

            bool degisiklikYapildi = false;

            // ===== KISISEL BILGI GUNCELLEME =====
            if (!string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(KullaniciAdi))
            {
                // Baska bir kullanici tarafindan ayni e-posta veya kullanici adi kullanilip kullanilmadigini kontrol et
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
                    
                    // Oturum bilgisini guncelle: Arayuzdeki kullanici adi degisikliginin aninda yansimasi icin
                    HttpContext.Session.SetString("KullaniciAdSoyad", $"{kullanici.Ad} {kullanici.Soyad}");
                    
                    degisiklikYapildi = true;
                }
            }

            // ===== SIFRE GUNCELLEME =====
            if (!string.IsNullOrEmpty(eskisifre) || !string.IsNullOrEmpty(yenisifre) || !string.IsNullOrEmpty(yenisifretekrar))
            {
                // Tum sifre alanlarinin doldurulmus olmasini zorunlu kil
                if (string.IsNullOrEmpty(eskisifre) || string.IsNullOrEmpty(yenisifre) || string.IsNullOrEmpty(yenisifretekrar))
                {
                    TempData["Error"] = "Şifre değiştirmek için lütfen tüm şifre alanlarını eksiksiz doldurun!";
                    return RedirectToAction("Profil");
                }

                // Yeni sifre ve tekrarinin birbiriyle eslesip eslesmedigini dogrula
                if (yenisifre != yenisifretekrar)
                {
                    TempData["Error"] = "Yeni şifreler birbiriyle uyuşmuyor!";
                    return RedirectToAction("Profil");
                }

                // Sifre guvenlik kriterlerini kontrol et
                if (!GecerliSifreMi(yenisifre))
                {
                    TempData["Error"] = "Yeni şifreniz en az 8 karakter olmalı; büyük/küçük harf, rakam ve özel karakter (?, @, !, #, %, +, -, *) içermelidir.";
                    return RedirectToAction("Profil");
                }

                // Mevcut sifreyi hash'leyerek veritabanindaki kayitla karsilastir
                var eskiSifreBytes = System.Text.Encoding.UTF8.GetBytes(eskisifre);
                string eskiSifreHash = Convert.ToBase64String(eskiSifreBytes);

                if (kullanici.SifreHash != eskiSifreHash)
                {
                    TempData["Error"] = "Mevcut (eski) şifrenizi yanlış girdiniz!";
                    return RedirectToAction("Profil");
                }

                // Yeni sifreyi hash'leyerek veritabanina kaydet
                var yeniSifreBytes = System.Text.Encoding.UTF8.GetBytes(yenisifre);
                kullanici.SifreHash = Convert.ToBase64String(yeniSifreBytes);

                // Sistem loglama: Sifre degisikligi islemini kayit altina al
                _context.SistemLoglari.Add(new SistemLog {
                    KullaniciAdi = kullanici.KullaniciAdi,
                    YapilanIslem = "Profil panelinden şifresini güncelledi.",
                    IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    IslemTarihi = DateTime.Now
                });

                degisiklikYapildi = true;
            }

            // ===== DEGISIKLIKLERI KAYDET =====
            if (degisiklikYapildi)
            {
                _context.SaveChanges();
                TempData["Success"] = "Profil bilgileriniz başarıyla güncellendi.";
            }

            return RedirectToAction("Profil");
        }

        /// <summary>
        /// Verilen sifrenin guvenlik kriterlerini karsilayip karsilamadigini denetleyen yardimci metot.
        /// Sifre en az 8 karakter uzunlugunda olmali, buyuk harf, kucuk harf, rakam ve
        /// ozel karakter (?, @, !, #, %, +, -, *) icermelidir.
        /// </summary>
        /// <param name="sifre">Dogrulanacak sifre metni.</param>
        /// <returns>Sifre tum kriterleri karsiliyorsa true, aksi halde false dondurur.</returns>
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

        // ===== BILGI SAYFALARI =====

        /// <summary>
        /// Hakkimizda sayfasini goruntuleyen metot.
        /// Uygulama hakkinda genel bilgi ve tanitim icerigi sunar.
        /// </summary>
        /// <returns>Hakkimizda gorunumu.</returns>
        public IActionResult About()
        {
            return View();
        }

        /// <summary>
        /// Gizlilik politikasi sayfasini goruntuleyen metot.
        /// Kullanici verilerinin islenmesine iliskin gizlilik politikasi bilgilerini sunar.
        /// </summary>
        /// <returns>Gizlilik politikasi gorunumu.</returns>
        public IActionResult Privacy()
        {
            return View();
        }
    }
}