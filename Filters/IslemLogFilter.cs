using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using GorevTakipSistemi.Data;
using GorevTakipSistemi.Models;

namespace GorevTakipSistemi.Filters
{
    /// <summary>
    /// Basarili POST islemlerini otomatik olarak sistem loguna kaydeden aksiyon filtresi.
    /// Denetleyici ve aksiyon bilgileriyle birlikte kullanici adi ve IP adresi gibi detaylari loglar.
    /// </summary>
    public class IslemLogFilter : IActionFilter
    {
        /// <summary>
        /// Veritabani baglam nesnesi. Log kayitlarinin veritabanina yazilmasi icin kullanilir.
        /// </summary>
        private readonly AppDbContext _context;

        /// <summary>
        /// IslemLogFilter sinifinin yapici metodu.
        /// </summary>
        /// <param name="context">Bagimlilik enjeksiyonu ile saglanan veritabani baglam nesnesi.</param>
        public IslemLogFilter(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Aksiyon calistirilmadan once tetiklenir. Bu filtrede islem oncesi ek bir mantik uygulanmaz.
        /// </summary>
        /// <param name="context">Aksiyon calistirma baglami.</param>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Islem oncesi ek bir islem yapilmamaktadir
        }

        /// <summary>
        /// Aksiyon calistirildiktan sonra tetiklenir.
        /// Basarili POST isteklerini kullanici bilgileriyle birlikte veritabanina loglar.
        /// </summary>
        /// <param name="context">Aksiyon calistirma sonuc baglami.</param>
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Yalnizca veri degisikligi yapan POST istekleri loglanir
            var requestMethod = context.HttpContext.Request.Method;
            
            // Istek basarili ve POST metodu ise log kaydini olusturur
            if (requestMethod == "POST" && context.Exception == null)
            {
                var controllerName = context.RouteData.Values["controller"]?.ToString();
                var actionName = context.RouteData.Values["action"]?.ToString();
                
                string kullaniciAdi = context.HttpContext.Session.GetString("KullaniciAdSoyad") ?? "Misafir / Bilinmeyen";
                string islemAdeti = $"{controllerName} modülünde {actionName} işlemi yapıldı.";

                var log = new SistemLog
                {
                    KullaniciAdi = kullaniciAdi,
                    YapilanIslem = islemAdeti,
                    IpAdresi = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen IP",
                    IslemTarihi = DateTime.Now
                };

                _context.SistemLoglari.Add(log);
                _context.SaveChanges();
            }
        }
    }
}
