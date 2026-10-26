using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
        private readonly string _player1Name;
        private readonly string _player2Name;
        private readonly string _roomId;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private int _gameEnded = 0; // 0: running, 1: ended

        public string RoomId => _roomId;
        public string Player1Name => _player1Name;
        public string Player2Name => _player2Name;

        private const int HEADER_SIZE = Protocol.HEADER_SIZE;

        public GameRoom(TcpClient player1, TcpClient player2, string player1Name = "Người chơi Đỏ", string player2Name = "Người chơi Đen")
        {
            _player1 = player1;
            _player2 = player2;
            _player1Name = player1Name;
            _player2Name = player2Name;
            _roomId = Guid.NewGuid().ToString()[..8];
        }

        public async Task StartAsync()
        {
            Logger.WriteLog($"[ROOM {_roomId}] Ván đấu bắt đầu! {_player1Name} (Đỏ) VS {_player2Name} (Đen)");

            // Gửi thông báo bắt đầu kèm tên đối thủ
            RoomManager.SendBytes(_player1, Protocol.CreateGameStart($"RED|{_player2Name}"));
            RoomManager.SendBytes(_player2, Protocol.CreateGameStart($"BLACK|{_player1Name}"));

            // Chạy 2 luồng lắng nghe song song với CancellationToken
            Task t1 = Task.Run(() => ListenFromAsync(_player1, _player2, "Đỏ", _player1Name, _cts.Token));
            Task t2 = Task.Run(() => ListenFromAsync(_player2, _player1, "Đen", _player2Name, _cts.Token));

            await Task.WhenAll(t1, t2);

            Logger.WriteLog($"[ROOM {_roomId}] Ván đấu kết thúc và phòng đã giải phóng hoàn toàn.");
        }

        /// <summary>
        /// Lắng nghe bất đồng bộ từ sender → chuyển tiếp sang receiver
        /// </summary>
        private async Task ListenFromAsync(TcpClient sender, TcpClient receiver, string role, string senderName, CancellationToken ct)
        {
            try
            {
                NetworkStream stream = sender.GetStream();
                byte[] headerBuf = new byte[HEADER_SIZE];

                while (!ct.IsCancellationRequested)
                {
                    // ── Bước 1: Đọc đúng 5 byte header ──────────────────────
                    int totalRead = 0;
                    while (totalRead < HEADER_SIZE)
                    {
                        int read = await stream.ReadAsync(headerBuf.AsMemory(totalRead, HEADER_SIZE - totalRead), ct);
                        if (read <= 0)
                        {
                            Logger.WriteLog($"[ROOM {_roomId}] {role} ({senderName}): kết nối đóng khi đọc header.");
                            return;
                        }
                        totalRead += read;
                    }

                    byte packetType = headerBuf[0];
                    int payloadLen = BitConverter.ToInt32(headerBuf, 1);

                    if (payloadLen < 0 || payloadLen > 65536)
                    {
                        Logger.WriteLog($"[ROOM {_roomId}] {role} ({senderName}): payload length không hợp lệ ({payloadLen}), bỏ qua.");
                        return;
                    }

                    // ── Bước 2: Đọc đúng payload bytes ──────────────────────
                    byte[] payloadBuf = new byte[payloadLen];
                    int payloadRead = 0;
                    while (payloadRead < payloadLen)
                    {
                        int read = await stream.ReadAsync(payloadBuf.AsMemory(payloadRead, payloadLen - payloadRead), ct);
                        if (read <= 0)
                        {
                            Logger.WriteLog($"[ROOM {_roomId}] {role} ({senderName}): kết nối đóng khi đọc payload.");
                            return;
                        }
                        payloadRead += read;
                    }

                    // ── Bước 3: Ghi log và relay sang đối thủ ────────────────
                    Logger.WriteLog($"[ROOM {_roomId}] {role} ({senderName}): packet type={(PacketType)packetType}, len={payloadLen}");

                    // Nếu là packet Chat (33) -> Xử lý qua ChatHandler
                    if (packetType == (byte)PacketType.Chat)
                    {
                        string chatMessage = Encoding.UTF8.GetString(payloadBuf);
                        ChatHandler.Instance.HandleRoomChat(sender, receiver, chatMessage, role, _roomId);
                        continue;
                    }

                    // Ghép lại packet gốc và gửi sang đối thủ
                    byte[] fullPacket = new byte[HEADER_SIZE + payloadLen];
                    Array.Copy(headerBuf, fullPacket, HEADER_SIZE);
                    Array.Copy(payloadBuf, 0, fullPacket, HEADER_SIZE, payloadLen);

                    RoomManager.SendBytes(receiver, fullPacket);

                    // Nếu là packet Surrender (32) hoặc Timeout (34) → Ván cờ kết thúc bình thường
                    if (packetType == (byte)PacketType.Surrender || packetType == (byte)PacketType.Timeout)
                    {
                        string endReason = packetType == (byte)PacketType.Surrender ? "đầu hàng" : "hết giờ suy nghĩ";
                        Logger.WriteLog($"[ROOM {_roomId}] {role} ({senderName}) {endReason}.");
                        if (Interlocked.Exchange(ref _gameEnded, 1) == 0)
                        {
                            _cts.Cancel();
                        }
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Bị cancel bình thường khi ván đấu kết thúc từ phía bên kia
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"[ROOM {_roomId}] {role} ({senderName}) mất kết nối: {ex.Message}");
            }
            finally
            {
                // Nếu ván kết thúc bất ngờ (mất kết nối hoặc thoát đột ngột mà chưa cờ gameEnded)
                if (Interlocked.Exchange(ref _gameEnded, 1) == 0)
                {
                    _cts.Cancel();
                    Logger.WriteLog($"[ROOM {_roomId}] {role} ({senderName}) mất kết nối, thông báo đối thủ.");
                    RoomManager.SendBytes(receiver, Protocol.CreateGameEnd("Đối thủ đã mất kết nối hoặc rời khỏi phòng."));
                }

                ChatHandler.Instance.RemoveClient(sender);
                UserManager.Instance.Unregister(sender);

                try { sender.Close(); } catch { }
            }
        }
    }
}
