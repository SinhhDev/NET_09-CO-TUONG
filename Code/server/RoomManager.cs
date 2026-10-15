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
                if (_waitingClient == null)
                {
                    // Chưa có ai chờ → cho vào hàng chờ
                    _waitingClient = client;
                    Logger.WriteLog($"[ROOM] Người chơi đang chờ đối thủ...");

                    // QUAN TRỌNG: Dùng packet Chat để báo client.
                    // Packet này sẽ được xử lý bởi client trước khi GameRoom bắt đầu,
                    // nên KHÔNG bị relay sang đối thủ (GameRoom chưa chạy).
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