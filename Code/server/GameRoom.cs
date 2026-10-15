using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using CoTuongOnline.Network;

namespace ChessServer
{
    /// <summary>
    /// 1 phòng chơi = 2 người chơi
    /// Nhận nước đi từ người này → gửi sang người kia
    /// </summary>
    public class GameRoom
    {
        private readonly TcpClient _player1;
        private readonly TcpClient _player2;
        private readonly string _roomId;
        private bool _gameRunning = true;

        // Kích thước header theo Protocol: 1 byte Type + 4 byte Length
        private const int HEADER_SIZE = 5;

        public GameRoom(TcpClient player1, TcpClient player2)
        {
            _player1 = player1;
            _player2 = player2;
            _roomId = Guid.NewGuid().ToString()[..8];
        }

        public async Task StartAsync()
        {
            Logger.WriteLog($"[ROOM {_roomId}] Ván đấu bắt đầu!");

            // Gửi vai trò cho 2 người chơi
            SendBytes(_player1, Protocol.CreateGameStart("RED"));
            SendBytes(_player2, Protocol.CreateGameStart("BLACK"));

            // Chạy 2 luồng lắng nghe song song
            Task t1 = Task.Run(() => ListenFrom(_player1, _player2, "Đỏ"));
            Task t2 = Task.Run(() => ListenFrom(_player2, _player1, "Đen"));

            await Task.WhenAll(t1, t2);

            Logger.WriteLog($"[ROOM {_roomId}] Ván đấu kết thúc.");
        }

        /// <summary>
        /// Lắng nghe từ sender → chuyển tiếp sang receiver
        /// Đọc đúng từng packet theo protocol 5-byte header để tránh dữ liệu bị lẫn lộn
        /// </summary>
        private void ListenFrom(TcpClient sender, TcpClient receiver, string role)
        {
            try
            {
                NetworkStream stream = sender.GetStream();
                byte[] headerBuf = new byte[HEADER_SIZE];

                while (_gameRunning)
                {
                    // ── Bước 1: Đọc đúng 5 byte header ──────────────────────
                    int totalRead = 0;
                    while (totalRead < HEADER_SIZE)
                    {
                        int read = stream.Read(headerBuf, totalRead, HEADER_SIZE - totalRead);
                        if (read <= 0)
                        {
                            Logger.WriteLog($"[ROOM {_roomId}] {role}: kết nối đóng khi đọc header.");
                            return;
                        }
                        totalRead += read;
                    }

                    byte packetType = headerBuf[0];
                    int payloadLen = BitConverter.ToInt32(headerBuf, 1);

                    // Kiểm tra độ dài hợp lệ
                    if (payloadLen < 0 || payloadLen > 65536)
                    {
                        Logger.WriteLog($"[ROOM {_roomId}] {role}: payload length không hợp lệ ({payloadLen}), bỏ qua.");
                        return;
                    }

                    // ── Bước 2: Đọc đúng payload bytes ──────────────────────
                    byte[] payloadBuf = new byte[payloadLen];
                    int payloadRead = 0;
                    while (payloadRead < payloadLen)
                    {
                        int read = stream.Read(payloadBuf, payloadRead, payloadLen - payloadRead);
                        if (read <= 0)
                        {
                            Logger.WriteLog($"[ROOM {_roomId}] {role}: kết nối đóng khi đọc payload.");
                            return;
                        }
                        payloadRead += read;
                    }

                    // ── Bước 3: Ghi log và relay sang đối thủ ────────────────
                    Logger.WriteLog($"[ROOM {_roomId}] {role}: packet type={packetType}, len={payloadLen}");

                    // Ghép lại packet gốc (header + payload) và gửi sang đối thủ
                    byte[] fullPacket = new byte[HEADER_SIZE + payloadLen];
                    Array.Copy(headerBuf, fullPacket, HEADER_SIZE);
                    Array.Copy(payloadBuf, 0, fullPacket, HEADER_SIZE, payloadLen);

                    try
                    {
                        lock (receiver)  // lock để tránh 2 luồng ghi đồng thời vào cùng 1 stream
                        {
                            receiver.GetStream().Write(fullPacket, 0, fullPacket.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog($"[ROOM {_roomId}] Lỗi gửi tới đối thủ: {ex.Message}");
                        return;
                    }

                    // Nếu là packet Surrender (32) → game kết thúc
                    if (packetType == (byte)PacketType.Surrender)
                    {
                        Logger.WriteLog($"[ROOM {_roomId}] {role} đầu hàng.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"[ROOM {_roomId}] {role} mất kết nối: {ex.Message}");
            }
            finally
            {
                _gameRunning = false;

                // Báo người còn lại biết đối thủ đã thoát
                SendBytes(receiver, Protocol.CreateGameEnd("Đối thủ đã thoát khỏi phòng."));

                try { sender.Close(); } catch { }
                Logger.WriteLog($"[ROOM {_roomId}] {role} đã ngắt kết nối.");
            }
        }

        private static void SendBytes(TcpClient client, byte[] data)
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
                Logger.WriteLog($"[ROOM] Lỗi gửi: {ex.Message}");
            }
        }
    }
}