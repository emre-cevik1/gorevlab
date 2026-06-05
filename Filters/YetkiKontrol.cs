using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using GorevTakipSistemi.Models;

namespace GorevTakipSistemi.Filters
{
    /// <summary>
    /// Oturum acmamis kullanicilarin korunakli sayfalara erisimini engelleyen yetkilendirme filtresi.
    /// Oturum bilgisi bulunmayan kullanicilar giris sayfasina yonlendirilir.
    /// </summary>
    public class YetkiKontrol : ActionFilterAttribute
    {
        /// <summary>
        /// Aksiyon calistirilmadan once oturum durumunu kontrol eder.
        /// Oturumda kullanici kimligi bulunamazsa giris sayfasina yonlendirir.
        /// </summary>
        /// <param name="context">Aksiyon calistirma baglami.</param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var kullaniciId = context.HttpContext.Session.GetInt32("KullaniciId");
            if (kullaniciId == null)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
            }
            base.OnActionExecuting(context);
        }
    }

    /// <summary>
    /// Yalnizca Admin rolune sahip kullanicilarin erisebilecegi sayfalari koruyan yetkilendirme filtresi.
    /// Admin disindaki kullanicilar ana sayfaya yonlendirilir.
    /// </summary>
    public class AdminYetki : ActionFilterAttribute
    {
        /// <summary>
        /// Aksiyon calistirilmadan once kullanicinin Admin rolune sahip olup olmadigini kontrol eder.
        /// Rol bilgisi eksikse veya Admin degilse ana sayfaya yonlendirir.
        /// </summary>
        /// <param name="context">Aksiyon calistirma baglami.</param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var rol = context.HttpContext.Session.GetInt32("KullaniciRol");
            
            // Rol bilgisi mevcut degilse veya Admin rolune esit degilse erisim engellenir
            if (rol == null || rol != (int)KullaniciRol.Admin)
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
            base.OnActionExecuting(context);
        }
    }
}