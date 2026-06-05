using Microsoft.EntityFrameworkCore;
using GorevTakipSistemi.Data;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMemoryCache(); // Bellek tabanli onbellekleme servisi (IP takibi ve hiz sinirlamasi icin kullanilir)
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 52428800; // Maksimum istek govde boyutunu 50 MB olarak sinirlandirir
    serverOptions.AddServerHeader = false;
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// MVC denetleyici ve gorunum servislerini global filtrelerle birlikte yapilandirir
builder.Services.AddControllersWithViews(options => 
{
    options.Filters.Add<GorevTakipSistemi.Filters.BakimModuFilter>();
    options.Filters.Add<GorevTakipSistemi.Filters.IslemLogFilter>(); // Tum POST islemlerini otomatik olarak loglar
});
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Oturum zaman asimi suresi: 30 dakika hareketsizlik sonrasi sona erer
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});


// SignalR gercek zamanli iletisim servisini kaydeder
builder.Services.AddSignalR();

var app = builder.Build();

// Uygulama baslatildiginda bekleyen veritabani migration islemlerini otomatik olarak uygular
using (var scope = app.Services.CreateScope())
{
    try 
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Veritabanı migration hatası: " + ex.Message);
    }
}

// HTTP istek hattini ortam turune gore yapilandirir
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // HSTS varsayilan suresi 30 gundur. Uretim ortami icin bu degerin ayarlanmasi onerilir.
    app.UseHsts();
}

app.UseHttpsRedirection();

// HTTP guvenlik basliklarini tum yanıtlara ekleyen ara katman yazilimi
app.Use(async (context, next) =>
{
    // Clickjacking saldirilarina karsi koruma saglar, sayfanin yalnizca ayni kaynaktan iframe icinde gosterilmesine izin verir
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    
    // MIME tur koklamasini devre disi birakarak icerik turu manipulasyonunu onler
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    
    // Tarayici tarafindaki XSS filtreleme mekanizmasini etkinlestirir
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

    // Icerik guvenlik politikasi tanimlari: yalnizca guvenilir kaynaklardan icerik yuklemesine izin verir
    // Tailwind CSS, SweetAlert, jQuery ve Google reCAPTCHA gibi harici bagimliliklara ozel izinler tanimlanmistir
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.tailwindcss.com https://cdn.jsdelivr.net https://www.google.com https://www.gstatic.com https://code.jquery.com; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net; font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; frame-src 'self' https://www.google.com; img-src 'self' data: https:;");

    // Sunucu teknolojisi bilgi sizintisini onlemek icin ilgili HTTP basliklarini kaldirir
    context.Response.Headers.Remove("X-Powered-By");
    context.Response.Headers.Remove("Server");

    await next();
});

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<GorevTakipSistemi.Hubs.BildirimHub>("/bildirimHub");

app.Run();
