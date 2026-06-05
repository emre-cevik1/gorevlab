namespace GorevTakipSistemi.Models
{
    /// <summary>
    /// Sistemdeki kullanici rollerini tanimlayan numaralandirma tipi.
    /// Her rol farkli yetki seviyelerine sahiptir.
    /// </summary>
    public enum KullaniciRol
    {
        /// <summary>
        /// Standart kullanici rolü. Temel gorev yonetimi islemlerini gerceklestirebilir.
        /// </summary>
        NormalKullanici = 1,

        /// <summary>
        /// Yonetici rolü. Kullanici yonetimi ve sistem yapilandirmasi yetkilerine sahiptir.
        /// </summary>
        Admin = 2,

        /// <summary>
        /// Sistem sahibi rolü. Tum yetkilere sahip en ust duzey yetkilendirme seviyesidir.
        /// </summary>
        Owner = 3
    }
}