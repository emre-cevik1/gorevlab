using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace GorevTakipSistemi.Hubs
{
    /// <summary>
    /// SignalR uzerinden gercek zamanli bildirim yonetimini saglayan hub sinifi.
    /// Kullanicilara ozel bildirim gruplari olusturarak hedefli mesaj iletimi yapar.
    /// </summary>
    public class BildirimHub : Hub
    {
        /// <summary>
        /// Kullaniciyi kendi benzersiz kimligine ait SignalR grubuna ekler.
        /// Bu sayede ilgili kullaniciya ozel bildirimler gonderilebilir.
        /// </summary>
        /// <param name="kullaniciId">Gruba eklenecek kullanicinin benzersiz kimligi.</param>
        public async Task KullaniciBaglandi(string kullaniciId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, kullaniciId);
        }

        /// <summary>
        /// Kullaniciyi ait oldugu SignalR grubundan cikarir.
        /// Kullanici baglantisi sonlandiginda cagirilir.
        /// </summary>
        /// <param name="kullaniciId">Gruptan cikarilacak kullanicinin benzersiz kimligi.</param>
        public async Task KullaniciAyrildi(string kullaniciId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, kullaniciId);
        }
    }
}
