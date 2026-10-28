using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoTuongOnline.Client
{
    /// <summary>
    /// Giám sát kết nối tới server và tự động reconnect khi mất kết nối.
    /// Bọc ngoài ClientEventListener, cung cấp trạng thái kết nối cho UI.
    /// </summary>
    public sealed class ConnectionGuard : IDisposable
    {
        private readonly Control _uiControl;
        private string _serverIp;
        private int _serverPort;

        private SocketClient? _socket;
        private ClientEventListener? _listener;
        private CancellationTokenSource? _cts;

        private bool _disposed;
        private bool _reconnecting;
        private int _attempt;

        // ===== CẤU HÌNH =====

        /// <summary>Số lần thử reconnect tối đa (0 = vô hạn).</summary>
        public int MaxRetries { get; set; } = 5;

        /// <summary>Thời gian chờ ban đầu giữa các lần thử (ms).</summary>
        public int BaseDelayMs { get; set; } = 1000;

        /// <summary>Thời gian chờ tối đa giữa các lần thử (ms).</summary>
        public int MaxDelayMs { get; set; } = 15000;

        // ===== EVENTS =====

        /// <summary>Trạng thái kết nối thay đổi.</summary>
        public event Action<ConnectionState>? StateChanged;

        /// <summary>Đang thử reconnect. (lần thử, tổng số lần tối đa).</summary>
        public event Action<int, int>? Reconnecting;

        /// <summary>Reconnect thất bại sau tất cả các lần thử.</summary>
        public event Action? ReconnectFailed;

        /// <summary>Truy cập ClientEventListener hiện tại để đăng ký event game.</summary>
        public ClientEventListener? Listener => _listener;

        /// <summary>Trạng thái hiện tại.</summary>
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

        public ConnectionGuard(Control uiControl)
        {
            _uiControl = uiControl ?? throw new ArgumentNullException(nameof(uiControl));
            _serverIp = "";
            _serverPort = 0;
        }

        /// <summary>
        /// Kết nối lần đầu tới server.
        /// </summary>
        public async Task ConnectAsync(string serverIp, int serverPort, CancellationToken ct = default)
        {
            ThrowIfDisposed();

            _serverIp = serverIp;
            _serverPort = serverPort;
            _attempt = 0;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            await CreateAndConnectAsync(_cts.Token);
        }

        /// <summary>
        /// Ngắt kết nối và dừng auto-reconnect.
        /// </summary>
        public void Disconnect()
        {
            _cts?.Cancel();
            CleanupCurrent();
            SetState(ConnectionState.Disconnected);
        }

        /// <summary>
        /// Gửi packet qua socket hiện tại. Ném exception nếu chưa kết nối.
        /// </summary>
        public async Task SendAsync(byte[] payload, CancellationToken ct = default)
        {
            if (_socket == null || State != ConnectionState.Connected)
                throw new InvalidOperationException("Not connected.");

            await _socket.SendAsync(payload, ct);
        }

        // ===== INTERNAL =====

        private async Task CreateAndConnectAsync(CancellationToken ct)
        {
            CleanupCurrent();
            SetState(ConnectionState.Connecting);

            _socket = new SocketClient();
            _listener = new ClientEventListener(_socket, _uiControl);

            // Lắng nghe lỗi kết nối để trigger reconnect
            _listener.OnConnectionError += OnConnectionLost;

            try
            {
                await _listener.ConnectAndListenAsync(_serverIp, _serverPort, ct);
                _attempt = 0;
                _reconnecting = false;
                SetState(ConnectionState.Connected);
            }
            catch (OperationCanceledException)
            {
                SetState(ConnectionState.Disconnected);
                throw;
            }
            catch
            {
                // Kết nối thất bại → thử reconnect
                if (!ct.IsCancellationRequested)
                    _ = Task.Run(() => ReconnectLoopAsync(ct));
                else
                    SetState(ConnectionState.Disconnected);
            }
        }

        private void OnConnectionLost(string errorMessage)
        {
            if (_disposed || _reconnecting) return;

            var ct = _cts?.Token ?? CancellationToken.None;
            if (!ct.IsCancellationRequested)
                _ = Task.Run(() => ReconnectLoopAsync(ct));
        }

        private async Task ReconnectLoopAsync(CancellationToken ct)
        {
            if (_reconnecting) return;
            _reconnecting = true;

            SetState(ConnectionState.Reconnecting);

            while (!ct.IsCancellationRequested)
            {
                _attempt++;

                if (MaxRetries > 0 && _attempt > MaxRetries)
                {
                    _reconnecting = false;
                    SetState(ConnectionState.Disconnected);
                    InvokeOnUI(() => ReconnectFailed?.Invoke());
                    return;
                }

                InvokeOnUI(() => Reconnecting?.Invoke(_attempt, MaxRetries));

                // Exponential backoff: 1s, 2s, 4s, 8s, 15s (capped)
                int delay = Math.Min(BaseDelayMs * (1 << (_attempt - 1)), MaxDelayMs);
                try
                {
                    await Task.Delay(delay, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                // Thử kết nối lại
                CleanupCurrent();

                _socket = new SocketClient();
                _listener = new ClientEventListener(_socket, _uiControl);
                _listener.OnConnectionError += OnConnectionLost;

                try
                {
                    await _listener.ConnectAndListenAsync(_serverIp, _serverPort, ct);
                    _attempt = 0;
                    _reconnecting = false;
                    SetState(ConnectionState.Connected);
                    return; // Reconnect thành công
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Tiếp tục vòng lặp retry
                }
            }

            _reconnecting = false;
            SetState(ConnectionState.Disconnected);
        }

        private void CleanupCurrent()
        {
            if (_listener != null)
            {
                _listener.OnConnectionError -= OnConnectionLost;
                _listener.Dispose();
                _listener = null;
            }

            if (_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }
        }

        private void SetState(ConnectionState newState)
        {
            if (State == newState) return;
            State = newState;
            InvokeOnUI(() => StateChanged?.Invoke(newState));
        }

        private void InvokeOnUI(Action action)
        {
            if (_uiControl.IsDisposed) return;
            if (_uiControl.InvokeRequired)
                _uiControl.BeginInvoke(action);
            else
                action();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ConnectionGuard));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cts?.Cancel();
            _cts?.Dispose();
            CleanupCurrent();
        }
    }

    /// <summary>
    /// Trạng thái kết nối.
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting
    }
}
