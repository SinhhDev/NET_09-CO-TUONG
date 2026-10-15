using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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

        public GameRoom(TcpClient player1, TcpClient player2)
        {
            _player1 = player1;
            _player2 = player2;
            _roomId = Guid.NewGuid().ToString()[..8];
        }

        public void Start()
        {
            Logger.WriteLog($"[ROOM {_roomId}] Ván đấu bắt đầu!");

            // Báo 2 người chơi biết ván đấu bắt đầu và vai trò của họ
            RoomManager.SendMessage(_player1, "START|RED");
            RoomManager.SendMessage(_player2, "START|BLACK");

            // Chạy 2 luồng lắng nghe song song
            Task t1 = Task.Run(() => ListenFrom(_player1, _player2, "Đỏ"));
            Task t2 = Task.Run(() => ListenFrom(_player2, _player1, "Đen"));

            Task.WaitAll(t1, t2);

            Logger.WriteLog($"[ROOM {_roomId}] Ván đấu kết thúc.");
        }

        /// <summary>
        /// Lắng nghe từ sender → chuyển tiếp sang receiver
        /// </summary>
        private void ListenFrom(TcpClient sender, TcpClient receiver, string role)
        {
            byte[] buffer = new byte[4096];

            try
            {
                NetworkStream stream = sender.GetStream();

                while (_gameRunning)
                {
                    int byteRead = stream.Read(buffer, 0, buffer.Length);
                    if (byteRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, byteRead);
                    Logger.WriteLog($"[ROOM {_roomId}] {role}: {message}");

                    // Chuyển tiếp sang đối thủ
                    RoomManager.SendMessage(receiver, message);
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
                RoomManager.SendMessage(receiver, "OPPONENT_LEFT|Đối thủ đã thoát khỏi phòng.");

                sender.Close();
                Logger.WriteLog($"[ROOM {_roomId}] {role} đã ngắt kết nối.");
            }
        }
    }
}