using Microsoft.AspNetCore.Mvc;
using GorevTakipSistemi.Data;
using GorevTakipSistemi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using GorevTakipSistemi.Hubs;
using Microsoft.AspNetCore.RateLimiting;

namespace GorevTakipSistemi.Controllers
{
    public class EkipController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<BildirimHub> _hubContext;

        /// <summary>
        /// EkipController sinifinin yapilandirici metodu.
        /// Veritabani baglami ve SignalR hub baglamini bagimlilik enjeksiyonu ile alir.
        /// </summary>
        /// <param name="context">Uygulama veritabani baglami.</param>
        /// <param name="hubContext">Anlik bildirim gondermek icin kullanilan SignalR hub baglami.</param>
        public EkipController(AppDbContext context, IHubContext<BildirimHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // ===== EKIP LISTELEME =====

        /// <summary>
        /// Kullanicinin uye oldugu veya kurdugu ekipleri ve bekleyen davetlerini listeler.
        /// Ekipler kurulusuna gore azalan sirada, davetler ise bekleyen durumda filtrelenerek getirilir.
        /// </summary>
        /// <returns>Ekip listesi ve davetleri iceren gorunum.</returns>
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            // Kullanicinin kurucusu veya uyesi oldugu ekipleri iliskili uye verileriyle birlikte getir
            var ekipler = _context.Ekipler
                .Include(e => e.Uyeler)
                .Where(e => e.KurucuId == userId || e.Uyeler.Any(u => u.KullaniciId == userId))
                .OrderByDescending(e => e.KurulusTarihi)
                .ToList();

            // Kullaniciya gelen ve henuz beklemede olan ekip davetlerini getir
            var davetler = _context.EkipDavetleri
                .Include(d => d.Ekip)
                .Include(d => d.Gonderen)
                .Where(d => d.AliciId == userId && d.Durum == "Bekliyor")
                .ToList();

            ViewBag.GelenDavetler = davetler;

            return View(ekipler);
        }

        // ===== EKIP OLUSTURMA =====

        /// <summary>
        /// Yeni ekip olusturma formunu goruntuleyen GET metodu.
        /// Oturum acilmamissa giris sayfasina yonlendirir.
        /// </summary>
        /// <returns>Ekip olusturma formu gorunumu.</returns>
        public IActionResult Olustur()
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            if (userId == null) return RedirectToAction("Login", "Auth");
            
            return View();
        }

        /// <summary>
        /// Yeni ekip olusturma islemini gerceklestiren POST metodu.
        /// Ekip adi zorunludur. Her kullanici en fazla 5 ekip olusturabilir.
        /// Ekip olusturuldugunda kurucu otomatik olarak Lider rolunde eklenir.
        /// </summary>
        /// <param name="model">Olusturulacak ekip bilgilerini iceren model.</param>
        /// <returns>Basarili ise ekip listesine yonlendirir, basarisiz ise formu hata mesajiyla dondurur.</returns>
        [HttpPost]
        public async Task<IActionResult> Olustur(Ekip model)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(model.Ad))
            {
                ModelState.AddModelError("Ad", "Lütfen bir ekip adı girin!");
                return View(model);
            }

            // Ekip limiti kontrolu: Her kullanici en fazla 5 ekip olusturabilir
            var mevcutEkipSayisi = _context.Ekipler.Count(e => e.KurucuId == userId.Value);
            if (mevcutEkipSayisi >= 5)
            {
                TempData["Error"] = "Maksimum 5 adet ekip oluşturabilirsiniz!";
                return RedirectToAction("Index");
            }

            ModelState.Clear(); 

            model.KurucuId = userId.Value;
            model.KurulusTarihi = DateTime.Now;
            model.Aciklama = model.Aciklama ?? ""; 

            _context.Ekipler.Add(model);
            await _context.SaveChangesAsync(); 

            // Kurucuyu ekibin ilk uyesi olarak Lider rolunde kaydet
            var kurucuUye = new EkipUyesi
            {
                EkipId = model.Id,
                KullaniciId = userId.Value,
                Rol = "Lider",
                KatilmaTarihi = DateTime.Now
            };
            _context.EkipUyeleri.Add(kurucuUye);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ekip başarıyla oluşturuldu!";
            return RedirectToAction("Index");
        }

        // ===== EKIP DETAY =====

        /// <summary>
        /// Ekip detay sayfasini (karargah) goruntuleyen metot.
        /// Ekip uyeleri, gorevler, alt gorevler, etiketler, davetler ve aktivite loglarini yukler.
        /// Yalnizca ekip uyesi olan kullanicilar bu sayfaya erisebilir.
        /// </summary>
        /// <param name="id">Detaylari goruntulenmek istenen ekibin kimlik numarasi.</param>
        /// <returns>Ekip detay gorunumu veya yetkisiz erisimde liste sayfasina yonlendirme.</returns>
        public IActionResult Detay(int id)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            // Ekibi tum iliskili verileriyle birlikte yukle
            var ekip = _context.Ekipler
                .Include(e => e.Uyeler).ThenInclude(u => u.Kullanici)
                .Include(e => e.Gorevler).ThenInclude(g => g.Tamamlamalar).ThenInclude(t => t.Kullanici)
                .Include(e => e.Gorevler).ThenInclude(g => g.AltGorevler)
                .Include(e => e.Gorevler).ThenInclude(g => g.GorevEtiketleri).ThenInclude(ge => ge.Etiket)
                .Include(e => e.Davetler).ThenInclude(d => d.Alici)
                .FirstOrDefault(e => e.Id == id);

            if (ekip == null) return NotFound();

            // Yetki kontrolu: Istekte bulunan kullanicinin ekip uyesi olup olmadigini dogrula
            if (!ekip.Uyeler.Any(u => u.KullaniciId == userId))
            {
                TempData["Error"] = "Bu ekibin karargahına girme yetkiniz yok!";
                return RedirectToAction("Index");
            }

            ViewBag.CurrentUserId = userId;
            ViewBag.IsLider = ekip.Uyeler.Any(u => u.KullaniciId == userId && u.Rol == "Lider");

            // Ekibe ait son 20 aktivite logunu tarih sirasina gore getir
            ViewBag.Aktiviteler = _context.EkipAktiviteleri
                                          .Include(a => a.Kullanici)
                                          .Where(a => a.EkipId == id)
                                          .OrderByDescending(a => a.Tarih)
                                          .Take(20)
                                          .ToList();

            return View(ekip);
        }

        // ===== EKIP GOREV EKLEME =====

        /// <summary>
        /// Ekibe yeni gorev ekleyen POST metodu. Yalnizca ekip lideri bu islemi yapabilir.
        /// Gorev eklendikten sonra alt gorevler kaydedilir, aktivite logu yazilir,
        /// diger ekip uyelerine veritabani ve SignalR bildirimi gonderilir.
        /// </summary>
        /// <param name="ekipId">Gorevin eklenecegi ekibin kimlik numarasi.</param>
        /// <param name="gorevAdi">Yeni gorevin adi.</param>
        /// <param name="aciklama">Gorevin aciklamasi.</param>
        /// <param name="tarih">Gorevin hedef tarihi.</param>
        /// <param name="altGorevler">Goreve eklenecek alt gorev basliklarinin listesi.</param>
        /// <returns>Islem sonucunu iceren JSON yaniti.</returns>
        [HttpPost]
        public async Task<IActionResult> EkipGorevEkle(int ekipId, string gorevAdi, string aciklama, DateTime tarih, List<string> altGorevler)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            if (userId == null) return Json(new { success = false, message = "Oturum kapalı." });

            // Yetki kontrolu: Islemi yapan kullanicinin ekip lideri olup olmadigini dogrula
            var liderMi = _context.EkipUyeleri.Any(u => u.EkipId == ekipId && u.KullaniciId == userId && u.Rol == "Lider");
            if (!liderMi) return Json(new { success = false, message = "Sadece ekip lideri görev atayabilir!" });

            // Ekip gorev limiti kontrolu: Bir ekipte en fazla 20 gorev bulunabilir
            var toplamGorevSayisi = _context.Gorevler.Count(g => g.EkipId == ekipId);
            if (toplamGorevSayisi >= 20) return Json(new { success = false, message = "Sistemde en fazla 20 adet görev barındırabilirsiniz. Lütfen yer açmak için eski veya tamamlanmış görevleri silin!" });

            var yeniGorev = new Gorev
            {
                GorevAdi = gorevAdi,
                Aciklama = aciklama ?? "",
                Tarih = tarih,
                DurumAktifMi = true,
                KullaniciId = userId.Value,
                AtayanKullaniciId = userId.Value,
                EkipId = ekipId, 
                Oncelik = "Normal"
            };

            _context.Gorevler.Add(yeniGorev);
            await _context.SaveChangesAsync();

            // Alt gorevleri ana goreve baglayarak veritabanina kaydet
            if (altGorevler != null && altGorevler.Any())
            {
                foreach (var baslik in altGorevler)
                {
                    if (!string.IsNullOrWhiteSpace(baslik))
                    {
                        _context.AltGorevler.Add(new AltGorev
                        {
                            GorevId = yeniGorev.Id,
                            Baslik = baslik.Trim(),
                            TamamlandiMi = false
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            // Ekip aktivite loguna gorev olusturma kaydini ekle
            _context.EkipAktiviteleri.Add(new EkipAktivite {
                EkipId = ekipId,
                KullaniciId = userId.Value,
                Aksiyon = "Oluşturdu",
                Mesaj = $"'{gorevAdi}' adlı görevi ekibe tanımladı."
            });

            await _context.SaveChangesAsync();

            // Ekipteki diger uyelere bildirim gonder (gorevi olusturan haric)
            var ekip = await _context.Ekipler.FindAsync(ekipId);
            var digerUyeler = await _context.EkipUyeleri
                .Where(u => u.EkipId == ekipId && u.KullaniciId != userId.Value)
                .Select(u => u.KullaniciId)
                .ToListAsync();

            string adSoyad = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Bir ekip arkadaşın";

            if (ekip != null && digerUyeler.Any())
            {
                foreach (var uyeId in digerUyeler)
                {
                    // Her ekip uyesi icin veritabani bildirimi olustur
                    _context.Bildirimler.Add(new Bildirim {
                        KullaniciId = uyeId,
                        Mesaj = $"{adSoyad}, '{ekip.Ad}' ekibine yeni bir görev ekledi: {gorevAdi}",
                        Url = $"/Ekip/Detay/{ekipId}"
                    });
                    
                    // SignalR uzerinden her ekip uyesine anlik bildirim gonder
                    await _hubContext.Clients.Group(uyeId.ToString()).SendAsync("YeniBildirim", "Yeni Ekip Görevi!", $"{adSoyad}, '{ekip.Ad}' ekibine yeni bir görev ekledi: {gorevAdi}", "info", $"/Ekip/Detay/{ekipId}");
                }
                await _context.SaveChangesAsync();
            }

            // Sistem loglama: Ekip gorevi olusturma islemini kayit altina al
            _context.SistemLoglari.Add(new SistemLog {
                KullaniciAdi = adSoyad,
                YapilanIslem = $"Yeni ekip görevi oluşturuldu ({ekip?.Ad}): {gorevAdi}",
                IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP",
                IslemTarihi = DateTime.Now
            });
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Ekip görevi başarıyla oluşturuldu!" });
        }

        // ===== KULLANICI ARAMA =====

        /// <summary>
        /// Ekibe davet edilecek kullanicilari canli olarak arayan GET metodu.
        /// Mevcut ekip uyeleri ve bekleyen davetleri olan kullanicilar sonuclardan haric tutulur.
        /// Yasaklanmis kullanicilar da sonuclara dahil edilmez. En fazla 5 sonuc dondurulur.
        /// </summary>
        /// <param name="q">Arama metni (kullanici adi veya e-posta).</param>
        /// <param name="ekipId">Aramanin yapildigi ekibin kimlik numarasi.</param>
        /// <returns>Eslesen kullanicilari iceren JSON yaniti.</returns>
        [HttpGet]
        public IActionResult KullaniciAra(string q, int ekipId)
        {
            if (string.IsNullOrWhiteSpace(q)) return Json(new List<object>());

            var aranan = q.ToLower();

            // Mevcut ekip uyeleri ve bekleyen davet alicilari haric tutulacak kimlik listesi
            var ekipUyeIds = _context.EkipUyeleri.Where(u => u.EkipId == ekipId).Select(u => u.KullaniciId).ToList();
            var bekleyenDavetIds = _context.EkipDavetleri.Where(d => d.EkipId == ekipId && d.Durum == "Bekliyor").Select(d => d.AliciId).ToList();
            
            var haricTutulacaklar = ekipUyeIds.Concat(bekleyenDavetIds).Distinct().ToList();

            var kullanicilar = _context.Kullanicilar
                .Where(k => !k.IsBanned && !haricTutulacaklar.Contains(k.Id) && 
                            (k.KullaniciAdi.ToLower().Contains(aranan) || k.Email.ToLower().Contains(aranan)))
                .Select(k => new { 
                    id = k.Id, 
                    adSoyad = k.Ad + " " + k.Soyad, 
                    kullaniciAdi = k.KullaniciAdi 
                })
                .Take(5)
                .ToList();

            return Json(kullanicilar);
        }

        // ===== DAVET YONETIMI =====

        /// <summary>
        /// Belirtilen kullaniciya ekip daveti gonderen POST metodu.
        /// Yalnizca ekip lideri davet gonderebilir.
        /// Davet gonderildikten sonra aliciya veritabani ve SignalR bildirimi gonderilir.
        /// </summary>
        /// <param name="ekipId">Davetin gonderilecegi ekibin kimlik numarasi.</param>
        /// <param name="aliciId">Davet edilecek kullanicinin kimlik numarasi.</param>
        /// <returns>Islem sonucunu iceren JSON yaniti.</returns>
        [HttpPost]
        public async Task<IActionResult> DavetGonder(int ekipId, int aliciId)
        {
            var gonderenId = HttpContext.Session.GetInt32("KullaniciId");
            if (gonderenId == null) return Json(new { success = false, message = "Oturum zaman aşımına uğradı." });

            // Yetki kontrolu: Yalnizca ekip lideri davet gonderebilir
            var yetkiKontrol = _context.EkipUyeleri.Any(u => u.EkipId == ekipId && u.KullaniciId == gonderenId && u.Rol == "Lider");
            if (!yetkiKontrol) return Json(new { success = false, message = "Sadece ekip liderleri davet gönderebilir!" });

            var davet = new EkipDavet
            {
                EkipId = ekipId,
                GonderenId = gonderenId.Value,
                AliciId = aliciId,
                DavetTarihi = DateTime.Now,
                Durum = "Bekliyor"
            };

            _context.EkipDavetleri.Add(davet);

            // Aliciya ekip daveti bildirimi olustur
            var gonderen = await _context.Kullanicilar.FindAsync(gonderenId.Value);
            var ekip = await _context.Ekipler.FindAsync(ekipId);
            string gonderenAdi = gonderen?.Ad ?? "Biri";
            
            _context.Bildirimler.Add(new Bildirim {
                KullaniciId = aliciId,
                Mesaj = $"{gonderenAdi} seni '{ekip?.Ad}' ekibine davet etti!",
                Url = "/Ekip/Index"
            });

            await _context.SaveChangesAsync();

            // SignalR uzerinden aliciya anlik davet bildirimi gonder
            await _hubContext.Clients.Group(aliciId.ToString()).SendAsync("YeniBildirim", "Yeni Ekip Daveti!", $"{gonderenAdi}, seni '{ekip?.Ad}' ekibine davet etti.", "info", "/Ekip/Index");

            return Json(new { success = true, message = "Davet başarıyla gönderildi!" });
        }

        /// <summary>
        /// Gelen ekip davetini kabul eden POST metodu.
        /// Kabul edilen davetle kullanici, ekibe Uye rolunde eklenir ve davet kaydi silinir.
        /// </summary>
        /// <param name="davetId">Kabul edilecek davetin kimlik numarasi.</param>
        /// <returns>Islem sonucunu iceren JSON yaniti.</returns>
        [HttpPost]
        public async Task<IActionResult> DavetKabul(int davetId)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            if (userId == null) return Json(new { success = false, message = "Oturum kapalı." });

            var davet = await _context.EkipDavetleri
                                      .Include(d => d.Ekip)
                                      .FirstOrDefaultAsync(d => d.Id == davetId && d.AliciId == userId);
            
            if (davet == null) return Json(new { success = false, message = "Davet bulunamadı veya zaten işlenmiş!" });

            // Kabul eden kullaniciyi ekibe Uye rolunde ekle
            var yeniUye = new EkipUyesi
            {
                EkipId = davet.EkipId,
                KullaniciId = userId.Value,
                Rol = "Uye",
                KatilmaTarihi = DateTime.Now
            };
            _context.EkipUyeleri.Add(yeniUye);

            // Islenen davet kaydini veritabanindan kaldir
            _context.EkipDavetleri.Remove(davet);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"{davet.Ekip.Ad} ekibine başarıyla katıldınız! Hoş geldiniz!" });
        }

        /// <summary>
        /// Gelen ekip davetini reddeden POST metodu.
        /// Reddedilen davet kaydi veritabanindan kaldirilir.
        /// </summary>
        /// <param name="davetId">Reddedilecek davetin kimlik numarasi.</param>
        /// <returns>Islem sonucunu iceren JSON yaniti.</returns>
        [HttpPost]
        public async Task<IActionResult> DavetRed(int davetId)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            if (userId == null) return Json(new { success = false });

            var davet = await _context.EkipDavetleri.FirstOrDefaultAsync(d => d.Id == davetId && d.AliciId == userId);
            
            if (davet != null)
            {
                _context.EkipDavetleri.Remove(davet);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Davet reddedildi." });
        }

        // ===== EKIP GOREV DURUM YONETIMI =====

        /// <summary>
        /// Ekip gorevini tamamlama islemini gerceklestiren POST metodu.
        /// Her ekip uyesi kendi tamamlama kaydini olusturur. Eger tum hedef uyeler gorevi
        /// tamamlamissa, gorev otomatik olarak pasif (tamamlanmis) duruma gecirilir.
        /// Lider haricindeki uyeler hedef olarak belirlenir; uye yoksa lider hedef olur.
        /// </summary>
        /// <param name="gorevId">Tamamlanacak gorevin kimlik numarasi.</param>
        /// <returns>Islem sonucunu iceren JSON yaniti.</returns>
        [HttpPost]
        public async Task<IActionResult> GorevDurumGuncelle(int gorevId)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            if (userId == null) return Json(new { success = false, message = "Oturum kapalı." });

            var gorev = await _context.Gorevler.FindAsync(gorevId);
            if (gorev == null) return Json(new { success = false, message = "Görev bulunamadı!" });

            // Kullanicinin bu gorev icin tamamlama kaydi olup olmadigini kontrol et
            var tamamlama = await _context.GorevTamamlamalari.FirstOrDefaultAsync(t => t.GorevId == gorevId && t.KullaniciId == userId.Value);
            if (tamamlama == null)
            {
                _context.GorevTamamlamalari.Add(new GorevTamamlama { GorevId = gorevId, KullaniciId = userId.Value });
                await _context.SaveChangesAsync();

                // Gorevi olusturan kullaniciya tamamlama bildirimi gonder
                if (gorev.KullaniciId != userId.Value)
                {
                    string adSoyad = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Bir ekip arkadaşın";
                    _context.Bildirimler.Add(new Bildirim {
                        KullaniciId = gorev.KullaniciId,
                        Mesaj = $"{adSoyad}, '{gorev.GorevAdi}' adlı ekip görevini tamamladı!",
                        Url = $"/Ekip/Detay/{gorev.EkipId}"
                    });
                    
                    // Sistem loglama: Ekip gorev tamamlama islemini kayit altina al
                    _context.SistemLoglari.Add(new SistemLog {
                        KullaniciAdi = adSoyad,
                        YapilanIslem = $"Ekip görevini tamamladı: {gorev.GorevAdi}",
                        IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP",
                        IslemTarihi = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                }
            }

            // Tum hedef uyelerin gorevi tamamlayip tamamlamadigini degerlendir
            var tamamlamalar = await _context.GorevTamamlamalari.Where(t => t.GorevId == gorevId).Select(t => t.KullaniciId).ToListAsync();
            var ekipUyeleri = await _context.EkipUyeleri.Where(u => u.EkipId == gorev.EkipId).ToListAsync();
            var uyelerHaricLider = ekipUyeleri.Where(u => u.Rol != "Lider").ToList();

            // Hedef uye listesini belirle: Lider haricindeki uyeler onceliklidir, yoksa lider hedef olur
            var hedefUyeler = uyelerHaricLider.Any() ? uyelerHaricLider : ekipUyeleri.Where(u => u.Rol == "Lider").ToList();
            
            var bekleyenKalmadi = !hedefUyeler.Any(u => !tamamlamalar.Contains(u.KullaniciId));
            
            // Tum hedef uyeler tamamladiysa gorevi otomatik olarak pasif duruma gecir
            if (bekleyenKalmadi && gorev.DurumAktifMi)
            {
                gorev.DurumAktifMi = false;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Görev başarıyla tamamlandı!" });
        }

        // ===== UYE YONETIMI =====

        /// <summary>
        /// Belirtilen uyeyi ekipten cikaran POST metodu. Yalnizca ekip lideri bu islemi yapabilir.
        /// Lider kendini ekipten cikaramaz.
        /// </summary>
        /// <param name="ekipId">Uyenin cikarilacagi ekibin kimlik numarasi.</param>
        /// <param name="uyeId">Ekipten cikarilacak uyenin kimlik numarasi.</param>
        /// <returns>Islem sonucunu iceren JSON yaniti.</returns>
        [HttpPost]
        public async Task<IActionResult> UyeCikar(int ekipId, int uyeId)
        {
            var liderId = HttpContext.Session.GetInt32("KullaniciId");
            if (liderId == null) return Json(new { success = false });

            // Yetki kontrolu: Islemi yapan kullanicinin ekip lideri olup olmadigini dogrula
            var liderMi = await _context.EkipUyeleri.AnyAsync(u => u.EkipId == ekipId && u.KullaniciId == liderId && u.Rol == "Lider");
            if (!liderMi) return Json(new { success = false, message = "Bu işlem için Lider yetkisi gerekiyor!" });

            if (liderId == uyeId) return Json(new { success = false, message = "Kendinizi ekipten çıkaramazsınız!" });

            var uye = await _context.EkipUyeleri.FirstOrDefaultAsync(u => u.EkipId == ekipId && u.KullaniciId == uyeId);
            if (uye != null)
            {
                _context.EkipUyeleri.Remove(uye);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Üye ekipten çıkarıldı." });
            }
            return Json(new { success = false, message = "Üye bulunamadı." });
        }

        // ===== EKIP SILME =====

        /// <summary>
        /// Ekibi tum iliskili verileriyle birlikte kalici olarak silen POST metodu.
        /// Yalnizca ekip kurucusu bu islemi yapabilir.
        /// Silme sirasi: Gorevler, davetler, uyeler ve son olarak ekip kaydi.
        /// </summary>
        /// <param name="ekipId">Silinecek ekibin kimlik numarasi.</param>
        /// <returns>Islem sonucunu iceren JSON yaniti.</returns>
        [HttpPost]
        public async Task<IActionResult> EkipSil(int ekipId)
        {
            var liderId = HttpContext.Session.GetInt32("KullaniciId");
            if (liderId == null) return Json(new { success = false });

            // Ekibi iliskili tum verileriyle birlikte yukle ve kurucu yetkisini dogrula
            var ekip = await _context.Ekipler
                .Include(e => e.Uyeler)
                .Include(e => e.Gorevler)
                .Include(e => e.Davetler)
                .FirstOrDefaultAsync(e => e.Id == ekipId && e.KurucuId == liderId);

            if (ekip == null) return Json(new { success = false, message = "Silme yetkiniz yok!" });

            // Iliskisel butunlugu korumak icin bagli verileri sirasina gore sil
            _context.EkipUyeleri.RemoveRange(ekip.Uyeler);
            _context.Gorevler.RemoveRange(ekip.Gorevler);
            _context.EkipDavetleri.RemoveRange(ekip.Davetler);
            _context.Ekipler.Remove(ekip);
            
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Ekip kalıcı olarak silindi." });
        }

        // ===== EKIP GOREV DETAY - AJAX =====

        /// <summary>
        /// Belirtilen ekip gorevinin detaylarini AJAX istegi icin JSON formatinda dondurur.
        /// Alt gorevler, tamamlama durumlari ve tamamlayan kullanici bilgileri dahil edilir.
        /// Ekip gorevlerinde tamamlama durumu oturumdaki kullaniciya ozel olarak hesaplanir.
        /// </summary>
        /// <param name="id">Detaylari getirilecek gorevin kimlik numarasi.</param>
        /// <returns>Gorev detaylarini iceren JSON yaniti.</returns>
        [HttpGet]
        public async Task<IActionResult> GorevGetir(int id)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            
            var gorev = await _context.Gorevler
                .Include(g => g.AltGorevler)
                    .ThenInclude(a => a.Tamamlamalar)
                        .ThenInclude(t => t.Kullanici)
                .Select(g => new {
                    g.Id,
                    g.GorevAdi,
                    g.Aciklama,
                    tarih = g.Tarih.ToString("yyyy-MM-dd"),
                    g.DurumAktifMi,
                    altGorevler = g.AltGorevler.Select(a => new { 
                        id = a.Id, 
                        baslik = a.Baslik, 
                        // Ekip gorevlerinde kullaniciya ozel tamamlama durumu, kisisel gorevlerde genel durum
                        tamamlandiMi = g.EkipId != null ? a.Tamamlamalar.Any(t => t.KullaniciId == userId) : a.TamamlandiMi,
                        tamamlayanlar = a.Tamamlamalar.Select(t => t.Kullanici.Ad).ToList()
                    }).ToList()
                }).FirstOrDefaultAsync(x => x.Id == id);

            return Json(gorev);
        }

        // ===== EKIP GOREV GUNCELLEME =====

        /// <summary>
        /// Ekip gorevini guncelleyen POST metodu. Yalnizca ekip lideri duzenleme yapabilir.
        /// Guncelleme islemi sistem loguna kaydedilir.
        /// </summary>
        /// <param name="id">Guncellenecek gorevin kimlik numarasi.</param>
        /// <param name="gorevAdi">Gorevin yeni adi.</param>
        /// <param name="aciklama">Gorevin yeni aciklamasi.</param>
        /// <param name="tarih">Gorevin yeni hedef tarihi.</param>
        /// <returns>Islem sonucunu iceren JSON yaniti.</returns>
        [HttpPost]
        public async Task<IActionResult> EkipGorevGuncelle(int id, string gorevAdi, string aciklama, DateTime tarih)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            var gorev = await _context.Gorevler.Include(g => g.Ekip).FirstOrDefaultAsync(x => x.Id == id);
            
            if (gorev == null) return Json(new { success = false });

            // Yetki kontrolu: Yalnizca ekip lideri gorev duzenleyebilir
            var liderMi = await _context.EkipUyeleri.AnyAsync(u => u.EkipId == gorev.EkipId && u.KullaniciId == userId && u.Rol == "Lider");
            if (!liderMi) return Json(new { success = false, message = "Düzenleme yetkiniz yok!" });

            gorev.GorevAdi = gorevAdi;
            gorev.Aciklama = aciklama ?? "";
            gorev.Tarih = tarih;

            // Sistem loglama: Ekip gorev guncelleme islemini kayit altina al
            string adSoyad = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Bilinmeyen Kullanıcı";
            _context.SistemLoglari.Add(new SistemLog {
                KullaniciAdi = adSoyad,
                YapilanIslem = $"Ekip görevini güncelledi ({gorev.Ekip?.Ad}): {gorevAdi}",
                IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP",
                IslemTarihi = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Görev başarıyla güncellendi." });
        }

        // ===== EKIP GOREV SILME =====

        /// <summary>
        /// Ekip gorevini kalici olarak silen POST metodu. Yalnizca ekip lideri silme yapabilir.
        /// Silme islemi sistem loguna kaydedilir.
        /// </summary>
        /// <param name="id">Silinecek gorevin kimlik numarasi.</param>
        /// <returns>Islem sonucunu iceren JSON yaniti.</returns>
        [HttpPost]
        public async Task<IActionResult> EkipGorevSil(int id)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            var gorev = await _context.Gorevler.Include(g => g.Ekip).FirstOrDefaultAsync(x => x.Id == id);
            if (gorev == null) return Json(new { success = false });

            // Yetki kontrolu: Yalnizca ekip lideri gorev silebilir
            var liderMi = await _context.EkipUyeleri.AnyAsync(u => u.EkipId == gorev.EkipId && u.KullaniciId == userId && u.Rol == "Lider");
            if (!liderMi) return Json(new { success = false, message = "Silme yetkiniz yok!" });

            // Sistem loglama: Ekip gorev silme islemini kayit altina al
            string adSoyad = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Bilinmeyen Kullanıcı";
            _context.SistemLoglari.Add(new SistemLog {
                KullaniciAdi = adSoyad,
                YapilanIslem = $"Ekip görevini sildi ({gorev.Ekip?.Ad}): {gorev.GorevAdi}",
                IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP",
                IslemTarihi = DateTime.Now
            });

            _context.Gorevler.Remove(gorev);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Görev kalıcı olarak silindi." });
        }

        // ===== TAKIMDAN AYRILMA =====

        /// <summary>
        /// Kullanicinin kendi istegi ile ekipten ayrilmasini saglayan POST metodu.
        /// Ekip lideri dogrudan ayrilamaz; once ekibi dagitmali veya liderligi devretmelidir.
        /// Ayrilma islemi sistem loguna kaydedilir.
        /// </summary>
        /// <param name="ekipId">Ayrilmak istenen ekibin kimlik numarasi.</param>
        /// <returns>Islem sonucunu iceren JSON yaniti.</returns>
        [HttpPost]
        public async Task<IActionResult> TakimdanAyril(int ekipId)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            if (userId == null) return Json(new { success = false, message = "Oturum kapalı." });

            var uyeKaydi = await _context.EkipUyeleri.FirstOrDefaultAsync(u => u.EkipId == ekipId && u.KullaniciId == userId);
            if (uyeKaydi == null) return Json(new { success = false, message = "Zaten bu takımda değilsiniz." });

            // Lider dogrudan ayrilamaz; once ekibi dagitmali veya liderligi devretmelidir
            if (uyeKaydi.Rol == "Lider") 
            {
                return Json(new { success = false, message = "Takım liderleri doğrudan ayrılamaz. Önce ekibi dağıtmalı veya liderliği devretmelisiniz." });
            }

            _context.EkipUyeleri.Remove(uyeKaydi);

            // Sistem loglama: Takimdan ayrilma islemini kayit altina al
            string adSoyad = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Bilinmeyen Kullanıcı";
            _context.SistemLoglari.Add(new SistemLog {
                KullaniciAdi = adSoyad,
                YapilanIslem = $"Takımdan kendi isteğiyle ayrıldı (Ekip ID: {ekipId})",
                IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP",
                IslemTarihi = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Takımdan başarıyla ayrıldınız." });
        }
    }
}