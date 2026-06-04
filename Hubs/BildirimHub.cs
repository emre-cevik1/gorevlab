using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace GorevTakipSistemi.Hubs
{
    public class BildirimHub : Hub
    {
        // Kullanıcılar kendi ID'leri ile bir gruba katılabilir
        // Böylece sadece belirli bir kullanıcıya bildirim gönderebiliriz.
        public async Task KullaniciBaglandi(string kullaniciId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, kullaniciId);
        }

        public async Task KullaniciAyrildi(string kullaniciId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, kullaniciId);
        }
    }
}
