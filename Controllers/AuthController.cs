using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using GorevTakipSistemi.Data;
using GorevTakipSistemi.Models;
using System.Text;
using System.Linq;
using System;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace GorevTakipSistemi.Controllers
{
    /// <summary>
    /// Kimlik dogrulama islemlerini yoneten controller.
    /// Kayit, giris, cikis, e-posta dogrulama, sifre sifirlama ve kullanici adi hatirlatma islevlerini icerir.
    /// </summary>
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;

        /// <summary>
        /// AuthController yapilandirici metodu. Gerekli servisleri bagimlilik enjeksiyonu ile alir.
        /// </summary>
        /// <param name="context">Veritabani erisim baglami.</param>
        /// <param name="cache">IP tabanli hiz sinirlamasi icin bellek ici onbellekleme servisi.</param>
        /// <param name="config">Uygulama yapilandirma ayarlari.</param>
        public AuthController(AppDbContext context, IMemoryCache cache, IConfiguration config)
        {
            _context = context;
            _cache = cache;
            _config = config;
        }

        /// <summary>
        /// Kayit formunun goruntulendigi sayfayi dondurur.
        /// </summary>
        /// <returns>Kayit formu gorunumunu dondurur.</returns>
        public IActionResult Register() { return View(); }

        /// <summary>
        /// Yeni kullanici kayit islemini gerceklestirir.
        /// IP tabanli hiz sinirlamasi, reCAPTCHA dogrulamasi, KVKK onayi ve veri dogrulama kontrolleri uygulanir.
        /// Basarili kayit sonrasi e-posta aktivasyon linki gonderilir.
        /// </summary>
        /// <param name="Ad">Kullanicinin adi.</param>
        /// <param name="Soyad">Kullanicinin soyadi.</param>
        /// <param name="KullaniciAdi">Sistemde kullanilacak benzersiz kullanici adi.</param>
        /// <param name="Email">Kullanicinin e-posta adresi.</param>
        /// <param name="Sifre">Kullanicinin belirledigi sifre.</param>
        /// <param name="KvkkOnay">KVKK ve gizlilik sozlesmesi onay durumu.</param>
        /// <returns>Basarili kayit sonrasi giris sayfasina yonlendirir.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string Ad, string Soyad, string KullaniciAdi, string Email, string Sifre, string KvkkOnay)
        {
            // Form verilerini geri doldurmak icin ViewBag'e aktar
            ViewBag.Ad = Ad;
            ViewBag.Soyad = Soyad;
            ViewBag.KullaniciAdi = KullaniciAdi;
            ViewBag.Email = Email;
            
            // Adim 1: IP tabanli hiz sinirlamasi (Rate Limiting) kontrolu
            string musteriIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP";
            string cacheKey = $"Kayit_IP_{musteriIp}";

            // Ayni IP adresinden 15 dakika icinde tekrar kayit girisimi engellenir
            if (_cache.TryGetValue(cacheKey, out DateTime sonKayitZamani))
            {
                TempData["Error"] = "Güvenlik İhlali! Aynı internet ağından (IP) 15 dakikada sadece 1 kez kayıt olabilirsiniz.";

                // Yoneticiye supheli aktivite uyari e-postasi gonder
                try
                {
                    MailMessage uyariMail = new MailMessage();
                    uyariMail.From = new MailAddress("info@gorevlab.com.tr", "GorevLab Güvenlik Sistemi");
                    uyariMail.To.Add("info@gorevlab.com.tr");
                    uyariMail.Subject = "UYARI: Supheli Pes Pese Kayit Denemesi!";
                    uyariMail.IsBodyHtml = true;
                    uyariMail.Body = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 550px; margin: 0 auto; padding: 20px; border: 2px solid #ef4444; border-radius: 12px; background-color: #fffafb;'>
                            <h2 style='color: #b91c1c; margin-top: 0;'>Supheli Aktivite Algilandi</h2>
                            <p style='color: #334155; font-size: 15px;'>Sisteminizdeki kayıt formuna aynı internet ağından üst üste üyelik isteği gönderildi.</p>
                            <div style='background-color: #fee2e2; border-left: 5px solid #ef4444; padding: 12px; margin: 15px 0; border-radius: 4px;'>
                                <b>Şüpheli IP Adresi:</b> <span style='color: #b91c1c; font-family: monospace; font-size: 16px;'>{musteriIp}</span>
                            </div>
                            <p style='color: #475569; font-size: 14px;'>Güvenlik duvarı bu IP adresini otomatik olarak <b>15 dakika boyunca</b> kilitlemiştir. Bu süre zarfında yeni kayıt açamazlar.</p>
                            <hr style='border: 0; border-top: 1px solid #fca5a5; margin: 20px 0;'>
                            <small style='color: #94a3b8; font-size: 12px;'>GorevLab Shield Tier-1 Koruması tarafından otomatik üretilmiştir.</small>
                        </div>";

                    SmtpClient smtp = new SmtpClient("smtp.turkticaret.net", 587);
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(_config["SmtpSettings:Email"], _config["SmtpSettings:Password"]); 
                    smtp.EnableSsl = true; 
                    smtp.Timeout = 15000; 
                    smtp.Send(uyariMail);
                }
                catch { }

                return View();
            }

            // Adim 2: Google reCAPTCHA dogrulamasi
            var recaptchaResponse = Request.Form["g-recaptcha-response"];
            if (string.IsNullOrEmpty(recaptchaResponse))
            {
                TempData["Error"] = "Lütfen robot olmadığınızı doğrulayın!";
                return View();
            }

            using (var client = new HttpClient())
            {
                var secretKey = _config["RecaptchaSettings:SecretKey"]; 
                var response = await client.PostAsync($"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={recaptchaResponse}", null);
                var jsonString = await response.Content.ReadAsStringAsync();

                if (!jsonString.Contains("\"success\": true"))
                {
                    TempData["Error"] = "Bot koruması geçilemedi! Sistem sizi şüpheli olarak algıladı.";
                    return View();
                }
            }

            // Adim 3: Veri dogrulama kontrolleri
            if (string.IsNullOrEmpty(KvkkOnay) || KvkkOnay != "true")
            {
                TempData["Error"] = "Sisteme kayıt olabilmek için KVKK ve Gizlilik Sözleşmesi'ni onaylamanız gerekmektedir!";
                return View();
            }

            if (string.IsNullOrWhiteSpace(Ad) || string.IsNullOrWhiteSpace(Soyad) || 
                string.IsNullOrWhiteSpace(KullaniciAdi) || string.IsNullOrWhiteSpace(Email) || 
                string.IsNullOrWhiteSpace(Sifre))
            {
                TempData["Error"] = "Lütfen tüm alanları geçerli karakterlerle doldurun!";
                return View();
            }

            // Ad ve soyad alanlari rakam iceremez
            if (Ad.Any(char.IsDigit) || Soyad.Any(char.IsDigit))
            {
                TempData["Error"] = "Ad ve Soyad alanları rakam (sayı) içeremez!";
                return View();
            }

            // Sifre guclulugunun dogrulanmasi
            if (!GecerliSifreMi(Sifre))
            {
                TempData["Error"] = "Şifreniz en az 8 karakter olmalı; en az 1 büyük harf ve 1 rakam içermelidir.";
                return View();
            }

            // Kullanici adi benzersizlik kontrolu
            if (_context.Kullanicilar.Any(k => k.KullaniciAdi == KullaniciAdi))
            {
                TempData["Error"] = "Bu Kullanıcı Adı zaten alınmış!"; return View();
            }

            // E-posta adresi benzersizlik kontrolu
            if (_context.Kullanicilar.Any(k => k.Email == Email))
            {
                TempData["Error"] = "Bu E-Posta adresi ile daha önce kayıt olunmuş!"; return View();
            }

            // Ilk kullaniciya Admin, sonrakilere Normal Kullanici rolu ata
            var rol = _context.Kullanicilar.Any() ? KullaniciRol.NormalKullanici : KullaniciRol.Admin;
            string sifreHash = Convert.ToBase64String(Encoding.UTF8.GetBytes(Sifre));

            var yeniKullanici = new Kullanici
            {
                Ad = Ad.Trim(), 
                Soyad = Soyad.Trim(), 
                KullaniciAdi = KullaniciAdi.Trim(),
                Email = Email.Trim(), 
                SifreHash = sifreHash, 
                Rol = rol, 
                IsBanned = false,
                KvkkOnay = true,
                IsEmailConfirmed = false,
                EmailConfirmationToken = Guid.NewGuid().ToString()
            };

            _context.Kullanicilar.Add(yeniKullanici);
            _context.SaveChanges();

            // Basarili kayit sonrasi IP adresini 15 dakika sureli olarak kilitle
            var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(15));
            _cache.Set(cacheKey, DateTime.Now, cacheOptions);

            // Kullaniciya e-posta aktivasyon linki gonder
            try
            {
                string link = Url.Action("EmailOnayla", "Auth", new { token = yeniKullanici.EmailConfirmationToken }, Request.Scheme) ?? $"https://gorevlab.com.tr/Auth/EmailOnayla?token={yeniKullanici.EmailConfirmationToken}";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("info@gorevlab.com.tr", "GorevLab Sistem");
                mail.To.Add(yeniKullanici.Email);
                mail.Subject = "GorevLab - Hesap Aktivasyonu";
                mail.IsBodyHtml = true;
                
                mail.Body = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                        <h2 style='color: #4f46e5;'>GorevLab'e Hoş Geldin {yeniKullanici.Ad}!</h2>
                        <p>Hesabını aktifleştirmek için aşağıdaki butona tıklaman yeterli:</p>
                        <a href='{link}' style='padding: 10px 20px; background-color: #4f46e5; color: white; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;'>Hesabımı Onayla</a>
                        <br><br>
                        <p style='color: #64748b; font-size: 14px;'>Eğer buton çalışmıyorsa şu linke tıklayabilirsin: <br> <a href='{link}' style='color: #4f46e5;'>{link}</a></p>
                    </div>";

                SmtpClient smtp = new SmtpClient("smtp.turkticaret.net", 587);
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(_config["SmtpSettings:Email"], _config["SmtpSettings:Password"]); 
                smtp.EnableSsl = true; 
                smtp.Timeout = 20000; 

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                // E-posta gonderim hatasi sessizce gecilir; kayit islemi basarili olmustur
            }

            TempData["Success"] = "Kayıt başarılı! Lütfen e-posta adresinize gönderilen onay linkine tıklayın.";
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Giris formunun goruntulendigi sayfayi dondurur.
        /// Giris yapmis kullanicilar anasayfaya yonlendirilir.
        /// Bakim modu aktifse ve kullanici Kurucu degilse oturumu temizler.
        /// </summary>
        /// <returns>Giris formu gorunumunu dondurur.</returns>
        [HttpGet]
        public IActionResult Login() 
        { 
            // Kullanici zaten oturum acmissa yonlendir
            if (HttpContext.Session.GetInt32("KullaniciId") != null)
            {
                var rol = HttpContext.Session.GetInt32("KullaniciRol") ?? 0;
                
                // Bakim modu aktifse ve kullanici Kurucu degilse oturumu temizle
                if (GorevTakipSistemi.Models.SiteSettings.BakimModuAktif && rol != (int)KullaniciRol.Owner)
                {
                    HttpContext.Session.Clear();
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            // "Beni Hatirla" ozelligi ile saklanan kullanici adini forma doldir
            if (Request.Cookies.TryGetValue("HatirlananKullanici", out string hatirlananKullanici))
            {
                ViewBag.HatirlananKullanici = hatirlananKullanici;
            }
            return View(); 
        }

        /// <summary>
        /// Kullanici giris islemini gerceklestirir. Kimlik bilgileri, e-posta dogrulamasi ve ban durumu kontrol edilir.
        /// Sureli ban bitmisse otomatik olarak kaldirilir. Basarili giris sonrasi oturum bilgileri ayarlanir ve sistem loguna kaydedilir.
        /// </summary>
        /// <param name="KullaniciAdi">Giris yapilacak kullanici adi.</param>
        /// <param name="Sifre">Kullanicinin sifresi.</param>
        /// <param name="BeniHatirla">Kullanici adinin cerez ile hatirlanip hatirlanmayacagi.</param>
        /// <returns>Basarili giris sonrasi anasayfaya yonlendirir.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string KullaniciAdi, string Sifre, bool BeniHatirla = false)
        {
            if (string.IsNullOrWhiteSpace(KullaniciAdi) || string.IsNullOrWhiteSpace(Sifre))
            {
                TempData["Error"] = "Kullanıcı adı ve şifre gereklidir!"; return View();
            }

            // Girilen sifreyi hash'leyerek veritabanindaki deger ile karsilastir
            string girilenSifreHash = Convert.ToBase64String(Encoding.UTF8.GetBytes(Sifre));
            var kullanici = _context.Kullanicilar.FirstOrDefault(k => k.KullaniciAdi == KullaniciAdi && k.SifreHash == girilenSifreHash);

            if (kullanici == null)
            {
                TempData["Error"] = "Kullanıcı adı veya şifre hatalı!"; return View();
            }

            // E-posta dogrulama kontrolu
            if (!kullanici.IsEmailConfirmed)
            {
                TempData["Error"] = "Hesabınız henüz onaylanmamış. Lütfen e-posta adresinize gelen aktivasyon linkine tıklayın.";
                return View();
            }

            // Ban durumu kontrolu ve sureli ban otomatik kaldirma
            if (kullanici.IsBanned)
            {
                if (kullanici.BanBitisTarihi.HasValue && kullanici.BanBitisTarihi.Value <= DateTime.Now)
                {
                    // Ban suresi dolmus, otomatik olarak kaldir
                    kullanici.IsBanned = false;
                    kullanici.BanNedeni = null;
                    kullanici.BanBitisTarihi = null;
                    _context.SaveChanges();
                }
                else
                {
                    // Ban hala gecerli, kullaniciya bilgi ver
                    string banMesaji = kullanici.BanBitisTarihi.HasValue ? $"Hesabınız {kullanici.BanBitisTarihi.Value:dd.MM.yyyy HH:mm} tarihine kadar askıya alınmıştır. Neden: {kullanici.BanNedeni}" : "SİSTEME ERİŞİMİNİZ YASAKLANMIŞTIR! Hesabınız kalıcı olarak askıya alındı.";
                    
                    TempData["Error"] = banMesaji;
                    return View();
                }
            }

            // Oturum bilgilerini ayarla
            HttpContext.Session.SetInt32("KullaniciId", kullanici.Id);
            HttpContext.Session.SetString("KullaniciAdSoyad", $"{kullanici.Ad} {kullanici.Soyad}");
            HttpContext.Session.SetInt32("KullaniciRol", (int)kullanici.Rol);

            // Giris islemini sistem loguna kaydet
            _context.SistemLoglari.Add(new SistemLog {
                KullaniciAdi = $"{kullanici.Ad} {kullanici.Soyad}",
                YapilanIslem = "Sisteme giriş yapıldı",
                IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP",
                IslemTarihi = DateTime.Now
            });
            
            _context.SaveChanges();

            // "Beni Hatirla" secenegi isaretlenmisse kullanici adini cerez olarak sakla
            if (BeniHatirla)
            {
                var cookieOptions = new CookieOptions { Expires = DateTime.Now.AddDays(30), HttpOnly = true, Secure = true };
                Response.Cookies.Append("HatirlananKullanici", kullanici.KullaniciAdi, cookieOptions);
            }
            else
            {
                Response.Cookies.Delete("HatirlananKullanici");
            }

            return RedirectToAction("Index", "Home"); 
        }

        /// <summary>
        /// Kullanicinin oturumunu kapatir, tum oturum verilerini ve hatirla cerezini temizler.
        /// </summary>
        /// <returns>Giris sayfasina yonlendirir.</returns>
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("HatirlananKullanici");
            return RedirectToAction("Login");
        }

        /// <summary>
        /// E-posta aktivasyon tokeni ile kullanici hesabini dogrular ve aktif hale getirir.
        /// Token dogrulandiktan sonra tekrar kullanilamaz hale getirilir.
        /// </summary>
        /// <param name="token">E-posta ile gonderilen benzersiz aktivasyon tokeni.</param>
        /// <returns>Giris sayfasina yonlendirir.</returns>
        [HttpGet]
        public IActionResult EmailOnayla(string token)
        {
            if (string.IsNullOrEmpty(token)) return NotFound();

            var kullanici = _context.Kullanicilar.FirstOrDefault(x => x.EmailConfirmationToken == token);

            if (kullanici == null)
            {
                TempData["Error"] = "Geçersiz veya süresi dolmuş aktivasyon kodu!";
                return RedirectToAction("Login");
            }

            // Hesabi onayla ve tokeni gecersiz kil
            kullanici.IsEmailConfirmed = true;
            kullanici.EmailConfirmationToken = null; 

            _context.Update(kullanici);
            _context.SaveChanges();

            TempData["Success"] = "E-posta adresiniz başarıyla doğrulandı! Artık giriş yapabilirsiniz.";
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Belirtilen e-posta adresine ait kullanici adini hatirlatma e-postasi gonderir.
        /// </summary>
        /// <param name="email">Kullanici adinin arandigi e-posta adresi.</param>
        /// <returns>Giris sayfasina yonlendirir.</returns>
        [HttpGet]
        public IActionResult KullaniciAdiHatirlat(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Lütfen geçerli bir e-posta adresi giriniz.";
                return RedirectToAction("Login");
            }

            var kullanici = _context.Kullanicilar.FirstOrDefault(x => x.Email == email);

            if (kullanici == null)
            {
                TempData["Error"] = "Sistemde bu e-posta adresine ait bir hesap bulunamadı.";
                return RedirectToAction("Login");
            }

            // Kullanici adi bilgisini e-posta ile gonder
            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("info@gorevlab.com.tr", "GorevLab Sistem");
                mail.To.Add(email);
                mail.Subject = "GorevLab - Kullanıcı Adı Hatırlatma";
                mail.IsBodyHtml = true;
                
                mail.Body = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                        <h2 style='color: #4f46e5;'>GorevLab Kullanıcı Destek</h2>
                        <p>Merhaba <b>{kullanici.Ad}</b>,</p>
                        <p>Sistemde kayıtlı kullanıcı adınızı hatırlatmak için bu maili gönderdik.</p>
                        <p style='font-size: 18px; padding: 10px; background-color: #f8fafc; border-radius: 5px;'>Kullanıcı Adınız: <b>{kullanici.KullaniciAdi}</b></p>
                        <p>Giriş yapmak için sitemizi ziyaret edebilirsiniz.</p>
                        <hr style='border-top: 1px solid #e2e8f0;'>
                        <small style='color: #64748b;'>Bu mail otomatik olarak gönderilmiştir, lütfen cevaplamayınız.</small>
                    </div>";

                SmtpClient smtp = new SmtpClient("smtp.turkticaret.net", 587);
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(_config["SmtpSettings:Email"], _config["SmtpSettings:Password"]); 
                smtp.EnableSsl = true; 
                smtp.Timeout = 20000; 

                smtp.Send(mail);
                
                TempData["Success"] = "Kullanıcı adınız e-posta adresinize gönderildi. Lütfen Spam/Gereksiz klasörünü de kontrol ediniz.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"E-posta gönderilirken bir sorun oluştu: {ex.Message}";
            }
            
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Sifre sifirlama formunun goruntulendigi sayfayi dondurur.
        /// </summary>
        /// <returns>Sifre sifirlama formu gorunumunu dondurur.</returns>
        [HttpGet]
        public IActionResult SifremiUnuttum() { return View(); }

        /// <summary>
        /// Sifre sifirlama islemi icin 6 haneli dogrulama kodu uretir ve kullanicinin e-posta adresine gonderir.
        /// Dogrulama kodu 15 dakika gecerlidir.
        /// </summary>
        /// <param name="Email">Sifre sifirlanacak hesabin e-posta adresi.</param>
        /// <returns>Kod dogrulama sayfasina yonlendirir.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SifremiUnuttum(string Email)
        {
            var kullanici = _context.Kullanicilar.FirstOrDefault(k => k.Email == Email);
            if (kullanici == null)
            {
                TempData["Error"] = "Sistemde bu e-posta adresine ait bir hesap bulunamadı.";
                return View();
            }

            // 6 haneli rastgele dogrulama kodu uret ve veritabanina kaydet
            Random rnd = new Random();
            string dogrulamaKodu = rnd.Next(100000, 999999).ToString(); 

            kullanici.ResetToken = dogrulamaKodu;
            kullanici.ResetTokenBitisSuresi = DateTime.Now.AddMinutes(15); 
            _context.SaveChanges();

            // Dogrulama kodunu kullaniciya e-posta ile gonder
            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("info@gorevlab.com.tr", "GorevLab Güvenlik"); 
                mail.To.Add(Email);
                mail.Subject = "GorevLab - Şifre Sıfırlama Kodunuz";
                mail.IsBodyHtml = true;
                mail.Body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 500px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 10px; text-align:center;'>
                        <h2 style='color: #4f46e5;'>GorevLab Güvenlik Merkezi</h2>
                        <p>Merhaba <b>{kullanici.KullaniciAdi}</b>,</p>
                        <p>Şifre sıfırlama işleminizi tamamlamak için doğrulama kodunuz:</p>
                        <div style='background-color: #f8fafc; padding: 15px; font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #1e293b; border-radius: 8px; margin: 20px 0;'>
                            {dogrulamaKodu}
                        </div>
                        <p style='font-size: 13px; color: #ef4444;'>Bu kod 15 dakika içinde geçerliliğini yitirecektir.</p>
                        <p style='font-size: 12px; color: #94a3b8;'>Bu işlemi siz yapmadıysanız lütfen bu e-postayı dikkate almayın.</p>
                    </div>";

              SmtpClient smtp = new SmtpClient("smtp.turkticaret.net", 587);
              smtp.UseDefaultCredentials = false;
              smtp.Credentials = new NetworkCredential(_config["SmtpSettings:Email"], _config["SmtpSettings:Password"]); 
              smtp.EnableSsl = true; 
              smtp.Timeout = 20000; 
              smtp.Send(mail);

                TempData["Success"] = "6 haneli doğrulama kodu e-postanıza gönderildi.";
                return RedirectToAction("KodDogrulama", new { email = Email });
            }
            catch (Exception ex) 
            {
                TempData["Error"] = $"Güvenlik kodu gönderilemedi. Sunucu Hatası: {ex.Message}";
                return View();
            }
        }

        /// <summary>
        /// Sifre sifirlama dogrulama kodu giris formunu goruntuleme (GET).
        /// </summary>
        /// <param name="email">Dogrulama kodunun gonderildigi e-posta adresi.</param>
        /// <returns>Kod dogrulama formu gorunumunu dondurur.</returns>
        [HttpGet]
        public IActionResult KodDogrulama(string email)
        {
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");
            ViewBag.Email = email;
            return View();
        }

        /// <summary>
        /// Kullanicinin girdigi dogrulama kodunu kontrol eder. Kod dogruysa ve suresi gecmemisse
        /// sifre belirleme sayfasina yonlendirir.
        /// </summary>
        /// <param name="email">Dogrulama kodunun gonderildigi e-posta adresi.</param>
        /// <param name="dogrulamaKodu">Kullanicinin girdigi 6 haneli dogrulama kodu.</param>
        /// <returns>Basarili dogrulamada sifre sifirlama sayfasina yonlendirir.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult KodDogrulama(string email, string dogrulamaKodu)
        {
            var kullanici = _context.Kullanicilar.FirstOrDefault(k => k.Email == email && k.ResetToken == dogrulamaKodu);
            
            if (kullanici == null)
            {
                TempData["Error"] = "Hatalı doğrulama kodu girdiniz!";
                ViewBag.Email = email;
                return View();
            }

            // Dogrulama kodunun suresinin dolup dolmadigini kontrol et
            if (kullanici.ResetTokenBitisSuresi < DateTime.Now)
            {
                TempData["Error"] = "Bu kodun kullanım süresi (15 dk) dolmuş. Lütfen yeni kod isteyin.";
                return RedirectToAction("SifremiUnuttum");
            }

            TempData["Success"] = "Kod doğrulandı! Şimdi yeni şifrenizi belirleyebilirsiniz.";
            return RedirectToAction("SifreSifirla", new { email = email, kod = dogrulamaKodu });
        }

        /// <summary>
        /// Yeni sifre belirleme formunu goruntuleme (GET).
        /// </summary>
        /// <param name="email">Sifre sifirlanacak hesabin e-posta adresi.</param>
        /// <param name="kod">Dogrulanmis sifirlama kodu.</param>
        /// <returns>Sifre sifirlama formu gorunumunu dondurur.</returns>
        [HttpGet]
        public IActionResult SifreSifirla(string email, string kod)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(kod)) return RedirectToAction("Login");
            ViewBag.Email = email;
            ViewBag.Kod = kod;
            return View();
        }

        /// <summary>
        /// Kullanicinin yeni sifresini belirler ve kaydeder. Sifre eslesme, gucluluK ve
        /// token gecerlilik kontrolleri uygulanir. Basarili sifirlama sonrasi token gecersiz kilinir.
        /// </summary>
        /// <param name="email">Sifre sifirlanacak hesabin e-posta adresi.</param>
        /// <param name="kod">Dogrulanmis sifirlama kodu.</param>
        /// <param name="yeniSifre">Kullanicinin belirledigi yeni sifre.</param>
        /// <param name="yeniSifreTekrar">Yeni sifrenin tekrar girisi (eslesme kontrolu icin).</param>
        /// <returns>Basarili sifirlama sonrasi giris sayfasina yonlendirir.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SifreSifirla(string email, string kod, string yeniSifre, string yeniSifreTekrar)
        {
            // Sifre eslesme kontrolu
            if (yeniSifre != yeniSifreTekrar)
            {
                TempData["Error"] = "Şifreler uyuşmuyor!";
                ViewBag.Email = email;
                ViewBag.Kod = kod;
                return View();
            }

            // Sifre guclulugunun dogrulanmasi
            if (!GecerliSifreMi(yeniSifre))
            {
                TempData["Error"] = "Şifreniz en az 8 karakter olmalı; en az 1 büyük harf ve 1 rakam içermelidir.";
                ViewBag.Email = email;
                ViewBag.Kod = kod;
                return View();
            }

            // Token gecerliligi ve sure kontrolu
            var kullanici = _context.Kullanicilar.FirstOrDefault(k => k.Email == email && k.ResetToken == kod && k.ResetTokenBitisSuresi > DateTime.Now);
            if (kullanici == null) 
            {
                TempData["Error"] = "Güvenlik ihlali veya süresi dolmuş işlem!";
                return RedirectToAction("Login");
            }

            // Yeni sifreyi hash'leyerek kaydet ve sifirlama tokenini temizle
            kullanici.SifreHash = Convert.ToBase64String(Encoding.UTF8.GetBytes(yeniSifre));
            kullanici.ResetToken = null; 
            kullanici.ResetTokenBitisSuresi = null;

            _context.SaveChanges();

            TempData["Success"] = "Harika! Şifreniz başarıyla sıfırlandı.";
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Verilen sifrenin guvenlik kriterlerini karsilayip karsilamadigini kontrol eder.
        /// Sifre en az 8 karakter uzunlugunda olmali, en az 1 buyuk harf ve 1 rakam icermelidir.
        /// </summary>
        /// <param name="sifre">Dogrulanacak sifre metni.</param>
        /// <returns>Sifre gecerliyse true, degilse false dondurur.</returns>
        private bool GecerliSifreMi(string sifre)
        {
            if (string.IsNullOrEmpty(sifre) || sifre.Length < 8) return false;
            if (!sifre.Any(char.IsUpper)) return false;
            if (!sifre.Any(char.IsDigit)) return false;
            
            return true;
        }
    }
}