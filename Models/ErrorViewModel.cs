namespace GorevTakipSistemi.Models;

/// <summary>
/// Hata sayfasinda goruntulenen bilgileri tasiyan gorunum modeli.
/// Hata takibi icin istek kimligini ve goruntuleme durumunu icerir.
/// </summary>
public class ErrorViewModel
{
    /// <summary>
    /// Hatanin olustugu istegin benzersiz takip kimligi. Hata ayiklama amaciyla kullanilir.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Istek kimliginin kullaniciya gosterilip gosterilmeyecegini belirler. 
    /// RequestId degeri bos veya null degilse true doner.
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
