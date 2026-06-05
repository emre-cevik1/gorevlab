using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using GorevTakipSistemi.Models;

namespace GorevTakipSistemi.Filters
{
    /// <summary>
    /// Bakim modu etkinken yetkisiz kullanicilarin sisteme erisimini engelleyen aksiyon filtresi.
    /// Yalnizca Owner rolundeki kullanicilar ve kimlik dogrulama sayfalari erisime acik kalir.
    /// </summary>
    public class BakimModuFilter : ActionFilterAttribute
    {
        /// <summary>
        /// Her aksiyon calistirilmadan once bakim modu durumunu kontrol eder.
        /// Bakim modu aktifse, yetkisiz kullanicilari bakim sayfasina yonlendirir.
        /// </summary>
        /// <param name="context">Aksiyon calistirma baglami.</param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (SiteSettings.BakimModuAktif)
            {
                var path = context.HttpContext.Request.Path.Value?.ToLower() ?? "";
                
                // Kimlik dogrulama ve bakim sayfasi istekleri filtreleme disinda tutulur
                if (path.StartsWith("/auth") || path.StartsWith("/home/bakim"))
                {
                    base.OnActionExecuting(context);
                    return;
                }

                // Oturumdan kullanicinin rol bilgisini alir
                var rol = context.HttpContext.Session.GetInt32("KullaniciRol");

                // Giris yapmamis veya Owner rolune sahip olmayan kullanicilar bakim sayfasina yonlendirilir
                if (rol == null || rol != (int)KullaniciRol.Owner)
                {
                    context.Result = new RedirectToActionResult("Bakim", "Home", null);
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
