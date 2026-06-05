using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using GorevTakipSistemi.Data;
using GorevTakipSistemi.Models;

namespace GorevTakipSistemi.Filters
{
    public class IslemLogFilter : IActionFilter
    {
        private readonly AppDbContext _context;

        public IslemLogFilter(AppDbContext context)
        {
            _context = context;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // İşlem öncesi çalışır
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Sadece POST işlemleri genelde sistemde değişiklik yapar (Kayıt, Silme, Güncelleme)
            var requestMethod = context.HttpContext.Request.Method;
            
            // Eğer sayfa başarıyla çalıştıysa ve POST işlemiyse kaydet
            if (requestMethod == "POST" && context.Exception == null)
            {
                var controllerName = context.RouteData.Values["controller"]?.ToString();
                var actionName = context.RouteData.Values["action"]?.ToString();
                
                // Önceden manuel loglanan bazı kritik şeyleri çiftlememek için filtreleyebiliriz
                // Veya her şeyi loglasın gitsin.
                
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
