<div align="center">
  <h1>GorevLab.</h1>
  <p>Modern, Güvenli ve Tam Otomatik Görev Yönetim Sistemi</p>
</div>

---

## 🚀 Proje Hakkında
**GorevLab**, ekiplerin ve bireylerin görevlerini modern ve kullanıcı dostu bir arayüzle yönetebilmeleri için geliştirilmiş, yüksek güvenlikli ve yüksek performanslı bir ASP.NET Core 8.0 MVC projesidir. 

Sistem sadece bir görev takibi değil, aynı zamanda detaylı rol yönetimi, ekip kurma, CI/CD otomasyonu ve Google/Apple standartlarında güvenlik altyapısına sahip tam teşekküllü bir SaaS (Software as a Service) mimarisi sunar.

## ✨ Öne Çıkan Özellikler

*   **👥 Gelişmiş Rol Sistemi:** Kurucu (Owner), Yönetici (Admin), Kullanıcı (User) ve Ziyaretçi (Guest) olmak üzere 4 katmanlı detaylı yetkilendirme.
*   **🤝 Ekip & Davet Yönetimi:** Kullanıcıların kendi ekiplerini kurabilmesi, diğer kullanıcılara sistem içi davet gönderebilmesi ve görevleri ekip üyelerine atayabilmesi.
*   **🛡️ Askeri Düzeyde Güvenlik:** 
    *   IP Tabanlı Spam Koruması (Rate Limiting).
    *   Google reCAPTCHA v3 Entegrasyonu.
    *   ASP.NET Data Protection ile Şifrelenmiş Çerezler.
    *   XSS, CSRF, Clickjacking ve MIME-Sniffing HTTP başlık korumaları.
*   **⚡ Hızlı Giriş (Kalıcı Oturum):** Tarayıcılardaki "Beni Hatırla" butonunun Google stili modern implementasyonu. Kullanıcı döndüğünde şifre girmek yerine "Emre Olarak Devam Et" kartı ile tek tıkla şifreli token üzerinden hızlı giriş yapabilir.
*   **✉️ SMTP E-Posta Onayı:** Kayıt olan kullanıcıların gerçekliğini doğrulamak için otomatik e-posta aktivasyon sistemi.
*   **🛠️ Yazılımsal Bakım Modu (Soft Maintenance):** Kurucu (Owner), siteyi yayından kaldırmadan sistem üzerinden tek tıkla "Bakım Modu"na alabilir. Ziyaretçiler şık bir uyarı sayfası görürken, kurucu siteyi aktif olarak kullanıp test edebilir.
*   **📊 Kapsamlı Log Sistemi:** Sistemdeki tüm hareketlerin, hataların ve girişlerin kaydedildiği Admin log yönetim paneli.
*   **🎫 Destek Talebi Modülü:** Kullanıcıların yöneticilerle doğrudan iletişim kurabileceği entegre ticket sistemi.

## 💻 Kullanılan Teknolojiler

**Backend:**
*   C# & ASP.NET Core 8.0 MVC
*   Entity Framework Core (Code First)
*   Microsoft SQL Server

**Frontend:**
*   HTML5, CSS3, JavaScript
*   [Tailwind CSS](https://tailwindcss.com/) (Özel Okyanus Konseptli Tasarım)
*   [SweetAlert2](https://sweetalert2.github.io/) (Dinamik Popup ve Uyarılar)
*   Bootstrap Icons & FontAwesome

**DevOps & Deployment:**
*   GitHub Actions (CI/CD Sürekli Entegrasyon)
*   Plesk FTP Otomatik Deploy
*   Git Versiyon Kontrolü

## ⚙️ CI/CD Otomasyonu (DevOps)
Bu proje, GitHub Actions kullanılarak tam otomatik yayınlama sistemine (CI/CD) sahiptir. 
Geliştirici kodu `main` dalına (branch) gönderdiği (push) anda:
1. GitHub sunucularında `.NET 8` ortamı kurulur.
2. Proje derlenir (`dotnet publish`).
3. GitHub Secrets içinde saklanan gizli şifreler (Veritabanı, SMTP, Recaptcha) `appsettings.json` dosyasına güvenle enjekte edilir.
4. Çıktı dosyaları FTP aracılığıyla canlı sunucuya (Türkticaret/Plesk) otomatik olarak aktarılır.
*(Böylece geliştiricinin FTP programı kullanmasına gerek kalmaz, kodlama ve yayınlama süreci tamamen ayrılır).*

---
<div align="center">
  <i>Bu proje <b>Emre Çevik</b> tarafından hayata geçirilmiştir. © 2026 GorevLab.</i>
</div>
