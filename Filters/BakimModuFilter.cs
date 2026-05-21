using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using GorevTakipSistemi.Models;

namespace GorevTakipSistemi.Filters
{
    public class BakimModuFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (SiteSettings.BakimModuAktif)
            {
                var path = context.HttpContext.Request.Path.Value?.ToLower() ?? "";
                
                // Auth (Login, Register vb.) ve Bakım sayfasına erişime izin ver
                if (path.StartsWith("/auth") || path.StartsWith("/home/bakim"))
                {
                    base.OnActionExecuting(context);
                    return;
                }

                // Kullanıcının rolünü al
                var rol = context.HttpContext.Session.GetInt32("KullaniciRol");

                // Eğer giriş yapmamışsa veya Kurucu (Owner) değilse, Bakım sayfasına at
                if (rol == null || rol != (int)KullaniciRol.Owner)
                {
                    context.Result = new RedirectToActionResult("Bakim", "Home", null);
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
