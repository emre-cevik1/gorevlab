using Microsoft.AspNetCore.Mvc;
using GorevTakipSistemi.Data;
using GorevTakipSistemi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GorevTakipSistemi.Controllers
{
    public class EkipController : Controller
    {
        private readonly AppDbContext _context;

        public EkipController(AppDbContext context)
        {
            _context = context;
        }

        // --- KULLANICININ EKİPLERİNİ VE DAVETLERİNİ LİSTELE ---
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            // Mevcut Ekiplerim
            var ekipler = _context.Ekipler
                .Include(e => e.Uyeler)
                .Where(e => e.KurucuId == userId || e.Uyeler.Any(u => u.KullaniciId == userId))
                .OrderByDescending(e => e.KurulusTarihi)
                .ToList();

            // Gelen Davetlerim
            var davetler = _context.EkipDavetleri
                .Include(d => d.Ekip)
                .Include(d => d.Gonderen)
                .Where(d => d.AliciId == userId && d.Durum == "Bekliyor")
                .ToList();

            ViewBag.GelenDavetler = davetler;

            return View(ekipler);
        }

        // --- YENİ EKİP OLUŞTURMA SAYFASI (GET) ---
        public IActionResult Olustur()
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            if (userId == null) return RedirectToAction("Login", "Auth");
            
            return View();
        }

        // --- YENİ EKİP OLUŞTURMA İŞLEMİ (POST) ---
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

            ModelState.Clear(); 

            model.KurucuId = userId.Value;
            model.KurulusTarihi = DateTime.Now;
            model.Aciklama = model.Aciklama ?? ""; 

            _context.Ekipler.Add(model);
            await _context.SaveChangesAsync(); 

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

        // --- EKİP DETAYLARI (KARARGAH) ---
        public IActionResult Detay(int id)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var ekip = _context.Ekipler
                .Include(e => e.Uyeler).ThenInclude(u => u.Kullanici)
                .Include(e => e.Gorevler).ThenInclude(g => g.Tamamlamalar).ThenInclude(t => t.Kullanici)
                .Include(e => e.Davetler).ThenInclude(d => d.Alici)
                .FirstOrDefault(e => e.Id == id);

            if (ekip == null) return NotFound();

            if (!ekip.Uyeler.Any(u => u.KullaniciId == userId))
            {
                TempData["Error"] = "Bu ekibin karargahına girme yetkiniz yok!";
                return RedirectToAction("Index");
            }

            ViewBag.CurrentUserId = userId;
            ViewBag.IsLider = ekip.Uyeler.Any(u => u.KullaniciId == userId && u.Rol == "Lider");

            return View(ekip);
        }

        // --- EKİBE GÖREV EKLEME ---
        [HttpPost]
        public async Task<IActionResult> EkipGorevEkle(int ekipId, string gorevAdi, string aciklama, DateTime tarih)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            if (userId == null) return Json(new { success = false, message = "Oturum kapalı." });

            var uyeMi = _context.EkipUyeleri.Any(u => u.EkipId == ekipId && u.KullaniciId == userId);
            if (!uyeMi) return Json(new { success = false, message = "Bu ekibe görev ekleme yetkiniz yok!" });

            var yeniGorev = new Gorev
            {
                GorevAdi = gorevAdi,
                Aciklama = aciklama ?? "",
                Tarih = tarih,
                DurumAktifMi = true,
                KullaniciId = userId.Value, 
                EkipId = ekipId, 
                Oncelik = "Normal" 
            };

            _context.Gorevler.Add(yeniGorev);
            await _context.SaveChangesAsync();

            // Ekipteki diğer üyelere bildirim gönder
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
                    _context.Bildirimler.Add(new Bildirim {
                        KullaniciId = uyeId,
                        Mesaj = $"{adSoyad}, '{ekip.Ad}' ekibine yeni bir görev ekledi: {gorevAdi}",
                        Url = $"/Ekip/Detay/{ekipId}"
                    });
                }
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Ekip görevi başarıyla oluşturuldu! 🎯" });
        }

        // --- CANLI KULLANICI ARAMA ---
        [HttpGet]
        public IActionResult KullaniciAra(string q, int ekipId)
        {
            if (string.IsNullOrWhiteSpace(q)) return Json(new List<object>());

            var aranan = q.ToLower();

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

        // --- DAVET GÖNDERME ---
        [HttpPost]
        public async Task<IActionResult> DavetGonder(int ekipId, int aliciId)
        {
            var gonderenId = HttpContext.Session.GetInt32("KullaniciId");
            if (gonderenId == null) return Json(new { success = false, message = "Oturum zaman aşımına uğradı." });

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
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Davet füzeleri başarıyla ateşlendi! 🚀" });
        }

        // --- DAVETİ KABUL ET ---
        [HttpPost]
        public async Task<IActionResult> DavetKabul(int davetId)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            if (userId == null) return Json(new { success = false, message = "Oturum kapalı." });

            var davet = await _context.EkipDavetleri
                                      .Include(d => d.Ekip)
                                      .FirstOrDefaultAsync(d => d.Id == davetId && d.AliciId == userId);
            
            if (davet == null) return Json(new { success = false, message = "Davet bulunamadı veya zaten işlenmiş!" });

            var yeniUye = new EkipUyesi
            {
                EkipId = davet.EkipId,
                KullaniciId = userId.Value,
                Rol = "Uye",
                KatilmaTarihi = DateTime.Now
            };
            _context.EkipUyeleri.Add(yeniUye);

            _context.EkipDavetleri.Remove(davet);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"{davet.Ekip.Ad} ekibine başarıyla katıldın! Hoş geldin! 🎉" });
        }

        // --- DAVETİ REDDET ---
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
        // --- 1. GÖREVİ TAMAMLA (HERKES YAPABİLİR) ---
        [HttpPost]
        public async Task<IActionResult> GorevDurumGuncelle(int gorevId)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciId");
            if (userId == null) return Json(new { success = false, message = "Oturum kapalı." });

            var gorev = await _context.Gorevler.FindAsync(gorevId);
            if (gorev == null) return Json(new { success = false, message = "Görev bulunamadı!" });

            var tamamlama = await _context.GorevTamamlamalari.FirstOrDefaultAsync(t => t.GorevId == gorevId && t.KullaniciId == userId.Value);
            if (tamamlama == null)
            {
                _context.GorevTamamlamalari.Add(new GorevTamamlama { GorevId = gorevId, KullaniciId = userId.Value });
                await _context.SaveChangesAsync();

                // Görevi oluşturana bildirim gönder
                if (gorev.KullaniciId != userId.Value)
                {
                    string adSoyad = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Bir ekip arkadaşın";
                    _context.Bildirimler.Add(new Bildirim {
                        KullaniciId = gorev.KullaniciId,
                        Mesaj = $"{adSoyad}, '{gorev.GorevAdi}' adlı ekip görevini tamamladı! ✅",
                        Url = $"/Ekip/Detay/{gorev.EkipId}"
                    });
                    await _context.SaveChangesAsync();
                }
            }

            return Json(new { success = true, message = "Görev başarıyla tamamlandı! ✅" });
        }

        // --- 2. ÜYE ŞUTLAMA (SADECE LİDER) ---
        [HttpPost]
        public async Task<IActionResult> UyeCikar(int ekipId, int uyeId)
        {
            var liderId = HttpContext.Session.GetInt32("KullaniciId");
            if (liderId == null) return Json(new { success = false });

            // İşlemi yapan kişi gerçekten lider mi?
            var liderMi = await _context.EkipUyeleri.AnyAsync(u => u.EkipId == ekipId && u.KullaniciId == liderId && u.Rol == "Lider");
            if (!liderMi) return Json(new { success = false, message = "Bu işlem için Lider yetkisi gerekiyor!" });

            if (liderId == uyeId) return Json(new { success = false, message = "Kendinizi ekipten çıkaramazsınız!" });

            var uye = await _context.EkipUyeleri.FirstOrDefaultAsync(u => u.EkipId == ekipId && u.KullaniciId == uyeId);
            if (uye != null)
            {
                _context.EkipUyeleri.Remove(uye);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Üye ekipten şutlandı! 🥾" });
            }
            return Json(new { success = false, message = "Üye bulunamadı." });
        }

        // --- 3. EKİBİ TAMAMEN DAĞITMA (SADECE LİDER) ---
        [HttpPost]
        public async Task<IActionResult> EkipSil(int ekipId)
        {
            var liderId = HttpContext.Session.GetInt32("KullaniciId");
            if (liderId == null) return Json(new { success = false });

            // SQL Patlamaması için her şeyi sırayla siliyoruz (Görevler, Davetler, Üyeler, Ekip)
            var ekip = await _context.Ekipler
                .Include(e => e.Uyeler)
                .Include(e => e.Gorevler)
                .Include(e => e.Davetler)
                .FirstOrDefaultAsync(e => e.Id == ekipId && e.KurucuId == liderId);

            if (ekip == null) return Json(new { success = false, message = "Silme yetkiniz yok!" });

            _context.EkipUyeleri.RemoveRange(ekip.Uyeler);
            _context.Gorevler.RemoveRange(ekip.Gorevler);
            _context.EkipDavetleri.RemoveRange(ekip.Davetler);
            _context.Ekipler.Remove(ekip);
            
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Karargah tamamen imha edildi. 💥" });
        }
        // --- GÖREV DETAYINI GETİR (AJAX) ---
[HttpGet]
public async Task<IActionResult> GorevGetir(int id)
{
    var gorev = await _context.Gorevler.Select(g => new {
        g.Id,
        g.GorevAdi,
        g.Aciklama,
        tarih = g.Tarih.ToString("yyyy-MM-dd"),
        g.DurumAktifMi
    }).FirstOrDefaultAsync(x => x.Id == id);

    return Json(gorev);
}

// --- GÖREV GÜNCELLE (SADECE LİDER) ---
[HttpPost]
public async Task<IActionResult> EkipGorevGuncelle(int id, string gorevAdi, string aciklama, DateTime tarih)
{
    var userId = HttpContext.Session.GetInt32("KullaniciId");
    var gorev = await _context.Gorevler.Include(g => g.Ekip).FirstOrDefaultAsync(x => x.Id == id);
    
    if (gorev == null) return Json(new { success = false });

    // Güvenlik: Sadece lider düzenleyebilir
    var liderMi = await _context.EkipUyeleri.AnyAsync(u => u.EkipId == gorev.EkipId && u.KullaniciId == userId && u.Rol == "Lider");
    if (!liderMi) return Json(new { success = false, message = "Düzenleme yetkiniz yok!" });

    gorev.GorevAdi = gorevAdi;
    gorev.Aciklama = aciklama ?? "";
    gorev.Tarih = tarih;

    await _context.SaveChangesAsync();
    return Json(new { success = true, message = "Görev güncellendi! 📝" });
}

// --- GÖREV SİL (SADECE LİDER) ---
[HttpPost]
public async Task<IActionResult> EkipGorevSil(int id)
{
    var userId = HttpContext.Session.GetInt32("KullaniciId");
    var gorev = await _context.Gorevler.FirstOrDefaultAsync(x => x.Id == id);
    if (gorev == null) return Json(new { success = false });

    var liderMi = await _context.EkipUyeleri.AnyAsync(u => u.EkipId == gorev.EkipId && u.KullaniciId == userId && u.Rol == "Lider");
    if (!liderMi) return Json(new { success = false, message = "Silme yetkiniz yok!" });

    _context.Gorevler.Remove(gorev);
    await _context.SaveChangesAsync();
    return Json(new { success = true, message = "Görev kalıcı olarak silindi. 🗑️" });
}
    }
}