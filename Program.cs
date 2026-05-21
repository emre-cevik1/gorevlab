using Microsoft.EntityFrameworkCore;
using GorevTakipSistemi.Data;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMemoryCache(); // 🧠 RAM tabanlı IP takibi için şart!
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 52428800; // 50MB'a kadar izin veriyoruz
    serverOptions.AddServerHeader = false;
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Add services to the container.
builder.Services.AddDataProtection();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new GorevTakipSistemi.Filters.BakimModuFilter());
});
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Oturum 30 dakika hareketsiz kalırsa kapanır
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// 🔥 ZAP GÜVENLİK KALKANLARI (HTTP HEADERS)
app.Use(async (context, next) =>
{
    // 1. Clickjacking Koruması (Siteni iframe içine almalarını engeller)
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    
    // 2. MIME-Sniffing Koruması (İçerik türü manipülasyonunu engeller)
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    
    // 3. Tarayıcı XSS Korumasını Zorla
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

    // 4. Content Security Policy (CSP) - Sadece güvenilir kaynaklardan kod çalışmasına izin ver
    // Not: Tailwind, SweetAlert, jQuery ve Google reCAPTCHA kullandığımız için onlara izin verdik.
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.tailwindcss.com https://cdn.jsdelivr.net https://www.google.com https://www.gstatic.com https://code.jquery.com; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net; font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; frame-src 'self' https://www.google.com; img-src 'self' data: https:;");

    // 5. ASP.NET Bilgi Sızıntısını Gizle
    context.Response.Headers.Remove("X-Powered-By");
    context.Response.Headers.Remove("Server");

    await next();
});

app.UseRouting();
// ... (geri kalan kodlar)
app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
