using Microsoft.AspNetCore.Mvc;
using GorevTakipSistemi.Data;
using GorevTakipSistemi.Models;
using GorevTakipSistemi.Filters;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using GorevTakipSistemi.Hubs;
using Microsoft.AspNetCore.RateLimiting;

namespace GorevTakipSistemi.Controllers
{
    [YetkiKontrol] // Yetkilendirme filtresi: Oturum acmamis kullanicilar bu controller'a erisemez
    public class GorevController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<BildirimHub> _hubContext;

        /// <summary>
        /// GorevController sinifinin yapilandirici metodu.
        /// Veritabani baglami ve SignalR hub baglamini bagimlilik enjeksiyonu ile alir.
        /// </summary>
        /// <param name="context">Uygulama veritabani baglami.</param>
        /// <param name="hubContext">Anlik bildirim gondermek icin kullanilan SignalR hub baglami.</param>
        public GorevController(AppDbContext context, IHubContext<BildirimHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // ===== GOREV LISTELEME =====

        /// <summary>
        /// Kullaniciya ait veya kullanicinin atadigi tum gorevleri listeler.
        /// Arama parametresi verilmisse gorev adina gore filtreleme uygular.
        /// Sonuclar once aktiflik durumuna, ardindan tarihe gore siralanir.
        /// </summary>
        /// <param name="arama">Gorev adina gore filtreleme icin arama metni.</param>
        /// <returns>Gorev listesini iceren Index gorunumu.</returns>
        public IActionResult Index(string arama)
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;

            // Kullaniciya ait veya kullanicinin atadigi gorevleri iliskili verileriyle birlikte sorgula
            var query = _context.Gorevler
                                .Include(g => g.Kullanici)
                                .Include(g => g.AtayanKullanici)
                                .Where(g => g.KullaniciId == kullaniciId || g.AtayanKullaniciId == kullaniciId);

            // Arama metni mevcutsa gorev adina gore filtre uygula
            if (!string.IsNullOrEmpty(arama))
            {
                query = query.Where(x => x.GorevAdi.Contains(arama));
            }

            // Aktif gorevler uste gelecek sekilde siralanir, ayni durumdakiler tarihe gore dizilir
            var gorevler = query.OrderByDescending(g => g.DurumAktifMi)
                                .ThenBy(g => g.Tarih)
                                .ToList();

            return View("Index", gorevler);
        }

        /// <summary>
        /// Bugune ait ve gecikmeye ugramis aktif gorevleri listeler.
        /// Yalnizca tarihi bugunun tarihine esit veya oncesinde olan aktif gorevler getirilir.
        /// </summary>
        /// <returns>Aktif gorevleri iceren Index gorunumu.</returns>
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

        /// <summary>
        /// Tamamlanmis gorevleri listeler.
        /// Durum aktif olmayan (tamamlanmis) gorevler tarihe gore azalan sirada getirilir.
        /// </summary>
        /// <returns>Tamamlanan gorevleri iceren Index gorunumu.</returns>
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

        /// <summary>
        /// Henuz baslama tarihi gelmemis bekleyen gorevleri listeler.
        /// Yalnizca tarihi gelecekte olan aktif gorevler getirilir.
        /// </summary>
        /// <returns>Bekleyen gorevleri iceren Index gorunumu.</returns>
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

        // ===== GOREV OLUSTURMA =====

        /// <summary>
        /// Yeni gorev ekleme formunu goruntuleyen GET metodu.
        /// Kullanicinin erisebilecegi etiketleri ViewBag uzerinden gorunume aktarir.
        /// </summary>
        /// <returns>Gorev olusturma formu gorunumu.</returns>
        public IActionResult Create()
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            
            // Kullanicinin kisisel veya uye oldugu ekiplere ait etiketleri getir
            ViewBag.Etiketler = _context.Etiketler.Where(e => e.EkipId == null || _context.EkipUyeleri.Any(eu => eu.KullaniciId == kullaniciId && eu.EkipId == e.EkipId)).ToList();

            return View();
        }

        // ===== ETIKET YONETIMI =====

        /// <summary>
        /// AJAX uzerinden yeni etiket olusturur.
        /// Etiket adi bos olamaz; varsayilan renk olarak indigo (#4f46e5) atanir.
        /// </summary>
        /// <param name="ad">Olusturulacak etiketin adi.</param>
        /// <param name="renkHex">Etiketin goruntulenecegi HEX renk kodu.</param>
        /// <returns>Basari durumu ve olusturulan etiket bilgilerini iceren JSON yaniti.</returns>
        [HttpPost]
        public IActionResult EtiketEkle(string ad, string renkHex)
        {
            if (string.IsNullOrWhiteSpace(ad)) return Json(new { success = false, message = "Etiket adı boş olamaz!" });
            
            var me = HttpContext.Session.GetInt32("KullaniciId") ?? 0;

            var yeniEtiket = new Etiket
            {
                Ad = ad,
                RenkHex = renkHex ?? "#4f46e5",
            };

            _context.Etiketler.Add(yeniEtiket);
            _context.SaveChanges();

            return Json(new { success = true, data = new { id = yeniEtiket.Id, ad = yeniEtiket.Ad, renkHex = yeniEtiket.RenkHex } });
        }

        // ===== ALT GOREV YONETIMI =====

        /// <summary>
        /// Belirtilen goreve AJAX uzerinden yeni bir alt gorev ekler.
        /// Alt gorev basligi bos olamaz; varsayilan olarak tamamlanmamis durumda olusturulur.
        /// </summary>
        /// <param name="gorevId">Alt gorevin bagli olacagi ana gorev kimlik numarasi.</param>
        /// <param name="baslik">Alt gorevin basligi.</param>
        /// <returns>Basari durumu ve olusturulan alt gorev bilgilerini iceren JSON yaniti.</returns>
        [HttpPost]
        public IActionResult AltGorevEkle(int gorevId, string baslik)
        {
            if (string.IsNullOrWhiteSpace(baslik)) return Json(new { success = false, message = "Başlık boş olamaz!" });
            
            var altGorev = new AltGorev
            {
                GorevId = gorevId,
                Baslik = baslik,
                TamamlandiMi = false
            };
            
            _context.AltGorevler.Add(altGorev);
            _context.SaveChanges();
            
            return Json(new { success = true, data = new { id = altGorev.Id, baslik = altGorev.Baslik } });
        }

        /// <summary>
        /// Alt gorevin tamamlanma durumunu AJAX uzerinden degistirir.
        /// Ekip gorevlerinde her kullanici kendi tamamlama kaydini olusturur veya siler.
        /// Kisisel gorevlerde dogrudan alt gorevin durumu guncellenir.
        /// </summary>
        /// <param name="id">Durumu degistirilecek alt gorev kimlik numarasi.</param>
        /// <param name="tamamlandiMi">Alt gorevin yeni tamamlanma durumu.</param>
        /// <returns>Islem sonucunu ve bagli gorev kimligini iceren JSON yaniti.</returns>
        [HttpPost]
        public IActionResult AltGorevDurumDegistir(int id, bool tamamlandiMi)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            if (userId == null) return Json(new { success = false, message = "Oturum süresi dolmuş." });

            var altGorev = _context.AltGorevler.Include(a => a.Gorev).FirstOrDefault(a => a.Id == id);
            if(altGorev != null)
            {
                if (altGorev.Gorev.EkipId != null)
                {
                    // Ekip gorevlerinde kullaniciya ozel tamamlama kaydi tutulur
                    var tamamlama = _context.AltGorevTamamlamalari.FirstOrDefault(t => t.AltGorevId == id && t.KullaniciId == userId.Value);
                    if (tamamlandiMi && tamamlama == null)
                    {
                        _context.AltGorevTamamlamalari.Add(new AltGorevTamamlama { AltGorevId = id, KullaniciId = userId.Value });
                    }
                    else if (!tamamlandiMi && tamamlama != null)
                    {
                        _context.AltGorevTamamlamalari.Remove(tamamlama);
                    }
                    _context.SaveChanges();
                }
                else
                {
                    // Kisisel gorevlerde tamamlanma durumu dogrudan guncellenir
                    altGorev.TamamlandiMi = tamamlandiMi;
                    _context.SaveChanges();
                }
                
                return Json(new { success = true, gorevId = altGorev.GorevId });
            }
            return Json(new { success = false });
        }

        /// <summary>
        /// Yeni gorev olusturma islemini gerceklestiren POST metodu.
        /// XSS saldirilarini engellemek icin giris verilerini dogrular, gorev limitini kontrol eder,
        /// alt gorevleri kaydeder, bildirim gonderir ve sistem loguna yazar.
        /// </summary>
        /// <param name="gorev">Olusturulacak gorev modeli.</param>
        /// <param name="seciliEtiketler">Goreve atanacak etiketlerin kimlik listesi.</param>
        /// <param name="altGorevler">Goreve eklenecek alt gorev basliklarinin listesi.</param>
        /// <returns>Basarili ise gorev listesine yonlendirir, basarisiz ise formu hata mesajiyla geri dondurur.</returns>
        [HttpPost]
        public IActionResult Create(Gorev gorev, List<int> seciliEtiketler, List<string> altGorevler)
        {
            // XSS korumasi: HTML, JavaScript veya CSS enjeksiyonunu engelleyen desen kontrolu
            string zararliKodDeseni = @"<[^>]+>"; 
            if (Regex.IsMatch(gorev.GorevAdi ?? "", zararliKodDeseni) || Regex.IsMatch(gorev.Aciklama ?? "", zararliKodDeseni))
            {
                TempData["Error"] = "Güvenlik İhlali: Görev bilgilerinde HTML, JS veya CSS kodları kullanılamaz!";
                return View(gorev);
            }

            int me = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            
            // Kisisel gorev limiti kontrolu: Kullanici en fazla 20 kisisel gorev olusturabilir
            int currentTaskCount = _context.Gorevler.Count(g => g.KullaniciId == me && g.EkipId == null);
            if (currentTaskCount >= 20)
            {
                TempData["Error"] = "Limit Aşıldı: Sistemde en fazla 20 adet kişisel görev barındırabilirsiniz. Yeni görev ekleyebilmek için lütfen eski veya tamamlanmış görevlerinizi silin.";
                return View(gorev);
            }

            // Gorev sahipligini oturumdaki kullaniciya ata
            gorev.KullaniciId = me;
            gorev.AtayanKullaniciId = null;
            gorev.DurumAktifMi = true; // Yeni olusturulan gorev varsayilan olarak aktif durumda baslar

            _context.Gorevler.Add(gorev);
            _context.SaveChanges();

            // Alt gorevleri ana goreve baglayarak veritabanina kaydet
            if (altGorevler != null && altGorevler.Any())
            {
                foreach (var baslik in altGorevler)
                {
                    if (!string.IsNullOrWhiteSpace(baslik))
                    {
                        _context.AltGorevler.Add(new AltGorev
                        {
                            GorevId = gorev.Id,
                            Baslik = baslik.Trim(),
                            TamamlandiMi = false
                        });
                    }
                }
                _context.SaveChanges();
            }

            // Gorev baska bir kullaniciya atanmissa, atanan kullaniciya bildirim gonder
            if (gorev.KullaniciId != me)
            {
                string adSoyad = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Biri";
                _context.Bildirimler.Add(new Bildirim {
                    KullaniciId = gorev.KullaniciId,
                    Mesaj = $"{adSoyad} sana '{gorev.GorevAdi}' görevini atadı.",
                    Url = $"/Gorev/Details/{gorev.Id}"
                });
                _context.SaveChanges();
                
                // SignalR uzerinden hedef kullaniciya anlik bildirim gonder
                _hubContext.Clients.Group(gorev.KullaniciId.ToString()).SendAsync("YeniBildirim", "Yeni Görev!", $"{adSoyad} sana '{gorev.GorevAdi}' görevini atadı.", "info", $"/Gorev/Details/{gorev.Id}");
            }

            // Gorev bir ekibe aitse, ekip aktivite loguna kayit ekle
            if (gorev.EkipId.HasValue && gorev.EkipId.Value > 0)
            {
                string adSoyad = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Biri";
                _context.EkipAktiviteleri.Add(new EkipAktivite {
                    EkipId = gorev.EkipId.Value,
                    KullaniciId = me,
                    Aksiyon = "Oluşturdu",
                    Mesaj = $"'{gorev.GorevAdi}' adlı görevi ekibe atadı."
                });
                _context.SaveChanges();
            }

            // Sistem loglama: Gorev olusturma islemini kayit altina al
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

        // ===== GOREV ATAMA =====

        /// <summary>
        /// Ekip gorevini baska bir kullaniciya atar. Yalnizca ekip kurucusu bu islemi yapabilir.
        /// Atama sonrasinda hedef kullaniciya veritabani bildirimi ve SignalR anlik bildirimi gonderilir.
        /// </summary>
        /// <param name="gorevId">Atanacak gorevin kimlik numarasi.</param>
        /// <param name="yeniKullaniciId">Gorevin atanacagi kullanicinin kimlik numarasi.</param>
        /// <returns>Islem sonucunu iceren JSON yaniti.</returns>
        [HttpPost]
        public IActionResult GorevAta(int gorevId, int yeniKullaniciId)
        {
            var me = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var gorev = _context.Gorevler.FirstOrDefault(g => g.Id == gorevId && g.EkipId != null);
            if(gorev == null) return Json(new { success = false, message = "Görev bulunamadı!" });

            // Yetki kontrolu: Islemi yapan kullanicinin ekip kurucusu olup olmadigini dogrula
            var ekip = _context.Ekipler.FirstOrDefault(e => e.Id == gorev.EkipId);
            if(ekip == null || ekip.KurucuId != me) {
                return Json(new { success = false, message = "Sadece ekip kurucusu görev ataması yapabilir!" });
            }

            gorev.KullaniciId = yeniKullaniciId;
            gorev.AtayanKullaniciId = me;
            _context.SaveChanges();

            // Atanan kullaniciya veritabani bildirimi olustur
            var adSoyad = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Yönetici";
            _context.Bildirimler.Add(new Bildirim {
                KullaniciId = yeniKullaniciId,
                Mesaj = $"{adSoyad} sana '{gorev.GorevAdi}' görevini atadı.",
                Url = $"/Ekip/Detay/{gorev.EkipId}"
            });
            _context.SaveChanges();

            // SignalR uzerinden hedef kullaniciya anlik bildirim gonder
            _hubContext.Clients.Group(yeniKullaniciId.ToString()).SendAsync("YeniBildirim", "Yeni Görev Ataması!", $"{adSoyad} sana '{gorev.GorevAdi}' görevini atadı.", "info", $"/Ekip/Detay/{gorev.EkipId}");

            return Json(new { success = true });
        }

        // ===== GOREV DETAY =====

        /// <summary>
        /// Belirtilen gorevin detay bilgilerini goruntuleyen sayfa.
        /// Yalnizca gorevin sahibi olan kullanici bu sayfaya erisebilir.
        /// </summary>
        /// <param name="id">Goruntulenmek istenen gorevin kimlik numarasi.</param>
        /// <returns>Gorev detayini iceren gorunum veya 404 hata sayfasi.</returns>
        public IActionResult Details(int id)
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var gorev = _context.Gorevler.FirstOrDefault(g => g.Id == id && g.KullaniciId == kullaniciId);
            
            if (gorev == null) return NotFound("Görev bulunamadı!");

            return View(gorev);
        }

        // ===== GOREV DUZENLEME =====

        /// <summary>
        /// Gorev duzenleme formunu goruntuleyen GET metodu.
        /// Yalnizca gorevin sahibi olan kullanici duzenleme yapabilir.
        /// </summary>
        /// <param name="id">Duzenlenecek gorevin kimlik numarasi.</param>
        /// <returns>Gorev duzenleme formu gorunumu veya 404 hata sayfasi.</returns>
        public IActionResult Edit(int id)
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var gorev = _context.Gorevler.FirstOrDefault(g => g.Id == id && g.KullaniciId == kullaniciId);
            
            if (gorev == null) return NotFound("Düzenlenecek görev bulunamadı!");

            return View(gorev);
        }

        /// <summary>
        /// Gorev guncelleme islemini gerceklestiren POST metodu.
        /// XSS saldirilarini engellemek icin giris dogrulamasi yapar,
        /// gorev bilgilerini gunceller ve sistem loguna yazar.
        /// </summary>
        /// <param name="guncelGorev">Guncellenmis gorev bilgilerini iceren model.</param>
        /// <returns>Basarili ise gorev listesine yonlendirir.</returns>
        [HttpPost]
        public IActionResult Edit(Gorev guncelGorev)
        {
            // XSS korumasi: HTML, JavaScript veya CSS enjeksiyonunu engelleyen desen kontrolu
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

                // Sistem loglama: Gorev guncelleme islemini kayit altina al
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

        // ===== GOREV TAMAMLAMA =====

        /// <summary>
        /// Belirtilen gorevi tamamlanmis olarak isaretler.
        /// Gorev bir ekibe aitse ekip aktivite loguna kayit ekler.
        /// Islem sistem loguna yazilir.
        /// </summary>
        /// <param name="id">Tamamlanacak gorevin kimlik numarasi.</param>
        /// <returns>Gorev listesine yonlendirme.</returns>
        public IActionResult Tamamla(int id)
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var gorev = _context.Gorevler.FirstOrDefault(g => g.Id == id && g.KullaniciId == kullaniciId);

            if (gorev != null)
            {
                gorev.DurumAktifMi = false; 

                // Gorev bir ekibe aitse, ekip aktivite loguna tamamlama kaydini ekle
                if (gorev.EkipId.HasValue && gorev.EkipId.Value > 0)
                {
                    _context.EkipAktiviteleri.Add(new EkipAktivite {
                        EkipId = gorev.EkipId.Value,
                        KullaniciId = kullaniciId,
                        Aksiyon = "Tamamladı",
                        Mesaj = $"'{gorev.GorevAdi}' adlı görevi tamamladı."
                    });
                }

                // Sistem loglama: Gorev tamamlama islemini kayit altina al
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

        // ===== GOREV SILME =====

        /// <summary>
        /// Belirtilen gorevi veritabanindan kalici olarak siler.
        /// Silme islemi sistem loguna kaydedilir.
        /// </summary>
        /// <param name="id">Silinecek gorevin kimlik numarasi.</param>
        /// <param name="sayfa">Silme isleminden sonra yonlendirilecek sayfa adi. Varsayilan: Index.</param>
        /// <returns>Belirtilen sayfaya yonlendirme.</returns>
        public IActionResult Delete(int id, string sayfa = "Index")
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var gorev = _context.Gorevler.FirstOrDefault(g => g.Id == id && g.KullaniciId == kullaniciId);
            
            if (gorev != null)
            {
                // Sistem loglama: Gorev silme islemini kayit altina al
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

        // ===== GOREV DURUM DEGISTIRME =====

        /// <summary>
        /// Gorevin aktiflik durumunu tersine cevirir (aktif ise pasif, pasif ise aktif yapar).
        /// Islem sonrasinda kullaniciyi onceki sayfasina yonlendirir.
        /// </summary>
        /// <param name="id">Durumu degistirilecek gorevin kimlik numarasi.</param>
        /// <returns>Onceki sayfaya veya gorev listesine yonlendirme.</returns>
        public IActionResult DurumDegistir(int id)
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var gorev = _context.Gorevler.FirstOrDefault(g => g.Id == id && g.KullaniciId == kullaniciId);
            if (gorev != null)
            {
                gorev.DurumAktifMi = !gorev.DurumAktifMi;
                _context.SaveChanges();
            }
            
            // Kullaniciyi istek yapilan onceki sayfaya yonlendir
            string oncekiSayfa = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(oncekiSayfa))
            {
                return Redirect(oncekiSayfa);
            }
            
            return RedirectToAction("Index");
        }

        // ===== GOREV DETAY - AJAX =====

        /// <summary>
        /// AJAX istekleri icin gorev detaylarini Partial View olarak dondurur.
        /// Guvenlik kontrolu uygulanir: Yalnizca gorevin sahibi, atayan veya ekip uyesi erisebilir.
        /// Alt gorevler ve etiketler dahil iliskili tum veriler yuklenir.
        /// </summary>
        /// <param name="id">Detaylari getirilecek gorevin kimlik numarasi.</param>
        /// <returns>Gorev detayini iceren Partial View veya 404 hata mesaji.</returns>
        public IActionResult DetayGetir(int id)
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            
            // Yetki kontrolu: Gorev sahibi, atayan veya ekip uyesi mi dogrula
            var gorev = _context.Gorevler
                                .Include(g => g.AltGorevler)
                                .Include(g => g.GorevEtiketleri).ThenInclude(ge => ge.Etiket)
                                .FirstOrDefault(g => g.Id == id && (g.KullaniciId == kullaniciId || g.AtayanKullaniciId == kullaniciId || _context.EkipUyeleri.Any(eu => eu.EkipId == g.EkipId && eu.KullaniciId == kullaniciId)));
            
            if (gorev == null) 
            {
                return NotFound("<div class='p-4 text-center text-red-500 font-bold'>Bu görevi görüntülemeye yetkiniz yok veya görev bulunamadı!</div>");
            }
            
            return PartialView("_GorevDetayPartial", gorev);
        }

        // ===== TAKVIM =====

        /// <summary>
        /// Takvim gorunum sayfasini goruntuleyen metot.
        /// Gorevler takvim uzerinde gorsel olarak sunulur.
        /// </summary>
        /// <returns>Takvim gorunumu.</returns>
        public IActionResult Takvim()
        {
            return View();
        }

        /// <summary>
        /// Takvim bileseni icin kullanicinin gorevlerini JSON formatinda dondurur.
        /// Her gorev icin renk kodlamasi uygulanir: tamamlanmis gorevler yesil, yuksek oncelikli gorevler kirmizi, diger gorevler indigo.
        /// </summary>
        /// <returns>Gorev listesini iceren JSON yaniti.</returns>
        [HttpGet]
        public JsonResult GetirGorevlerJSON()
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;

            var gorevler = _context.Gorevler
                                   .Where(g => g.KullaniciId == kullaniciId || g.AtayanKullaniciId == kullaniciId)
                                   .Select(g => new
                                   {
                                       id = g.Id,
                                       title = g.GorevAdi,
                                       start = g.Tarih.ToString("yyyy-MM-dd"),
                                       allDay = true,
                                       // Renk kodlamasi: Tamamlanmis=yesil, Yuksek oncelik=kirmizi, Diger=indigo
                                       color = !g.DurumAktifMi ? "#10b981" : (g.Oncelik == "Yüksek" ? "#ef4444" : "#4f46e5"),
                                       url = $"/Gorev/Detay/{g.Id}"
                                   })
                                   .ToList();

            return Json(gorevler);
        }

        // ===== TAKVIM TARIH GUNCELLEME =====

        /// <summary>
        /// Takvim uzerinde surukle-birak islemiyle gorevin tarihini gunceller.
        /// Gorevin mevcut saat bilgisi korunarak yalnizca tarih kismi degistirilir.
        /// </summary>
        /// <param name="id">Tarihi guncellenecek gorevin kimlik numarasi.</param>
        /// <param name="yeniTarih">Gorevin yeni tarihi (string formatinda).</param>
        /// <returns>Islem sonucunu iceren JSON yaniti.</returns>
        [HttpPost]
        public IActionResult GuncelleGorevTarih(int id, string yeniTarih)
        {
            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var gorev = _context.Gorevler.FirstOrDefault(g => g.Id == id && (g.KullaniciId == kullaniciId || g.AtayanKullaniciId == kullaniciId));
            
            if (gorev == null)
            {
                return Json(new { success = false, message = "Görev bulunamadı veya yetkiniz yok." });
            }

            if (DateTime.TryParse(yeniTarih, out DateTime parsedDate))
            {
                // Mevcut saat bilgisini koruyarak yalnizca tarih kismini guncelle
                gorev.Tarih = new DateTime(parsedDate.Year, parsedDate.Month, parsedDate.Day, gorev.Tarih.Hour, gorev.Tarih.Minute, gorev.Tarih.Second);
                _context.SaveChanges();
                return Json(new { success = true, message = "Görev tarihi güncellendi." });
            }

            return Json(new { success = false, message = "Geçersiz tarih formatı." });
        }
    }
}