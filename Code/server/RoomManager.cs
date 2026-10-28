using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoTuongOnline.Network;

namespace ChessServer
{
    /// <summary>
    /// Quản lý phòng chơi — ghép cặp 2 người chơi vào 1 phòng
    /// </summary>
    public class RoomManager
    {
        private readonly List<GameRoom> _rooms = new List<GameRoom>();
        private TcpClient? _waitingClient = null;
        private readonly object _lock = new object();

        // Singleton
        private static RoomManager? _instance;
        public static RoomManager Instance => _instance ??= new RoomManager();

        /// <summary>
        /// Xử lý client mới kết nối: Đọc gói tin Login đầu tiên (nếu có) rồi đưa vào hàng đợi ghép cặp
        /// </summary>
        public async Task HandleNewClientAsync(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();

                // Đợi đọc packet Login với thời gian chờ tối đa 4 giây
                byte[] headerBuf = await ReadExactBytesAsync(stream, Protocol.HEADER_SIZE, 4000);
                PacketType packetType = (PacketType)headerBuf[0];
                int payloadLen = Protocol.ReadPayloadLength(headerBuf);

                if (packetType == PacketType.Login && payloadLen > 0 && payloadLen <= 100)
                {
                    byte[] payloadBuf = await ReadExactBytesAsync(stream, payloadLen, 4000);
                    string desiredUsername = Encoding.UTF8.GetString(payloadBuf).Trim();

                    if (UserManager.Instance.Login(client, desiredUsername, out string errorMessage))
                    {
                        SendBytes(client, Protocol.CreateLoginOK($"Đăng nhập thành công! Chào {desiredUsername}."));
                        EnqueueClient(client);
                    }
                    else
                    {
                        SendBytes(client, Protocol.CreateLoginFail(errorMessage));
                        await Task.Delay(200); // Đợi buffer mạng kịp truyền gói tin đến Client trước khi ngắt kết nối
                        UserManager.Instance.Unregister(client);
                        try { client.Close(); } catch { }
                    }
                }
                else
                {
                    // Nếu client cũ không gửi packet Login, cho vào hàng đợi với tên mặc định
                    EnqueueClient(client);
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout khi đợi Login -> Cho vào hàng đợi với tên mặc định
                if (IsClientConnected(client))
                {
                    EnqueueClient(client);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"[ROOM] Lỗi xác thực client: {ex.Message}");
                UserManager.Instance.Unregister(client);
                try { client.Close(); } catch { }
            }
        }

        private static async Task<byte[]> ReadExactBytesAsync(NetworkStream stream, int count, int timeoutMs)
        {
            using var cts = new CancellationTokenSource(timeoutMs);
            byte[] buffer = new byte[count];
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(totalRead, count - totalRead), cts.Token);
                if (read <= 0) throw new IOException("Đóng kết nối khi đọc.");
                totalRead += read;
            }
            return buffer;
        }

        /// <summary>
        /// Đưa client vào hàng đợi ghép cặp
        /// </summary>
        private void EnqueueClient(TcpClient client)
        {
            lock (_lock)
            {
                if (!IsClientConnected(client))
                {
                    UserManager.Instance.Unregister(client);
                    try { client.Close(); } catch { }
                    return;
                }

                // Kiểm tra xem người chơi đang chờ trước đó có còn kết nối hay đã thoát
                if (_waitingClient != null && !IsClientConnected(_waitingClient))
                {
                    Logger.WriteLog("[ROOM] Client đang chờ đã mất kết nối, loại khỏi hàng đợi.");
                    UserManager.Instance.Unregister(_waitingClient);
                    try { _waitingClient.Close(); } catch { }
                    _waitingClient = null;
                }

                if (_waitingClient == null)
                {
                    // Chưa có ai chờ → cho vào hàng chờ
                    _waitingClient = client;
                    UserManager.Instance.SetStatus(client, UserStatus.Waiting);
                    string name = UserManager.Instance.GetUser(client)?.Username ?? "Người chơi";
                    Logger.WriteLog($"[ROOM] Người chơi '{name}' đang chờ đối thủ...");

                    SendBytes(client, Protocol.CreateChat("[Hệ thống] Đang chờ đối thủ..."));
                }
                else
                {
                    // Đã có người chờ → ghép cặp tạo phòng
                    TcpClient player1 = _waitingClient;
                    TcpClient player2 = client;
                    _waitingClient = null;

                    string name1 = UserManager.Instance.GetUser(player1)?.Username ?? "Người chơi 1";
                    string name2 = UserManager.Instance.GetUser(player2)?.Username ?? "Người chơi 2";

                    GameRoom room = new GameRoom(player1, player2, name1, name2);
                    _rooms.Add(room);

                    UserManager.Instance.SetStatus(player1, UserStatus.InGame, room.RoomId);
                    UserManager.Instance.SetStatus(player2, UserStatus.InGame, room.RoomId);

                    Logger.WriteLog($"[ROOM] Ghép cặp thành công! '{name1}' VS '{name2}'. Phòng #{room.RoomId} bắt đầu.");

                    _ = Task.Run(async () =>
                    {
                        await room.StartAsync();

                        lock (_lock)
                        {
                            _rooms.Remove(room);
                            Logger.WriteLog($"[ROOM] Đã giải phóng phòng #{room.RoomId}. Còn {_rooms.Count} phòng đang chạy.");
                        }
                    });
                }
            }
        }

        private static bool IsClientConnected(TcpClient client) => UserManager.IsClientConnected(client);

        public static void SendBytes(TcpClient client, byte[] data)
        {
            try
            {
                if (client == null || !client.Connected || client.Client == null) return;
                lock (client)
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(data, 0, data.Length);
                    stream.Flush();
                }
            }
            catch (ObjectDisposedException)
            {
                // Bỏ qua ngoại lệ khi socket đã bị đóng bình thường trong lúc kết thúc phòng
            }
            catch (IOException)
            {
                // Bỏ qua ngoại lệ khi kết nối mạng bị đối phương đóng đột ngột
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"[ROOM] Lỗi gửi tin: {ex.Message}");
            }
        }
    }
}
