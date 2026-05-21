using Microsoft.AspNetCore.Mvc;
using GorevTakipSistemi.Data;
using GorevTakipSistemi.Models;
using GorevTakipSistemi.Filters;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace GorevTakipSistemi.Controllers
{
    [YetkiKontrol]
    public class IletisimController : Controller
    {
        private readonly AppDbContext _context;

        public IletisimController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

       [HttpPost]
public async Task<IActionResult> Gonder(string konu, string mesaj)
{
    int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
    if (kullaniciId == 0) return RedirectToAction("Login", "Auth");

    var yeniMesaj = new DestekMesaji
    {
        KullaniciId = kullaniciId,
        Konu = konu,
        Mesaj = mesaj,
        Tarih = DateTime.Now,
        IsCevaplandi = false
    };

    _context.DestekMesajlari.Add(yeniMesaj);
    
    // İŞTE BURASI: Yorum satırlarını sildik ve log atmayı açtık!
    _context.SistemLoglari.Add(new SistemLog {
        KullaniciAdi = HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Bilinmeyen Kullanıcı",
        YapilanIslem = "Yeni destek talebi açıldı",
        IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP",
        IslemTarihi = DateTime.Now
    });

    await _context.SaveChangesAsync(); // KAYDETME İŞLEMİ (Log burada veritabanına yazılır)

    TempData["Success"] = "Destek talebiniz alındı! En kısa sürede e-posta adresiniz üzerinden dönüş yapılacaktır.";
    return RedirectToAction("Index", "Home");
}
        }
    }
