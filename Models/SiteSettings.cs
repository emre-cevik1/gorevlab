namespace GorevTakipSistemi.Models
{
    /// <summary>
    /// Uygulamanin genel site yapilandirma ayarlarini tutan statik sinif.
    /// Bakim modu gibi sistem genelinde gecerli olan ayarlari yonetir.
    /// </summary>
    public static class SiteSettings
    {
        /// <summary>
        /// Bakim modunun aktif olup olmadigini belirtir. 
        /// Aktif oldugunda yalnizca Owner rolundeki kullanicilar sisteme erisebilir. 
        /// Varsayilan deger: pasif.
        /// </summary>
        public static bool BakimModuAktif { get; set; } = false;
    }
}
