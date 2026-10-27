using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoTuongOnline.Common;
using CoTuongOnline.Network;

namespace ChessServer
{
    /// <summary>
    /// 1 phòng chơi = 2 người chơi
    /// Authoritative Server: Kiểm tra tính hợp lệ của từng nước đi theo luật cờ trước khi chuyển tiếp
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

        // Authoritative Game State trên Server
        private readonly Board _board = new Board();
        private bool _isRedTurn = true;
        private readonly object _gameLock = new object();

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
            _board.Init();
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
        /// Lắng nghe bất đồng bộ từ sender → xác thực nước cờ → chuyển tiếp sang receiver
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
                    int payloadLen = Protocol.ReadPayloadLength(headerBuf);

                    if (payloadLen < 0 || payloadLen > 65536)
                    {
                        Logger.WriteLog($"[ROOM {_roomId}] {role} ({senderName}): payload length không hợp lệ ({payloadLen}), ngắt kết nối.");
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

                    // ── Bước 3: Ghi log và xử lý nghiệp vụ theo loại gói tin ────────────────
                    Logger.WriteLog($"[ROOM {_roomId}] {role} ({senderName}): packet type={(PacketType)packetType}, len={payloadLen}");

                    // Nếu là packet Chat (33) -> Xử lý qua ChatHandler
                    if (packetType == (byte)PacketType.Chat)
                    {
                        string chatMessage = Encoding.UTF8.GetString(payloadBuf);
                        ChatHandler.Instance.HandleRoomChat(sender, receiver, chatMessage, role, _roomId);
                        continue;
                    }

                    // Nếu là packet Move (31) -> Kiểm tra tính hợp lệ trên Server (Authoritative Validation)
                    if (packetType == (byte)PacketType.Move)
                    {
                        string moveData = Encoding.UTF8.GetString(payloadBuf);
                        var move = Protocol.ParseMove(moveData);
                        if (move == null)
                        {
                            Logger.WriteLog($"[ROOM {_roomId}] {role} ({senderName}): Nước đi sai định dạng: '{moveData}'");
                            continue;
                        }

                        bool isSenderRed = (role == "Đỏ");
                        lock (_gameLock)
                        {
                            // 1. Kiểm tra có đúng lượt người chơi hay không
                            if (isSenderRed != _isRedTurn)
                            {
                                Logger.WriteLog($"[ROOM {_roomId}] [CẢNH BÁO] {role} ({senderName}): Đi sai lượt!");
                                RoomManager.SendBytes(sender, Protocol.CreateChat("[Hệ thống] Chưa đến lượt của bạn!"));
                                continue;
                            }

                            // 2. Kiểm tra nước đi hợp lệ theo luật cờ trên Server
                            bool isValidMove = MoveValidator.TryMove(_board, move.Value.fromRow, move.Value.fromCol, move.Value.toRow, move.Value.toCol, _isRedTurn);
                            if (!isValidMove)
                            {
                                Logger.WriteLog($"[ROOM {_roomId}] [CẢNH BÁO] {role} ({senderName}): Nước đi không hợp lệ theo luật cờ ({moveData})!");
                                RoomManager.SendBytes(sender, Protocol.CreateChat("[Hệ thống] Nước đi không hợp lệ hoặc phạm luật cờ!"));
                                continue;
                            }

                            // Hợp lệ -> Đổi lượt
                            _isRedTurn = !_isRedTurn;

                            // Kiểm tra xem đối phương có bị chiếu bí không
                            if (ChessRules.IsCheckmate(_board, _isRedTurn))
                            {
                                Logger.WriteLog($"[ROOM {_roomId}] {role} ({senderName}) đã CHIẾU BÍ đối thủ!");
                            }
                        }
                    }

                    // Ghép lại packet gốc và gửi sang đối thủ
                    byte[] fullPacket = new byte[HEADER_SIZE + payloadLen];
                    Array.Copy(headerBuf, fullPacket, HEADER_SIZE);
                    Array.Copy(payloadBuf, 0, fullPacket, HEADER_SIZE, payloadLen);

                    RoomManager.SendBytes(receiver, fullPacket);

                    // Nếu là packet Surrender (32), Timeout (34) hoặc GameEnd (60 - Chiếu bí / Kết thúc trận)
                    if (packetType == (byte)PacketType.Surrender || packetType == (byte)PacketType.Timeout || packetType == (byte)PacketType.GameEnd)
                    {
                        string endReason = packetType switch
                        {
                            (byte)PacketType.Surrender => "đầu hàng",
                            (byte)PacketType.Timeout => "hết giờ suy nghĩ",
                            _ => "ván cờ kết thúc (chiếu bí)"
                        };
                        Logger.WriteLog($"[ROOM {_roomId}] {role} ({senderName}) {endReason}.");

                        if (Interlocked.Exchange(ref _gameEnded, 1) == 0)
                        {
                            // Đợi ngắn để receiver kịp nhận trọn vẹn gói tin trước khi đóng token
                            await Task.Delay(400);
                            _cts.Cancel();
                        }
                        return;
                    }

                    // Nếu đối thủ chấp nhận hòa cờ
                    if (packetType == (byte)PacketType.DrawResponse)
                    {
                        string responseText = Encoding.UTF8.GetString(payloadBuf);
                        if (responseText.Equals("ACCEPT", StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.WriteLog($"[ROOM {_roomId}] {role} ({senderName}) đã chấp nhận hòa cờ.");
                            if (Interlocked.Exchange(ref _gameEnded, 1) == 0)
                            {
                                await Task.Delay(400);
                                _cts.Cancel();
                            }
                            return;
                        }
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
                    Logger.WriteLog($"[ROOM {_roomId}] {role} ({senderName}) mất kết nối, thông báo đối thủ.");
                    RoomManager.SendBytes(receiver, Protocol.CreateGameEnd("Đối thủ đã mất kết nối hoặc rời khỏi phòng."));
                    try { await Task.Delay(300); } catch { }
                    _cts.Cancel();
                }

                ChatHandler.Instance.RemoveClient(sender);
                UserManager.Instance.Unregister(sender);

                try { sender.Close(); } catch { }
            }
        }
    }
}
