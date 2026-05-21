using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using GorevTakipSistemi.Data;
using GorevTakipSistemi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq; 
using System;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;

namespace GorevTakipSistemi.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AdminController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // --- KULLANICI LİSTESİ ---
        public IActionResult Kullanicilar()
        {
            var rol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            // GÜVENLİK: Admin VEYA Kurucu girebilir
            if (rol != (int)KullaniciRol.Admin && rol != (int)KullaniciRol.Owner) 
            {
                TempData["Error"] = "Bu alana erişim yetkiniz bulunmamaktadır!";
                return RedirectToAction("Index", "Home");
            }

            // PATRON LİSTEDE GÖRÜNSÜN: Filtreyi kaldırdık, herkes listeleniyor.
            var kullanicilar = _context.Kullanicilar.AsNoTracking().Select(u => new KullaniciYonetimViewModel
            {
                Id = u.Id,
                AdSoyad = u.Ad + " " + u.Soyad,
                KullaniciAdi = u.KullaniciAdi,
                Email = u.Email,
                Rol = u.Rol,
                
                IsBanned = u.IsBanned, 
                BanNedeni = u.BanNedeni,
                BanBitisTarihi = u.BanBitisTarihi,
                
                ToplamGorevSayisi = _context.Gorevler.Count(g => g.KullaniciId == u.Id),
                TamamlananGorevSayisi = _context.Gorevler.Count(g => g.KullaniciId == u.Id && !g.DurumAktifMi)
            }).ToList();

            return View(kullanicilar);
        }

        [HttpPost]
        public IActionResult BakimModuTetikle()
        {
            var rol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (rol != (int)KullaniciRol.Owner)
            {
                TempData["Error"] = "Bu işlem için kurucu yetkisi gereklidir.";
                return RedirectToAction("Index", "Home");
            }

            GorevTakipSistemi.Models.SiteSettings.BakimModuAktif = !GorevTakipSistemi.Models.SiteSettings.BakimModuAktif;

            TempData["Success"] = GorevTakipSistemi.Models.SiteSettings.BakimModuAktif 
                ? "Sistem başarıyla BAKIM moduna alındı." 
                : "Sistem tekrar YAYINA alındı.";
                
            return Redirect(Request.Headers["Referer"].ToString() ?? "/Admin/Loglar");
        }

        // --- SÜRELİ VE NEDENLİ BANLAMA METODU ---
        [HttpPost]
        public async Task<IActionResult> KullaniciBanla(int id, string neden, int gun)
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) return RedirectToAction("Index", "Home");

            var currentUserId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var kullanici = await _context.Kullanicilar.FindAsync(id);

            if (kullanici == null) return NotFound();

            // KORUMA: Kurucu banlanamaz!
            if (kullanici.Rol == KullaniciRol.Owner)
            {
                TempData["Error"] = "Sistem Kurucusu banlanamaz!";
                return RedirectToAction("Kullanicilar");
            }

            // 🛡️ ADMİN KORUMA KALKANI
            if (kullanici.Rol == KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner)
            {
                TempData["Error"] = "Sistem yöneticilerini banlayamazsınız!";
                return RedirectToAction("Kullanicilar");
            }

            if (id == currentUserId)
            {
                TempData["Error"] = "Kendi hesabınızı banlayamazsınız!";
                return RedirectToAction("Kullanicilar");
            }

            kullanici.BanNedeni = neden;
            kullanici.BanBitisTarihi = DateTime.Now.AddDays(gun);
            kullanici.IsBanned = true; 

            string adminIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP";
            _context.SistemLoglari.Add(new SistemLog {
                KullaniciAdi = (HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Admin") + " (Admin)",
                YapilanIslem = $"Kullanıcı banlandı: {kullanici.KullaniciAdi}",
                IpAdresi = adminIp,
                IslemTarihi = DateTime.Now
            });

            _context.Update(kullanici);
            await _context.SaveChangesAsync();

            // KULLANICIYA BAN MAİLİ GÖNDERME İŞLEMİ
            try
            {
                string gondericiMail = _config["SmtpSettings:Email"]; 
                string gondericiSifre = _config["SmtpSettings:Password"]; 

                var mail = new MailMessage();
                mail.From = new MailAddress(gondericiMail, "GorevLab Yönetimi");
                mail.To.Add(kullanici.Email ?? "info@gorevlab.com.tr"); 
                mail.Subject = "Hesabınız Geçici Olarak Askıya Alındı";
                mail.IsBodyHtml = true;

                mail.Body = $@"
                    <div style='font-family: Arial; padding: 20px; border: 1px solid #dc3545; border-radius: 10px;'>
                        <h2 style='color: #dc3545;'>Hesabınız Askıya Alındı!</h2>
                        <p>Sayın {kullanici.KullaniciAdi}, GorevLab sistem kurallarını ihlal ettiğiniz tespit edilmiştir.</p>
                        <hr>
                        <p><strong>Uzaklaştırma Nedeni:</strong> {neden}</p>
                        <p><strong>Uzaklaştırma Süresi:</strong> {gun} Gün</p>
                        <p><strong>Erişiminizin Açılacağı Tarih:</strong> {kullanici.BanBitisTarihi?.ToString("dd.MM.yyyy HH:mm")}</p>
                        <hr>
                        <p style='font-size: 12px; color: #666;'>Bu süre zarfında sisteme giriş yapamayacaksınız. Süre dolduğunda hesabınız otomatik olarak aktif edilecektir.</p>
                    </div>";

                using (var smtp = new SmtpClient("smtp.turkticaret.net", 587))
                {
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(gondericiMail, gondericiSifre);
                    smtp.EnableSsl = false;
                    smtp.Timeout = 20000;
                    smtp.Send(mail);
                }
            }
            catch (Exception ex)
            {
                // Mail hatası sessizce geçilsin
            }

            TempData["Success"] = "Kullanıcı başarıyla sistemden uzaklaştırıldı.";
            return RedirectToAction("Kullanicilar"); 
        }

        // --- BAN KALDIRMA METODU ---
        [HttpGet]
        public async Task<IActionResult> BanKaldir(int id)
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) return RedirectToAction("Index", "Home");

            var kullanici = await _context.Kullanicilar.FindAsync(id);
            if (kullanici != null)
            {
                kullanici.IsBanned = false;
                kullanici.BanNedeni = null;
                kullanici.BanBitisTarihi = null;
                
                _context.Update(kullanici);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Kullanıcının banı başarıyla kaldırıldı.";
            }
            return RedirectToAction("Kullanicilar");
        }

        // --- KULLANICIYI ADMİN YAP / NORMAL YAP ---
        public IActionResult RolDegistir(int id)
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) return RedirectToAction("Index", "Home");

            var currentUserId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var user = _context.Kullanicilar.Find(id);

            if (user != null)
            {
                if (user.Rol == KullaniciRol.Owner)
                {
                    TempData["Error"] = "Sistem Kurucusunun yetkisine müdahale edilemez!";
                    return RedirectToAction("Kullanicilar");
                }

                if (user.Rol == KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner)
                {
                    TempData["Error"] = "Sistem yöneticilerini silemez, banlayamaz veya yetkisini değiştiremezsiniz!";
                    return RedirectToAction("Kullanicilar");
                }

                if (id == currentUserId)
                {
                    TempData["Error"] = "Kendi yetkinizi kaldıramazsınız!";
                    return RedirectToAction("Kullanicilar");
                }

                user.Rol = user.Rol == KullaniciRol.Admin ? KullaniciRol.NormalKullanici : KullaniciRol.Admin;
                _context.SaveChanges();
                TempData["Success"] = "Kullanıcı yetkisi başarıyla güncellendi!";
            }
            return RedirectToAction("Kullanicilar");
        }

        // --- KULLANICIYI SİL ---
        public IActionResult KullaniciSil(int id)
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) return RedirectToAction("Index", "Home");

            var currentUserId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var user = _context.Kullanicilar.Find(id);

            if (user != null)
            {
                if (user.Rol == KullaniciRol.Owner)
                {
                    TempData["Error"] = "Sistem Kurucusu silinemez!";
                    return RedirectToAction("Kullanicilar");
                }

                if (user.Rol == KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner)
                {
                    TempData["Error"] = "Sistem yöneticilerini silemez, banlayamaz veya yetkisini değiştiremezsiniz!";
                    return RedirectToAction("Kullanicilar");
                }

                if (id == currentUserId)
                {
                    TempData["Error"] = "Kendi hesabınızı sistemden silemezsiniz!";
                    return RedirectToAction("Kullanicilar");
                }

                var gorevler = _context.Gorevler.Where(g => g.KullaniciId == id);
                _context.Gorevler.RemoveRange(gorevler);

                var destekMesajlari = _context.DestekMesajlari.Where(d => d.KullaniciId == id);
                _context.DestekMesajlari.RemoveRange(destekMesajlari);

                _context.Kullanicilar.Remove(user);
                
                _context.SistemLoglari.Add(new SistemLog {
                    KullaniciAdi = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Bilinmeyen Kullanıcı",
                    YapilanIslem = $"Kullanıcı silindi: {user.KullaniciAdi}",
                    IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP",
                    IslemTarihi = DateTime.Now
                });

                _context.SaveChanges();
                TempData["Success"] = "Kullanıcı, görevleri ve destek talepleri başarıyla silindi!";
            }
            return RedirectToAction("Kullanicilar");
        }

        // --- GİZLİ KURUCU ATAMA METODU ---
        public IActionResult TaciTak()
        {
            var user = _context.Kullanicilar.FirstOrDefault(u => u.Email == "ceviksemre@gmail.com");
            
            if (user != null)
            {
                user.Rol = KullaniciRol.Owner; 
                _context.SaveChanges(); 
                return Content("Tebrikler Kurucu! Taç başarıyla takıldı. Lütfen bu sekmeyi kapat, siteden ÇIKIŞ YAP ve TEKRAR GİRİŞ YAP.");
            }
            return Content("Hedef kullanıcı bulunamadı! Lütfen e-posta adresini kontrol et.");
        }

        // --- ADMİN TÜM GÖREVLERİ GÖRÜNTÜLEME (KURUCU GÖREVLERİ GİZLİ) ---
        public IActionResult TumGorevler()
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) 
                return RedirectToAction("Index", "Home");

            var sorgu = _context.Gorevler.Include(g => g.Kullanici).Include(g => g.Ekip).AsQueryable();

            // 🛡️ GÖREV KORUMASI: Kurucunun görevleri listeden gizlenir
            if (sessionRol != (int)KullaniciRol.Owner)
            {
                sorgu = sorgu.Where(g => g.Kullanici.Rol != KullaniciRol.Owner);
            }

            var tumGorevler = sorgu.OrderByDescending(g => g.Tarih).ToList();

            return View(tumGorevler);
        }

        // --- ADMİN GÖREV DETAY GETİR (MODAL İÇİN - KORUMALI) ---
        public IActionResult GorevDetayGetir(int id)
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) 
                return Unauthorized("Yetkisiz erişim!");

            var gorev = _context.Gorevler.Include(g => g.Kullanici).FirstOrDefault(g => g.Id == id);
            
            if (gorev == null) 
            {
                return NotFound("<div class='p-4 text-center text-red-500 font-bold'>Görev bulunamadı veya silinmiş!</div>");
            }

            // 🛡️ GÖREV KORUMASI: Kurucunun görevi modal ile çağrılırsa engelle
            if (gorev.Kullanici.Rol == KullaniciRol.Owner && sessionRol != (int)KullaniciRol.Owner)
            {
                return NotFound("<div class='p-4 text-center text-red-500 font-bold'>Bu görevi görüntüleme yetkiniz yok!</div>");
            }
            
            return PartialView("_AdminGorevDetayPartial", gorev);
        }

        // --- ADMİN: KULLANICININ GÖREVLERİNİ GÖRÜNTÜLEME (KORUMALI) ---
        public IActionResult KullaniciGorevleri(int id)
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            var kullanici = _context.Kullanicilar.Find(id);
            if (kullanici == null) return NotFound("Kullanıcı bulunamadı.");

            // 🛡️ GÖREV KORUMASI: Normal Admin Kurucunun profiline tıklarsa görev listesi açılmasın!
            if (kullanici.Rol == KullaniciRol.Owner && sessionRol != (int)KullaniciRol.Owner)
            {
                TempData["Error"] = "Sistem Kurucusunun görevlerini görüntüleyemezsiniz!";
                return RedirectToAction("Kullanicilar");
            }

            ViewBag.KullaniciAdSoyad = kullanici.Ad + " " + kullanici.Soyad;
            ViewBag.HedefKullaniciId = id;

            // Kullanıcının üye olduğu ekipler
            var ekipIds = _context.EkipUyeleri.Where(eu => eu.KullaniciId == id).Select(eu => eu.EkipId).ToList();

            var gorevler = _context.Gorevler
                                   .Include(g => g.Ekip)
                                   .Where(g => g.KullaniciId == id || (g.EkipId != null && ekipIds.Contains(g.EkipId.Value)))
                                   .OrderByDescending(g => g.Tarih)
                                   .ToList();

            return View(gorevler);
        }

        // --- ADMİN: KULLANICI GÖREVİ SİLME (KURUCU KORUMALI) ---
        public IActionResult KullaniciGoreviSil(int id)
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) 
                return RedirectToAction("Index", "Home");

            var gorev = _context.Gorevler.Include(g => g.Kullanici).FirstOrDefault(g => g.Id == id);
            
            if (gorev != null)
            {
                // 🛡️ KORUMA KALKANI: Silinmek istenen görev Kurucuya aitse ve silen kişi Kurucu değilse ENGELLE!
                if (gorev.Kullanici.Rol == KullaniciRol.Owner && sessionRol != (int)KullaniciRol.Owner)
                {
                    TempData["Error"] = "Sistem Kurucusuna ait görevlere müdahale edemezsiniz!";
                    string yetkisizSayfa = Request.Headers["Referer"].ToString();
                    return !string.IsNullOrEmpty(yetkisizSayfa) ? Redirect(yetkisizSayfa) : RedirectToAction("TumGorevler");
                }

                // LOG VE SİLME
                _context.SistemLoglari.Add(new SistemLog {
                    KullaniciAdi = (HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Admin") + " (Admin)",
                    YapilanIslem = $"Kullanıcının görevini yetkiyle sildi: {gorev.GorevAdi}",
                    IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP",
                    IslemTarihi = DateTime.Now
                });

                _context.Gorevler.Remove(gorev);
                _context.SaveChanges();
                TempData["Success"] = "Görev admin yetkisiyle başarıyla silindi.";
            }

            string oncekiSayfa = Request.Headers["Referer"].ToString();
            return !string.IsNullOrEmpty(oncekiSayfa) ? Redirect(oncekiSayfa) : RedirectToAction("TumGorevler");
        }

        // --- ADMİN: DESTEK TALEPLERİ LİSTESİ ---
        public IActionResult DestekTalepleri()
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) return RedirectToAction("Index", "Home");

            var mesajlar = _context.DestekMesajlari.Include(x => x.Kullanici).OrderByDescending(x => x.Tarih).ToList();
            return View(mesajlar);
        }

        // --- ADMİN: DESTEK TALEBİ CEVAPLAMA ---
        [HttpPost]
        public async Task<IActionResult> DestekCevapla(int mesajId, string cevap)
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) return RedirectToAction("Index", "Home");

            var destekMesaji = await _context.DestekMesajlari.Include(x => x.Kullanici).FirstOrDefaultAsync(x => x.Id == mesajId);
            
            if (destekMesaji != null)
            {
                destekMesaji.Cevap = cevap;
                destekMesaji.IsCevaplandi = true;
                
                _context.Update(destekMesaji);
                await _context.SaveChangesAsync();

                // Mail Gönderme İşlemi
                try
                {
                    string gondericiMail = _config["SmtpSettings:Email"]; 
                    string gondericiSifre = _config["SmtpSettings:Password"]; 

                    var mail = new MailMessage();
                    mail.From = new MailAddress(gondericiMail, "GorevLab Destek");
                    mail.To.Add(destekMesaji.Kullanici?.Email ?? "info@gorevlab.com.tr"); 
                    mail.Subject = "Destek Talebiniz Cevaplandı: " + destekMesaji.Konu;
                    mail.IsBodyHtml = true;

                    mail.Body = $@"
                        <div style='font-family: Arial; padding: 20px; border: 1px solid #4f46e5; border-radius: 10px;'>
                            <h2 style='color: #4f46e5;'>Destek Talebiniz Cevaplandı</h2>
                            <p>Sayın <b>{destekMesaji.Kullanici.Ad}</b>, bize ilettiğiniz destek talebiniz sistem yöneticilerimiz tarafından yanıtlanmıştır.</p>
                            <hr>
                            <p><strong>Sorunuz:</strong><br>{destekMesaji.Mesaj}</p>
                            <div style='background-color:#f8fafc; padding:15px; border-left:4px solid #4f46e5; margin-top:10px;'>
                                <strong>Yetkili Cevabı:</strong><br>{cevap}
                            </div>
                        </div>";

                    using (var smtp = new SmtpClient("smtp.turkticaret.net", 587))
                    {
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(gondericiMail, gondericiSifre);
                        smtp.EnableSsl = false;
                        smtp.Timeout = 20000;
                        smtp.Send(mail);
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Cevap kaydedildi fakat mail gönderilemedi: {ex.Message}";
                    return RedirectToAction("DestekTalepleri");
                }

                TempData["Success"] = "Destek talebi başarıyla cevaplandı ve kullanıcıya mail gönderildi!";
            }
            return RedirectToAction("DestekTalepleri");
            
        }
        
        // --- ADMİN (KURUCU): SİSTEM LOGLARINI İZLEME EKRANI ---
        public IActionResult Loglar()
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            
            // Sadece Sistem Kurucusu görebilsin
            if (sessionRol != (int)KullaniciRol.Owner) 
            {
                TempData["Error"] = "Yetkisiz Erişim: Bu sayfayı sadece sistem kurucusu görüntüleyebilir!";
                return RedirectToAction("Index", "Home");
            }

            var loglar = _context.SistemLoglari
                                 .OrderByDescending(l => l.IslemTarihi)
                                 .Take(200)
                                 .ToList();

            return View(loglar);
        }
        // --- ADMİN (KURUCU): LOGLARI TEMİZLE ---
        public IActionResult LogTemizle()
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            
            // Sadece Sistem Kurucusu temizleyebilir
            if (sessionRol != (int)KullaniciRol.Owner) 
            {
                TempData["Error"] = "Yetkisiz İşlem: Log kayıtlarını sadece sistem kurucusu silebilir!";
                return RedirectToAction("Index", "Home");
            }

            var tumLoglar = _context.SistemLoglari.ToList();
            if (tumLoglar.Any())
            {
                _context.SistemLoglari.RemoveRange(tumLoglar);
                _context.SaveChanges();
                TempData["Success"] = "Tüm sistem logları kalıcı olarak başarıyla temizlendi.";
            }

            return RedirectToAction("Loglar");
        }

        // --- ADMİN: DESTEK TALEBİ SİLME ---
        public IActionResult DestekTalebiSil(int id)
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) 
                return RedirectToAction("Index", "Home");

            var mesaj = _context.DestekMesajlari.Find(id);
            if (mesaj != null)
            {
                _context.SistemLoglari.Add(new SistemLog {
                    KullaniciAdi = (HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Admin") + " (Admin)",
                    YapilanIslem = $"Destek talebi kalıcı olarak silindi: {mesaj.Konu}",
                    IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP",
                    IslemTarihi = DateTime.Now
                });

                _context.DestekMesajlari.Remove(mesaj);
                _context.SaveChanges();
                TempData["Success"] = "Destek talebi başarıyla temizlendi.";
            }
            
            return RedirectToAction("DestekTalepleri");
        }
    }
}