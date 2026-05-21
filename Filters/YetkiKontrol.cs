using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using GorevTakipSistemi.Models;

namespace GorevTakipSistemi.Filters
{
    // 1. NORMAL KULLANICI FİLTRESİ (Giriş yapmayanları engeller)
    public class YetkiKontrol : ActionFilterAttribute
    {
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

    // 2. ADMİN FİLTRESİ (Sadece "2" numaralı role sahip olanları geçirir)
    public class AdminYetki : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var rol = context.HttpContext.Session.GetInt32("KullaniciRol");
            
            // Eğer rol boşsa veya Admin (2) değilse, ana sayfaya geri yolla
            if (rol == null || rol != (int)KullaniciRol.Admin)
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
            base.OnActionExecuting(context);
        }
    }
}