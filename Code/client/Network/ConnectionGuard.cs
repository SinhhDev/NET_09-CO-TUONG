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

        public async Task ConnectAsync(string serverIp, int port)
        {
            StateChanged?.Invoke($"Đang kết nối đến {serverIp}:{port}...");
            await Listener.ConnectAsync(serverIp, port);
            StateChanged?.Invoke("Kết nối thành công!");
        }

        public void Dispose()
        {
            Listener.Dispose();
        }
    }
}
