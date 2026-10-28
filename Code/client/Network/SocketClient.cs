using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CoTuongOnline.Client
{
    /// <summary>
    /// Lớp kết nối mạng tầng Socket của Client.
    /// Quản lý kết nối TCP, vòng nhận dữ liệu và gửi dữ liệu thread-safe.
    /// </summary>
    public sealed class SocketClient : IDisposable
    {
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private CancellationTokenSource? _receiveCts;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private readonly object _gate = new object();
        private bool _disposed;

        /// <summary>
        /// Được gọi mỗi khi có chunk dữ liệu mới từ Server.
        /// </summary>
        public event Action<byte[]>? DataReceived;
        public event Action<Exception>? ConnectionError;

        /// <summary>
        /// Kết nối tới máy chủ TCP.
        /// </summary>
        public async Task ConnectAsync(string serverIp, int serverPort, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(serverIp))
                throw new ArgumentException("Địa chỉ IP không được để trống.", nameof(serverIp));
            if (serverPort <= 0 || serverPort > 65535)
                throw new ArgumentOutOfRangeException(nameof(serverPort), "Cổng mạng không hợp lệ.");

            ThrowIfDisposed();
            ct.ThrowIfCancellationRequested();

            lock (_gate)
            {
                // Đóng kết nối cũ nếu có trước khi mở kết nối mới
                try { _stream?.Dispose(); } catch { }
                try { _tcpClient?.Close(); } catch { }

                _tcpClient = new TcpClient();
            }

            var connectTask = _tcpClient.ConnectAsync(serverIp, serverPort);
            if (ct != default)
            {
                var completed = await Task.WhenAny(connectTask, Task.Delay(Timeout.Infinite, ct)).ConfigureAwait(false);
                if (!ReferenceEquals(completed, connectTask))
                    throw new OperationCanceledException(ct);
            }

            await connectTask.ConfigureAwait(false);
            _stream = _tcpClient.GetStream();
        }

        /// <summary>
        /// Bắt đầu vòng lặp đọc dữ liệu từ server.
        /// </summary>
        public void StartReceiveLoop(CancellationToken ct = default)
        {
            ThrowIfDisposed();
            if (_stream == null)
                throw new InvalidOperationException("Chưa kết nối máy chủ. Hãy gọi ConnectAsync trước.");

            {
                if (_receiveCts != null)
                    throw new InvalidOperationException("Receive loop đã đang chạy.");

                _receiveCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            }

            _ = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token));
        }

        /// <summary>
        /// Gửi dữ liệu thô tới máy chủ, đảm bảo thread-safe với SemaphoreSlim.
        /// </summary>
        public async Task SendAsync(byte[] payload, CancellationToken ct = default)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            ThrowIfDisposed();

            NetworkStream? stream;
            lock (_gate)
            {
                stream = _stream;
            }

            if (stream == null)
                throw new InvalidOperationException("Chưa kết nối máy chủ.");

            ct.ThrowIfCancellationRequested();

            await _sendLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await stream.WriteAsync(payload, ct).ConfigureAwait(false);
                await stream.FlushAsync(ct).ConfigureAwait(false);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            NetworkStream? stream;
            lock (_gate)
            {
                stream = _stream;
            }

            if (stream == null) return;

            var buffer = new byte[8192];
            while (!ct.IsCancellationRequested && !_disposed)
            {
                int read;
                try
                {
                    read = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!_disposed && !ct.IsCancellationRequested)
                    {
                        ConnectionError?.Invoke(ex);
                    }
                    break;
                }

                if (read <= 0)
                {
                    if (!_disposed && !ct.IsCancellationRequested)
                    {
                        ConnectionError?.Invoke(new IOException("Máy chủ đã đóng kết nối."));
                    }
                    break;
                }

                var chunk = new byte[read];
                Buffer.BlockCopy(buffer, 0, chunk, 0, read);

                try
                {
                    DataReceived?.Invoke(chunk);
                }
                catch
                {
                    // Tránh exception trong event listener làm sập vòng lặp đọc
                }
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

                try { _tcpClient?.Close(); } catch { }
                _tcpClient = null;
            }

            _sendLock.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SocketClient));
        }
    }
}
