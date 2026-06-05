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
    [YetkiKontrol] // Giriş yapmayan kullanıcılar bu Controller'a erişemez.
    public class GorevController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<BildirimHub> _hubContext;

        public GorevController(AppDbContext context, IHubContext<BildirimHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
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
            
            // Mevcut etiketleri getir
            ViewBag.Etiketler = _context.Etiketler.Where(e => e.EkipId == null || _context.EkipUyeleri.Any(eu => eu.KullaniciId == kullaniciId && eu.EkipId == e.EkipId)).ToList();

            return View();
        }

        // --- ETİKET EKLEME (AJAX) ---
        [HttpPost]
        public IActionResult EtiketEkle(string ad, string renkHex)
        {
            if (string.IsNullOrWhiteSpace(ad)) return Json(new { success = false, message = "Etiket adı boş olamaz!" });
            
            var me = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            // Sadece rengi veya adı aynı olan var mı kontrolü eklenebilir.
            var yeniEtiket = new Etiket
            {
                Ad = ad,
                RenkHex = renkHex ?? "#4f46e5",
                // İstersen EkipId de atayabilirsin, şu an basit tutuyoruz
            };

            _context.Etiketler.Add(yeniEtiket);
            _context.SaveChanges();

            return Json(new { success = true, data = new { id = yeniEtiket.Id, ad = yeniEtiket.Ad, renkHex = yeniEtiket.RenkHex } });
        }

        // --- ALT GÖREV EKLEME (AJAX) ---
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

        // --- ALT GÖREV DURUM DEĞİŞTİRME (AJAX) ---
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
                    // Ekip görevi ise her kullanıcı kendi tamamlamasını yapar
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
                    // Kişisel görev ise doğrudan durumu değiştir
                    altGorev.TamamlandiMi = tamamlandiMi;
                    _context.SaveChanges();
                }
                
                return Json(new { success = true, gorevId = altGorev.GorevId });
            }
            return Json(new { success = false });
        }

        // --- 6. YENİ GÖREV EKLEME İŞLEMİ (POST) ---
        [HttpPost]
        [EnableRateLimiting("GorevEklemeSiniri")]
        public IActionResult Create(Gorev gorev, List<int> seciliEtiketler, List<string> altGorevler)
        {
            string zararliKodDeseni = @"<[^>]+>"; 
            if (Regex.IsMatch(gorev.GorevAdi ?? "", zararliKodDeseni) || Regex.IsMatch(gorev.Aciklama ?? "", zararliKodDeseni))
            {
                TempData["Error"] = "Güvenlik İhlali: Görev bilgilerinde HTML, JS veya CSS kodları kullanılamaz!";
                return View(gorev);
            }

            int me = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            
            // Limit Kontrolü: Kişisel görev sayısı en fazla 50 olabilir
            int currentTaskCount = _context.Gorevler.Count(g => g.KullaniciId == me && g.EkipId == null);
            if (currentTaskCount >= 50)
            {
                TempData["Error"] = "Limit Aşıldı: En fazla 50 kişisel görev oluşturabilirsiniz.";
                return View(gorev);
            }

            // Sadece kendine atanıyor
            gorev.KullaniciId = me;
            gorev.AtayanKullaniciId = null;
            gorev.DurumAktifMi = true; // Yeni görev eklendiğinde aktiftir

            _context.Gorevler.Add(gorev);
            _context.SaveChanges();

            // Alt Görevleri Kaydet
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

            if (gorev.KullaniciId != me)
            {
                string adSoyad = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Biri";
                _context.Bildirimler.Add(new Bildirim {
                    KullaniciId = gorev.KullaniciId,
                    Mesaj = $"{adSoyad} sana '{gorev.GorevAdi}' görevini atadı.",
                    Url = $"/Gorev/Details/{gorev.Id}"
                });
                _context.SaveChanges();
                
                // SignalR ile Anlık Bildirim
                _hubContext.Clients.Group(gorev.KullaniciId.ToString()).SendAsync("YeniBildirim", "Yeni Görev!", $"{adSoyad} sana '{gorev.GorevAdi}' görevini atadı.", "info", $"/Gorev/Details/{gorev.Id}");
            }

            // EKİP AKTİVİTE LOGLAMA
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
        
        // --- 13. SADECE YETKİLİ (Ekip Sahibi/Yönetici) GÖREV ATAMASI (AJAX) ---
        [HttpPost]
        public IActionResult GorevAta(int gorevId, int yeniKullaniciId)
        {
            var me = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var gorev = _context.Gorevler.FirstOrDefault(g => g.Id == gorevId && g.EkipId != null);
            if(gorev == null) return Json(new { success = false, message = "Görev bulunamadı!" });

            // Kullanıcının bu ekibin kurucusu olup olmadığını kontrol et
            var ekip = _context.Ekipler.FirstOrDefault(e => e.Id == gorev.EkipId);
            if(ekip == null || ekip.KurucuId != me) {
                return Json(new { success = false, message = "Sadece ekip kurucusu görev ataması yapabilir!" });
            }

            gorev.KullaniciId = yeniKullaniciId;
            gorev.AtayanKullaniciId = me;
            _context.SaveChanges();

            // Bildirim
            var adSoyad = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Yönetici";
            _context.Bildirimler.Add(new Bildirim {
                KullaniciId = yeniKullaniciId,
                Mesaj = $"{adSoyad} sana '{gorev.GorevAdi}' görevini atadı.",
                Url = $"/Ekip/Detay/{gorev.EkipId}"
            });
            _context.SaveChanges();

            // SignalR ile Anlık Bildirim
            _hubContext.Clients.Group(yeniKullaniciId.ToString()).SendAsync("YeniBildirim", "Yeni Görev Ataması!", $"{adSoyad} sana '{gorev.GorevAdi}' görevini atadı.", "info", $"/Ekip/Detay/{gorev.EkipId}");

            return Json(new { success = true });
        }

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

                // EKİP AKTİVİTE LOGLAMA
                if (gorev.EkipId.HasValue && gorev.EkipId.Value > 0)
                {
                    _context.EkipAktiviteleri.Add(new EkipAktivite {
                        EkipId = gorev.EkipId.Value,
                        KullaniciId = kullaniciId,
                        Aksiyon = "Tamamladı",
                        Mesaj = $"'{gorev.GorevAdi}' adlı görevi tamamladı."
                    });
                }

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
            
            // Sadece giriş yapan kullanıcı (veya kurucusu) kendi görevini görüntüleyebilir!
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

        // --- 14. TAKVİM SAYFASI ---
        public IActionResult Takvim()
        {
            return View();
        }

        // --- 15. TAKVİM İÇİN GÖREVLERİ GETİR (JSON) ---
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
                                       color = !g.DurumAktifMi ? "#10b981" : (g.Oncelik == "Yüksek" ? "#ef4444" : "#4f46e5"), // Tamamlanmış yeşil, Yüksek kırmızı, diğerleri indigo
                                       url = $"/Gorev/Detay/{g.Id}"
                                   })
                                   .ToList();

            return Json(gorevler);
        }

        // --- 16. TAKVİMDE SÜRÜKLE BIRAK İLE TARİH GÜNCELLEME ---
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
                // Mevcut saati koru, sadece tarihi güncelle (Eğer saati varsa)
                gorev.Tarih = new DateTime(parsedDate.Year, parsedDate.Month, parsedDate.Day, gorev.Tarih.Hour, gorev.Tarih.Minute, gorev.Tarih.Second);
                _context.SaveChanges();
                return Json(new { success = true, message = "Görev tarihi güncellendi." });
            }

            return Json(new { success = false, message = "Geçersiz tarih formatı." });
        }
    }
}