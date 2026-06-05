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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.SignalR;
using GorevTakipSistemi.Hubs;

namespace GorevTakipSistemi.Controllers
{
    /// <summary>
    /// Yonetim paneli islemlerini yoneten controller.
    /// Kullanici yonetimi, gorev yonetimi, destek talepleri, sistem loglari ve bildirim islemlerini icerir.
    /// </summary>
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;
        private readonly IHubContext<BildirimHub> _hubContext;

        /// <summary>
        /// AdminController yapilandirici metodu. Gerekli servisleri bagimlilik enjeksiyonu ile alir.
        /// </summary>
        /// <param name="context">Veritabani erisim baglami.</param>
        /// <param name="config">Uygulama yapilandirma ayarlari.</param>
        /// <param name="cache">Bellek ici onbellekleme servisi.</param>
        /// <param name="hubContext">SignalR bildirim hub baglami.</param>
        public AdminController(AppDbContext context, IConfiguration config, IMemoryCache cache, IHubContext<BildirimHub> hubContext)
        {
            _context = context;
            _config = config;
            _cache = cache;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Sistemdeki tum kullanicilari listeler. Admin ve Kurucu rolune sahip kullanicilar erisebilir.
        /// Her kullanicinin gorev istatistikleri ve ban durumu ile birlikte goruntulenir.
        /// </summary>
        /// <returns>Kullanici yonetim gorunumunu dondurur.</returns>
        public IActionResult Kullanicilar()
        {
            var rol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;

            // Yetki kontrolu: Yalnizca Admin veya Kurucu erisebilir
            if (rol != (int)KullaniciRol.Admin && rol != (int)KullaniciRol.Owner) 
            {
                TempData["Error"] = "Bu alana erişim yetkiniz bulunmamaktadır!";
                return RedirectToAction("Index", "Home");
            }

            // Tum kullanicilari gorev istatistikleri ve ban bilgileri ile birlikte getir
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

        /// <summary>
        /// Sistemin bakim modunu aktif veya pasif hale getirir.
        /// Yalnizca Kurucu yetkisine sahip kullanicilar tarafindan calistirabilir.
        /// </summary>
        /// <returns>Onceki sayfaya yonlendirme yapar.</returns>
        [HttpPost]
        public IActionResult BakimModuTetikle()
        {
            var rol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (rol != (int)KullaniciRol.Owner)
            {
                TempData["Error"] = "Bu işlem için kurucu yetkisi gereklidir.";
                return RedirectToAction("Index", "Home");
            }

            // Bakim modu durumunu tersine cevir
            GorevTakipSistemi.Models.SiteSettings.BakimModuAktif = !GorevTakipSistemi.Models.SiteSettings.BakimModuAktif;

            TempData["Success"] = GorevTakipSistemi.Models.SiteSettings.BakimModuAktif 
                ? "Sistem başarıyla BAKIM moduna alındı." 
                : "Sistem tekrar YAYINA alındı.";
                
            return Redirect(Request.Headers["Referer"].ToString() ?? "/Admin/Loglar");
        }

        /// <summary>
        /// Belirtilen kullaniciyi sureli veya kalici olarak sistemden uzaklastirir (banlar).
        /// Banlanan kullaniciya e-posta bildirimi gonderilir ve islem sistem loguna kaydedilir.
        /// Kurucu hesabi banlanamaz, Admin hesabi yalnizca Kurucu tarafindan banlanabilir.
        /// </summary>
        /// <param name="id">Banlanacak kullanicinin benzersiz kimlik numarasi.</param>
        /// <param name="neden">Banlama isleminin gerekce metni.</param>
        /// <param name="gun">Ban suresi (gun cinsinden). Null veya 0 ise kalici ban uygulanir.</param>
        /// <returns>Kullanici listesine yonlendirme yapar.</returns>
        [HttpPost]
        public async Task<IActionResult> KullaniciBanla(int id, string neden, int? gun)
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) return RedirectToAction("Index", "Home");

            var currentUserId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var kullanici = await _context.Kullanicilar.FindAsync(id);

            if (kullanici == null) return NotFound();

            // Kurucu hesabi ban isleminden muaftir
            if (kullanici.Rol == KullaniciRol.Owner)
            {
                TempData["Error"] = "Sistem Kurucusu banlanamaz!";
                return RedirectToAction("Kullanicilar");
            }

            // Admin hesabi yalnizca Kurucu tarafindan banlanabilir
            if (kullanici.Rol == KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner)
            {
                TempData["Error"] = "Sistem yöneticilerini banlayamazsınız!";
                return RedirectToAction("Kullanicilar");
            }

            // Kullanicinin kendi hesabini banlamasi engellenir
            if (id == currentUserId)
            {
                TempData["Error"] = "Kendi hesabınızı banlayamazsınız!";
                return RedirectToAction("Kullanicilar");
            }

            // Ban bilgilerini guncelle: sure belirtilmemisse kalici ban uygulanir
            kullanici.BanNedeni = neden;
            kullanici.BanBitisTarihi = gun.HasValue && gun.Value > 0 ? DateTime.Now.AddDays(gun.Value) : null;
            kullanici.IsBanned = true; 

            // Ban islemini sistem loguna kaydet
            string adminIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP";
            _context.SistemLoglari.Add(new SistemLog {
                KullaniciAdi = (HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Admin") + " (Admin)",
                YapilanIslem = $"Kullanıcı banlandı: {kullanici.KullaniciAdi} (Süre: {(gun.HasValue ? gun.Value + " gün" : "Kalıcı")})",
                IpAdresi = adminIp,
                IslemTarihi = DateTime.Now
            });

            _context.Update(kullanici);
            await _context.SaveChangesAsync();

            // Banlanan kullaniciya bilgilendirme e-postasi gonder
            try
            {
                string gondericiMail = _config["SmtpSettings:Email"]; 
                string gondericiSifre = _config["SmtpSettings:Password"]; 

                string banSuresiMetin = gun.HasValue && gun.Value > 0 ? $"{gun.Value} Gün" : "Süresiz (Kalıcı)";
                string banBitisMetin = gun.HasValue && gun.Value > 0 ? kullanici.BanBitisTarihi?.ToString("dd.MM.yyyy HH:mm") : "Belirsiz / Açılmayacak";
                string aciklamaMetin = gun.HasValue && gun.Value > 0 ? "Bu süre zarfında sisteme giriş yapamayacaksınız. Süre dolduğunda hesabınız otomatik olarak aktif edilecektir." : "Sistem kurallarını ağır şekilde ihlal ettiğiniz için hesabınız kalıcı olarak kapatılmıştır.";

                var mail = new MailMessage();
                mail.From = new MailAddress(gondericiMail, "GorevLab Yönetimi");
                mail.To.Add(kullanici.Email ?? "info@gorevlab.com.tr"); 
                mail.Subject = gun.HasValue && gun.Value > 0 ? "Hesabınız Geçici Olarak Askıya Alındı" : "Hesabınız Kalıcı Olarak Kapatıldı!";
                mail.IsBodyHtml = true;

                mail.Body = $@"
                    <div style='font-family: Arial; padding: 20px; border: 1px solid #dc3545; border-radius: 10px;'>
                        <h2 style='color: #dc3545;'>Hesabınız Askıya Alındı!</h2>
                        <p>Sayın {kullanici.KullaniciAdi}, GorevLab sistem kurallarını ihlal ettiğiniz tespit edilmiştir.</p>
                        <hr>
                        <p><strong>Uzaklaştırma Nedeni:</strong> {neden}</p>
                        <p><strong>Uzaklaştırma Süresi:</strong> {banSuresiMetin}</p>
                        <p><strong>Erişiminizin Açılacağı Tarih:</strong> {banBitisMetin}</p>
                        <hr>
                        <p style='font-size: 12px; color: #666;'>{aciklamaMetin}</p>
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
                // E-posta gonderim hatasi sessizce gecilir; ban islemi basarili olmustur
            }

            TempData["Success"] = "Kullanıcı başarıyla sistemden uzaklaştırıldı.";
            return RedirectToAction("Kullanicilar"); 
        }

        /// <summary>
        /// Belirtilen kullanicinin ban durumunu kaldirir ve hesabini tekrar aktif hale getirir.
        /// Yalnizca Admin veya Kurucu yetkisine sahip kullanicilar calistirabilir.
        /// </summary>
        /// <param name="id">Bani kaldirilacak kullanicinin benzersiz kimlik numarasi.</param>
        /// <returns>Kullanici listesine yonlendirme yapar.</returns>
        [HttpGet]
        public async Task<IActionResult> BanKaldir(int id)
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) return RedirectToAction("Index", "Home");

            var kullanici = await _context.Kullanicilar.FindAsync(id);
            if (kullanici != null)
            {
                // Ban bilgilerini sifirla
                kullanici.IsBanned = false;
                kullanici.BanNedeni = null;
                kullanici.BanBitisTarihi = null;
                
                _context.Update(kullanici);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Kullanıcının banı başarıyla kaldırıldı.";
            }
            return RedirectToAction("Kullanicilar");
        }

        /// <summary>
        /// Belirtilen kullanicinin rolunu Admin ve Normal Kullanici arasinda degistirir.
        /// Kurucu hesabinin rolu degistirilemez. Admin hesabinin rolu yalnizca Kurucu tarafindan degistirilebilir.
        /// Kullanici kendi rol yetkisini degistiremez.
        /// </summary>
        /// <param name="id">Rol degisikligi yapilacak kullanicinin benzersiz kimlik numarasi.</param>
        /// <returns>Kullanici listesine yonlendirme yapar.</returns>
        public IActionResult RolDegistir(int id)
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) return RedirectToAction("Index", "Home");

            var currentUserId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var user = _context.Kullanicilar.Find(id);

            if (user != null)
            {
                // Kurucu rolune mudahale edilemez
                if (user.Rol == KullaniciRol.Owner)
                {
                    TempData["Error"] = "Sistem Kurucusunun yetkisine müdahale edilemez!";
                    return RedirectToAction("Kullanicilar");
                }

                // Admin hesabinin rolu yalnizca Kurucu tarafindan degistirilebilir
                if (user.Rol == KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner)
                {
                    TempData["Error"] = "Sistem yöneticilerini silemez, banlayamaz veya yetkisini değiştiremezsiniz!";
                    return RedirectToAction("Kullanicilar");
                }

                // Kullanicinin kendi yetkisini degistirmesi engellenir
                if (id == currentUserId)
                {
                    TempData["Error"] = "Kendi yetkinizi kaldıramazsınız!";
                    return RedirectToAction("Kullanicilar");
                }

                // Rolu tersine cevir: Admin ise Normal, Normal ise Admin yap
                user.Rol = user.Rol == KullaniciRol.Admin ? KullaniciRol.NormalKullanici : KullaniciRol.Admin;
                _context.SaveChanges();
                TempData["Success"] = "Kullanıcı yetkisi başarıyla güncellendi!";
            }
            return RedirectToAction("Kullanicilar");
        }

        /// <summary>
        /// Belirtilen kullaniciyi, iliskili gorevleri ve destek taleplerini kalici olarak siler.
        /// Kurucu silinemez. Admin yalnizca Kurucu tarafindan silinebilir.
        /// Kullanici kendi hesabini silemez. Silme islemi sistem loguna kaydedilir.
        /// </summary>
        /// <param name="id">Silinecek kullanicinin benzersiz kimlik numarasi.</param>
        /// <returns>Kullanici listesine yonlendirme yapar.</returns>
        public IActionResult KullaniciSil(int id)
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) return RedirectToAction("Index", "Home");

            var currentUserId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var user = _context.Kullanicilar.Find(id);

            if (user != null)
            {
                // Kurucu hesabi silinemez
                if (user.Rol == KullaniciRol.Owner)
                {
                    TempData["Error"] = "Sistem Kurucusu silinemez!";
                    return RedirectToAction("Kullanicilar");
                }

                // Admin hesabi yalnizca Kurucu tarafindan silinebilir
                if (user.Rol == KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner)
                {
                    TempData["Error"] = "Sistem yöneticilerini silemez, banlayamaz veya yetkisini değiştiremezsiniz!";
                    return RedirectToAction("Kullanicilar");
                }

                // Kullanicinin kendi hesabini silmesi engellenir
                if (id == currentUserId)
                {
                    TempData["Error"] = "Kendi hesabınızı sistemden silemezsiniz!";
                    return RedirectToAction("Kullanicilar");
                }

                // Kullaniciya ait gorevleri sil
                var gorevler = _context.Gorevler.Where(g => g.KullaniciId == id);
                _context.Gorevler.RemoveRange(gorevler);

                // Kullaniciya ait destek mesajlarini sil
                var destekMesajlari = _context.DestekMesajlari.Where(d => d.KullaniciId == id);
                _context.DestekMesajlari.RemoveRange(destekMesajlari);

                _context.Kullanicilar.Remove(user);
                
                // Silme islemini sistem loguna kaydet
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

        /// <summary>
        /// Ozel kurucu atama metodu. Tanimli e-posta adresine sahip kullaniciya Kurucu (Owner) rolu atar.
        /// Bu islem sonrasi kullanicinin oturumu kapatip yeniden giris yapmasi gerekmektedir.
        /// </summary>
        /// <returns>Islem sonucunu icerik olarak dondurur.</returns>
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

        /// <summary>
        /// Sistemdeki tum gorevleri kullanici ve ekip bilgileriyle birlikte listeler.
        /// Admin yetkisine sahip kullanicilar icin Kurucu gorevleri gizlenir.
        /// Kurucu yetkisine sahip kullanicilar tum gorevleri gorebilir.
        /// </summary>
        /// <returns>Tum gorevlerin listelendigi gorunumu dondurur.</returns>
        public IActionResult TumGorevler()
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) 
                return RedirectToAction("Index", "Home");

            var sorgu = _context.Gorevler.Include(g => g.Kullanici).Include(g => g.Ekip).AsQueryable();

            // Kurucu disindaki yoneticiler icin Kurucu gorevleri filtrelenir
            if (sessionRol != (int)KullaniciRol.Owner)
            {
                sorgu = sorgu.Where(g => g.Kullanici.Rol != KullaniciRol.Owner);
            }

            var tumGorevler = sorgu.OrderByDescending(g => g.Tarih).ToList();

            return View(tumGorevler);
        }

        /// <summary>
        /// Belirtilen gorevin detay bilgilerini modal pencere icin getirir.
        /// Kurucu gorevleri yalnizca Kurucu tarafindan goruntulenebilir.
        /// </summary>
        /// <param name="id">Detayi goruntulenecek gorevin benzersiz kimlik numarasi.</param>
        /// <returns>Gorev detay kismi gorunumunu (partial view) dondurur.</returns>
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

            // Kurucu gorevleri yalnizca Kurucu tarafindan goruntulenebilir
            if (gorev.Kullanici.Rol == KullaniciRol.Owner && sessionRol != (int)KullaniciRol.Owner)
            {
                return NotFound("<div class='p-4 text-center text-red-500 font-bold'>Bu görevi görüntüleme yetkiniz yok!</div>");
            }
            
            return PartialView("_AdminGorevDetayPartial", gorev);
        }

        /// <summary>
        /// Belirtilen kullaniciya ait gorevleri listeler. Bireysel ve ekip gorevlerini icerir.
        /// Kurucu gorevleri yalnizca Kurucu tarafindan goruntulenebilir.
        /// </summary>
        /// <param name="id">Gorevleri listelenmek istenen kullanicinin benzersiz kimlik numarasi.</param>
        /// <returns>Kullanicinin gorev listesini iceren gorunumu dondurur.</returns>
        public IActionResult KullaniciGorevleri(int id)
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            var kullanici = _context.Kullanicilar.Find(id);
            if (kullanici == null) return NotFound("Kullanıcı bulunamadı.");

            // Kurucu gorevleri yalnizca Kurucu tarafindan goruntulenebilir
            if (kullanici.Rol == KullaniciRol.Owner && sessionRol != (int)KullaniciRol.Owner)
            {
                TempData["Error"] = "Sistem Kurucusunun görevlerini görüntüleyemezsiniz!";
                return RedirectToAction("Kullanicilar");
            }

            ViewBag.KullaniciAdSoyad = kullanici.Ad + " " + kullanici.Soyad;
            ViewBag.HedefKullaniciId = id;

            // Kullanicinin uye oldugu ekiplerin kimlik numaralarini getir
            var ekipIds = _context.EkipUyeleri.Where(eu => eu.KullaniciId == id).Select(eu => eu.EkipId).ToList();

            // Bireysel ve ekip gorevlerini birlikte sorgula
            var gorevler = _context.Gorevler
                                   .Include(g => g.Ekip)
                                   .Where(g => g.KullaniciId == id || (g.EkipId != null && ekipIds.Contains(g.EkipId.Value)))
                                   .OrderByDescending(g => g.Tarih)
                                   .ToList();

            return View(gorevler);
        }

        /// <summary>
        /// Admin yetkisiyle belirtilen gorevi siler. Kurucu gorevleri korunur.
        /// Kurucu gorevleri yalnizca Kurucu tarafindan silinebilir.
        /// Silme islemi sistem loguna kaydedilir.
        /// </summary>
        /// <param name="id">Silinecek gorevin benzersiz kimlik numarasi.</param>
        /// <returns>Onceki sayfaya veya tum gorevler listesine yonlendirme yapar.</returns>
        public IActionResult KullaniciGoreviSil(int id)
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) 
                return RedirectToAction("Index", "Home");

            var gorev = _context.Gorevler.Include(g => g.Kullanici).FirstOrDefault(g => g.Id == id);
            
            if (gorev != null)
            {
                // Kurucu gorevleri yalnizca Kurucu tarafindan silinebilir
                if (gorev.Kullanici.Rol == KullaniciRol.Owner && sessionRol != (int)KullaniciRol.Owner)
                {
                    TempData["Error"] = "Sistem Kurucusuna ait görevlere müdahale edemezsiniz!";
                    string yetkisizSayfa = Request.Headers["Referer"].ToString();
                    return !string.IsNullOrEmpty(yetkisizSayfa) ? Redirect(yetkisizSayfa) : RedirectToAction("TumGorevler");
                }

                // Silme islemini sistem loguna kaydet
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

            // Onceki sayfaya yonlendir, mevcut degilse tum gorevler sayfasina git
            string oncekiSayfa = Request.Headers["Referer"].ToString();
            return !string.IsNullOrEmpty(oncekiSayfa) ? Redirect(oncekiSayfa) : RedirectToAction("TumGorevler");
        }

        /// <summary>
        /// Sistemdeki tum destek taleplerini kullanici bilgileriyle birlikte listeler.
        /// Yalnizca Admin veya Kurucu yetkisine sahip kullanicilar erisebilir.
        /// </summary>
        /// <returns>Destek talepleri gorunumunu dondurur.</returns>
        public IActionResult DestekTalepleri()
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) return RedirectToAction("Index", "Home");

            var mesajlar = _context.DestekMesajlari.Include(x => x.Kullanici).OrderByDescending(x => x.Tarih).ToList();
            return View(mesajlar);
        }

        /// <summary>
        /// Belirtilen destek talebine yanit verir, yaniti veritabanina kaydeder.
        /// Kullaniciya e-posta ve SignalR uzerinden gercek zamanli bildirim gonderilir.
        /// </summary>
        /// <param name="mesajId">Cevaplanacak destek mesajinin benzersiz kimlik numarasi.</param>
        /// <param name="cevap">Destek talebine verilecek yanit metni.</param>
        /// <returns>Destek talepleri sayfasina yonlendirme yapar.</returns>
        [HttpPost]
        public async Task<IActionResult> DestekCevapla(int mesajId, string cevap)
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) return RedirectToAction("Index", "Home");

            var destekMesaji = await _context.DestekMesajlari.Include(x => x.Kullanici).FirstOrDefaultAsync(x => x.Id == mesajId);
            
            if (destekMesaji != null)
            {
                // Cevap bilgilerini guncelle
                destekMesaji.Cevap = cevap;
                destekMesaji.IsCevaplandi = true;
                
                _context.Update(destekMesaji);
                await _context.SaveChangesAsync();

                // Kullaniciya cevap bilgilendirme e-postasi gonder
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

                // SignalR uzerinden kullaniciya gercek zamanli bildirim gonder
                if (destekMesaji.KullaniciId != null)
                {
                    await _hubContext.Clients.Group(destekMesaji.KullaniciId.ToString()).SendAsync("YeniBildirim", 
                        "Destek Talebiniz Cevaplandı", 
                        $"'{destekMesaji.Konu}' konulu talebiniz yöneticilerimiz tarafından yanıtlandı.",
                        "info",
                        "/Iletisim/DestekTaleplerim");
                }
            }
            return RedirectToAction("DestekTalepleri");
            
        }
        
        /// <summary>
        /// Sistem loglarini goruntuleme ekranini acar. Son 200 log kaydini tarihe gore sirali listeler.
        /// Yalnizca Kurucu yetkisine sahip kullanicilar erisebilir.
        /// </summary>
        /// <returns>Sistem loglari gorunumunu dondurur.</returns>
        public IActionResult Loglar()
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            
            // Yalnizca Kurucu erisim yetkisine sahiptir
            if (sessionRol != (int)KullaniciRol.Owner) 
            {
                TempData["Error"] = "Yetkisiz Erişim: Bu sayfayı sadece sistem kurucusu görüntüleyebilir!";
                return RedirectToAction("Index", "Home");
            }

            // Son 200 log kaydini tarihe gore azalan sirada getir
            var loglar = _context.SistemLoglari
                                 .OrderByDescending(l => l.IslemTarihi)
                                 .Take(200)
                                 .ToList();

            return View(loglar);
        }

        /// <summary>
        /// Sistem loglarini temizleme islemi icin dogrulama kodu uretir ve kurucu e-posta adresine gonderir.
        /// Uretilen kod 5 dakika sureli olarak bellekte saklanir.
        /// Yalnizca Kurucu yetkisine sahip kullanicilar calistirabilir.
        /// </summary>
        /// <returns>Islem sonucunu JSON formatinda dondurur.</returns>
        [HttpPost]
        public IActionResult LogTemizleTalep()
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Owner) 
                return Json(new { success = false, message = "Yetkisiz İşlem!" });

            // Rastgele 6 haneli dogrulama kodu uret
            var random = new Random();
            string dogrulamaKodu = random.Next(100000, 999999).ToString();

            // Dogrulama kodunu 5 dakika sureli olarak bellege kaydet
            _cache.Set("LogTemizleKodu", dogrulamaKodu, TimeSpan.FromMinutes(5));

            // Kurucu e-posta adresine dogrulama kodunu gonder
            string adminEmail = "info@gorevlab.com.tr";
            
            try
            {
                System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
                mail.From = new System.Net.Mail.MailAddress("info@gorevlab.com.tr", "GorevLab Sistem");
                mail.To.Add(adminEmail);
                mail.Subject = "Sistem Loglari Silme Islemi Dogrulama Kodu";
                mail.IsBodyHtml = true;
                mail.Body = $"<p>Sistem loglarını tamamen silmek için bir talepte bulunuldu.</p><b>Doğrulama Kodunuz: {dogrulamaKodu}</b><p>Bu kodu kimseyle paylaşmayın. Kod 5 dakika geçerlidir.</p>";

                System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient("smtp.turkticaret.net", 587);
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new System.Net.NetworkCredential(_config["SmtpSettings:Email"], _config["SmtpSettings:Password"]);
                smtp.EnableSsl = true;
                smtp.Send(mail);
            }
            catch 
            {
                return Json(new { success = false, message = "E-Posta gönderilirken bir hata oluştu!" });
            }

            return Json(new { success = true, message = "Doğrulama kodu kurucu e-posta adresine gönderildi." });
        }

        /// <summary>
        /// Dogrulama kodu kontrolu yaparak tum sistem loglarini kalici olarak siler.
        /// Kod dogrulamasi basarili olursa tum loglar temizlenir ve dogrulama kodu bellekten kaldirilir.
        /// Yalnizca Kurucu yetkisine sahip kullanicilar calistirabilir.
        /// </summary>
        /// <param name="kod">E-posta ile gonderilen 6 haneli dogrulama kodu.</param>
        /// <returns>Islem sonucunu JSON formatinda dondurur.</returns>
        [HttpPost]
        public IActionResult LogTemizle(string kod)
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Owner) 
                return Json(new { success = false, message = "Yetkisiz İşlem!" });

            // Bellekteki dogrulama kodunu kontrol et
            if (!_cache.TryGetValue("LogTemizleKodu", out string beklenenKod) || kod != beklenenKod)
            {
                return Json(new { success = false, message = "Hatalı veya süresi dolmuş doğrulama kodu!" });
            }

            // Kod dogrulandi, kullanilmis kodu bellekten kaldir
            _cache.Remove("LogTemizleKodu");

            // Tum sistem loglarini veritabanindan sil
            var tumLoglar = _context.SistemLoglari.ToList();
            if (tumLoglar.Any())
            {
                _context.SistemLoglari.RemoveRange(tumLoglar);
                _context.SaveChanges();
            }

            return Json(new { success = true, message = "Tüm sistem logları başarıyla temizlendi." });
        }

        /// <summary>
        /// Belirtilen destek talebini kalici olarak siler ve islemi sistem loguna kaydeder.
        /// Yalnizca Admin veya Kurucu yetkisine sahip kullanicilar calistirabilir.
        /// </summary>
        /// <param name="id">Silinecek destek talebinin benzersiz kimlik numarasi.</param>
        /// <returns>Destek talepleri sayfasina yonlendirme yapar.</returns>
        public IActionResult DestekTalebiSil(int id)
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) 
                return RedirectToAction("Index", "Home");

            var mesaj = _context.DestekMesajlari.Find(id);
            if (mesaj != null)
            {
                // Silme islemini sistem loguna kaydet
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
        
        /// <summary>
        /// Belirtilen kullaniciya veya tum aktif kullanicilara sistem bildirimi gonderir.
        /// Bildirimler veritabanina kaydedilir ve SignalR uzerinden gercek zamanli iletilir.
        /// Islem sistem loguna kaydedilir.
        /// </summary>
        /// <param name="kullaniciId">Bildirim gonderilecek kullanicinin kimlik numarasi. Null ise tum kullanicilara gonderilir.</param>
        /// <param name="mesaj">Bildirim mesaj icerigi.</param>
        /// <param name="baslik">Bildirim basligi. Varsayilan deger: "Sistem Bildirimi".</param>
        /// <returns>Islem sonucunu JSON formatinda dondurur.</returns>
        [HttpPost]
        public async Task<IActionResult> BildirimGonder(int? kullaniciId, string mesaj, string baslik = "Sistem Bildirimi")
        {
            var sessionRol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
            if (sessionRol != (int)KullaniciRol.Admin && sessionRol != (int)KullaniciRol.Owner) 
                return Json(new { success = false, message = "Yetkisiz İşlem!" });

            if (string.IsNullOrWhiteSpace(mesaj))
                return Json(new { success = false, message = "Mesaj boş olamaz!" });

            if (kullaniciId.HasValue && kullaniciId.Value > 0)
            {
                // Belirtilen kullaniciya bildirim olustur ve kaydet
                _context.Bildirimler.Add(new Bildirim {
                    KullaniciId = kullaniciId.Value,
                    Mesaj = $"{baslik}: {mesaj}",
                    Url = "/Home/Index"
                });
                await _context.SaveChangesAsync();
                
                // SignalR uzerinden kullaniciya gercek zamanli bildirim gonder
                await _hubContext.Clients.Group(kullaniciId.Value.ToString()).SendAsync("YeniBildirim", baslik, mesaj, "success", "/Home/Index");
            }
            else
            {
                // Banli olmayanlar dahil tum aktif kullanicilara bildirim olustur
                var aktifKullanicilar = _context.Kullanicilar.Where(u => !u.IsBanned).Select(u => u.Id).ToList();
                foreach(var id in aktifKullanicilar)
                {
                    _context.Bildirimler.Add(new Bildirim {
                        KullaniciId = id,
                        Mesaj = $"{baslik}: {mesaj}",
                        Url = "/Home/Index"
                    });
                }
                await _context.SaveChangesAsync();
                
                // SignalR uzerinden tum bagli istemcilere toplu bildirim gonder
                await _hubContext.Clients.All.SendAsync("YeniBildirim", baslik, mesaj, "success", "/Home/Index");
            }

            // Bildirim gonderim islemini sistem loguna kaydet
            _context.SistemLoglari.Add(new SistemLog {
                KullaniciAdi = (HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Admin") + " (Admin)",
                YapilanIslem = $"Sistem bildirimi gönderildi. Hedef: {(kullaniciId.HasValue ? kullaniciId.ToString() : "Tümü")}",
                IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP",
                IslemTarihi = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Bildirim başarıyla gönderildi." });
        }
    }
}