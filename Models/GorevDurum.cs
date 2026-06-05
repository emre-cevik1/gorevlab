namespace GorevTakipSistemi.Models
{
    /// <summary>
    /// Gorevlerin islem durumunu belirleyen numaralandirma tipi.
    /// Bir gorevin yasam dongusundeki olasi durumlari tanimlar.
    /// </summary>
    public enum GorevDurum
    {
        /// <summary>
        /// Gorev henuz baslanmamis ve islem bekliyor.
        /// </summary>
        Bekliyor = 0,

        /// <summary>
        /// Gorev uzerinde aktif olarak calisiliyor.
        /// </summary>
        DevamEdiyor = 1,

        /// <summary>
        /// Gorev basariyla tamamlanmis.
        /// </summary>
        Tamamlandi = 2
    }
}