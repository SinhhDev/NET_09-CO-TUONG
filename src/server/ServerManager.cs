using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks; // Cần cái này để xài Task (Đa luồng)

namespace ChessServer
{
    public class ServerManager
    {
        private TcpListener _listener; // Thằng gác cổng
        private int _port = 54000;     // Số cổng mở ra (không nên dùng cổng 80 hay 443 vì trùng web)

        public void Start()
        {
            // Khởi tạo gác cổng, IPAddress.Any nghĩa là ai kết nối tới máy bạn cũng được
            _listener = new TcpListener(IPAddress.Any, _port);

            // Bắt đầu làm việc!
            _listener.Start();
            Logger.WriteLog($"=== SERVER CỜ TƯỚNG ĐÃ MỞ TẠI CỔNG {_port} ===");

            // Vòng lặp đón khách liên tục
            while (true)
            {
                // AcceptTcpClient() sẽ làm Server "treo" ở dòng này cho tới khi có người vào
                TcpClient incomingClient = _listener.AcceptTcpClient();

                // Có người vào thì ghi log lại IP của họ
                Logger.WriteLog($"[+] Khách mới kết nối từ: {incomingClient.Client.RemoteEndPoint}");

                // Tạo một "nhân viên phục vụ" (ClientHandler) cho khách này
                ClientHandler handler = new ClientHandler(incomingClient);

                // Đa luồng (Multi-threading)
                // Ném ông khách này sang một luồng (Task) khác để nhân viên phục vụ, 
                // còn Server quay lại dòng while(true) để đón khách tiếp theo lập tức.
                Task.Run(() => handler.Process());
            }
        }
    }
}