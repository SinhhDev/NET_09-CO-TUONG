using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ChessServer
{
    public class ServerManager
    {
        private TcpListener _listener = null!; // Thằng gác cổng TCP
        private int _port = 54000;     // Cổng lắng nghe

        public void Start()
        {
            // Khởi tạo listener, IPAddress.Any = chấp nhận kết nối từ mọi IP
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            Logger.WriteLog($"=== SERVER CỜ TƯỚNG ĐÃ MỞ TẠI CỔNG {_port} ===");

            // Vòng lặp đón client liên tục
            while (true)
            {
                try
                {
                    // Chặn tại đây cho đến khi có client kết nối
                    TcpClient incomingClient = _listener.AcceptTcpClient();

                    // Ghi log IP của client vừa kết nối
                    Logger.WriteLog($"[+] Client kết nối từ: {incomingClient.Client.RemoteEndPoint}");

                    // Đăng ký người chơi vào UserManager để theo dõi danh sách Online
                    UserManager.Instance.Register(incomingClient);

                    // Đưa client vào RoomManager để xác thực đăng nhập & ghép cặp
                    _ = RoomManager.Instance.HandleNewClientAsync(incomingClient);
                }
                catch (SocketException ex)
                {
                    // Lỗi mạng: port bị chiếm, client ngắt đột ngột, v.v.
                    Logger.WriteLog($"[!] Lỗi chấp nhận kết nối: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // Lỗi không xác định — ghi log và tiếp tục, không để server sập
                    Logger.WriteLog($"[!] Lỗi không xác định: {ex.Message}");
                }
            }
        }
    }
}
