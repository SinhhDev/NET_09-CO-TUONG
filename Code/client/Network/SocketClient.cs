using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CoTuongOnline.Client
{
    /// <summary>
    /// Client networking layer (bàn giao cho phần "Client Network").
    /// Trách nhiệm chính:
    /// - Kết nối tới server bằng TcpClient.
    /// - Nhận dữ liệu dạng byte[] theo luồng.
    /// - Cung cấp event để phần UI / game logic xử lý dữ liệu (ví dụ test "bit"/"voice").
    /// </summary>
    public sealed class SocketClient : IDisposable
    {
        private readonly TcpClient _tcpClient = new TcpClient();
        private NetworkStreamWrapper? _stream;
        private CancellationTokenSource? _receiveCts;
        private readonly object _gate = new object();
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private bool _disposed;
        private bool _disposed;

        /// <summary>
        /// Được gọi mỗi khi có dữ liệu mới nhận được từ server.
        /// Lưu ý: byte[] này là "chunk" theo mức độ đọc từ NetworkStream,
        /// không đảm bảo ranh giới theo message nếu chưa có framing/protocol.
        /// </summary>
        public event Action<byte[]>? DataReceived;

        /// <summary>
        /// Kết nối tới server.
        /// </summary>
        public async Task ConnectAsync(string serverIp, int serverPort, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(serverIp))
                throw new ArgumentException("serverIp is required.", nameof(serverIp));
            if (serverPort <= 0 || serverPort > 65535)
                throw new ArgumentOutOfRangeException(nameof(serverPort));

            ThrowIfDisposed();

            ct.ThrowIfCancellationRequested();

            // TcpClient.ConnectAsync(string,int) không có overload cancellation token ở một số phiên bản.
            // Kết nối TCP tới server. Overload có CancellationToken (.NET 5+) hủy kết nối thật sự khi ct bị hủy,
            // khác với cách cũ WhenAny + Task.Delay chỉ bỏ chờ còn kết nối vẫn chạy ngầm.
            await _tcpClient.ConnectAsync(serverIp, serverPort, ct).ConfigureAwait(false);
            _tcpClient.NoDelay = true; // giảm trễ cho game
            _stream = new NetworkStreamWrapper(_tcpClient.GetStream());
        }

        /// <summary>
        /// Bắt đầu vòng lắng nghe dữ liệu từ server.
        /// </summary>
        public void StartReceiveLoop(CancellationToken ct = default)
        {
            ThrowIfDisposed();
            if (_stream == null)
                throw new InvalidOperationException("Not connected. Call ConnectAsync first.");

            lock (_gate)
            {
                if (_receiveCts != null)
                    throw new InvalidOperationException("Receive loop already started.");

                _receiveCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            }

            _ = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token));
        }

        /// <summary>
        /// Gửi dữ liệu thô tới server.
        /// (Protocol framing nếu cần thì phía game logic sẽ tự đóng gói.)
        /// </summary>
      public async Task SendAsync(byte[] payload, CancellationToken ct = default)
{
    if (payload == null) throw new ArgumentNullException(nameof(payload));
    ThrowIfDisposed();
    if (_stream == null)
        throw new InvalidOperationException("Not connected. Call ConnectAsync first.");

    await _sendLock.WaitAsync(ct).ConfigureAwait(false);
    try
    {
        ThrowIfDisposed();
        if (_stream == null)
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");

        await _stream.WriteAsync(payload, ct).ConfigureAwait(false);
    }
    finally
    {
        _sendLock.Release();
    }
}
        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            if (_stream == null) return;

            var buffer = new byte[8192];
            while (!ct.IsCancellationRequested)
            {
                int read;
                try
                {
                    read = await _stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Có thể do server đóng kết nối hoặc lỗi mạng.
                    break;
                }

                if (read <= 0) break;

                var chunk = new byte[read];
                Buffer.BlockCopy(buffer, 0, chunk, 0, read);
                try
                {
                    DataReceived?.Invoke(chunk);
                }
                catch
                {
                    // Callback lỗi không được làm sập vòng lắng nghe.
                }

                // TODO (phối hợp test): nếu server có framing/protocol riêng
                // để tách "bit" và "voice" thành message thì client logic ở nơi gọi event
                // sẽ parse chunk theo đúng protocol đó.
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            lock (_gate)
            {
                if (_receiveCts != null)
                {
                    try { _receiveCts.Cancel(); } catch { }
                    _receiveCts.Dispose();
                    _receiveCts = null;
                }

                try { _stream?.Dispose(); } catch { }
                _stream = null;

                try { _tcpClient.Close(); } catch { }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SocketClient));
        }

        /// <summary>
        /// Wrapper để không phải đưa using NetworkStream vào file (giữ gọn và dễ thay đổi).
        /// </summary>
        private sealed class NetworkStreamWrapper : IDisposable
        {
            private readonly System.IO.Stream _stream;

            public NetworkStreamWrapper(System.IO.Stream stream)
            {
                _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            }

            public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
                => _stream.ReadAsync(buffer, offset, count, ct);

            public Task WriteAsync(byte[] buffer, CancellationToken ct)
                => _stream.WriteAsync(buffer, 0, buffer.Length, ct);

            public void Dispose()
            {
                try { _stream.Dispose(); } catch { }
            }
        }
    }
}

