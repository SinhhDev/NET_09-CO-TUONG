using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoTuongOnline.Client
{
    public class ConnectionGuard : IDisposable
    {
        private readonly Form _owner;
        public ClientEventListener Listener { get; }
        public event Action<string>? StateChanged;

        public ConnectionGuard(Form owner)
        {
            _owner = owner;
            Listener = new ClientEventListener();
        }

        public async Task ConnectAsync(string serverIp, int port, string? username = null)
        {
            StateChanged?.Invoke($"Đang kết nối đến {serverIp}:{port}...");
            await Listener.ConnectAsync(serverIp, port);
            StateChanged?.Invoke("Kết nối mạng thành công!");

            if (!string.IsNullOrWhiteSpace(username))
            {
                StateChanged?.Invoke($"Đang đăng nhập với tên '{username}'...");
                await Listener.SendLoginAsync(username);
            }
        }

        public void Dispose()
        {
            Listener.Dispose();
        }
    }
}
