using System;
using System.Collections.Generic;
using System.Net.Sockets;
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
        /// Khi có client mới kết nối — thử ghép cặp
        /// </summary>
        public void HandleNewClient(TcpClient client)
        {
            lock (_lock)
            {
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
                    Logger.WriteLog($"[ROOM] Người chơi đang chờ đối thủ...");

                    // Dùng packet Chat để báo client
                    SendBytes(client, Protocol.CreateChat("Đang chờ đối thủ..."));
                }
                else
                {
                    // Đã có người chờ → ghép cặp tạo phòng
                    TcpClient player1 = _waitingClient;
                    TcpClient player2 = client;
                    _waitingClient = null;

                    GameRoom room = new GameRoom(player1, player2);
                    _rooms.Add(room);

                    UserManager.Instance.SetStatus(player1, UserStatus.InGame, room.RoomId);
                    UserManager.Instance.SetStatus(player2, UserStatus.InGame, room.RoomId);

                    Logger.WriteLog($"[ROOM] Ghép cặp thành công! Phòng #{_rooms.Count} bắt đầu.");

                    Task.Run(async () =>
                    {
                        await room.StartAsync();

                        lock (_lock)
                        {
                            _rooms.Remove(room);
                            Logger.WriteLog($"[ROOM] Đã giải phóng phòng. Còn {_rooms.Count} phòng đang chạy.");
                        }
                    });
                }
            }
        }

        private static bool IsClientConnected(TcpClient client)
        {
            try
            {
                if (!client.Connected || client.Client == null) return false;
                // Kiểm tra trạng thái socket thực tế
                return !(client.Client.Poll(1000, SelectMode.SelectRead) && client.Client.Available == 0);
            }
            catch
            {
                return false;
            }
        }

        public static void SendBytes(TcpClient client, byte[] data)
        {
            try
            {
                lock (client)
                {
                    client.GetStream().Write(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"[ROOM] Lỗi gửi tin: {ex.Message}");
            }
        }
    }
}
