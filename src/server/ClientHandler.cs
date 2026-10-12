using System;
using System.Net.Sockets;
using System.Text;

namespace ChessServer
{
    public class ClientHandler
    {
        private TcpClient _client; // Lưu trữ thông tin của người chơi này

        // Constructor: Nhận người chơi từ Server truyền vào
        public ClientHandler(TcpClient client)
        {
            _client = client;
        }

        // Hàm này chứa vòng lặp đọc dữ liệu liên tục
        public void Process()
        {
            try
            {
                // Lấy luồng dữ liệu (Stream) từ người chơi để đọc/ghi
                NetworkStream stream = _client.GetStream();

                // Tạo một mảng byte trống để chứa dữ liệu người chơi gửi lên
                byte[] buffer = new byte[1024];

                while (true) // Vòng lặp vô tận để luôn nghe ngóng
                {
                    // Đọc dữ liệu. byteRead là số lượng ký tự nhận được
                    int byteRead = stream.Read(buffer, 0, buffer.Length);

                    // Nếu byteRead = 0 nghĩa là người chơi đã tắt app / ngắt kết nối
                    if (byteRead == 0) break;

                    // Dịch mảng byte đó thành chữ (String) để người đọc hiểu
                    string message = Encoding.UTF8.GetString(buffer, 0, byteRead);

                    // Ghi log lại việc nhận tin nhắn
                    Logger.WriteLog($"Nhận từ Client: {message}");

                    // Test thử: Trả lời lại Client để xác nhận Server đã nhận
                    byte[] response = Encoding.UTF8.GetBytes("Server đã nhận được tin: " + message);
                    stream.Write(response, 0, response.Length); // Gửi về lại Client
                }
            }
            catch (Exception ex) // Bắt lỗi lỡ rớt mạng giữa chừng không bị văng app
            {
                Logger.WriteLog($"Lỗi Client: {ex.Message}");
            }
            finally
            {
                // Dọn dẹp, đóng kết nối khi Client thoát
                _client.Close();
                Logger.WriteLog("Một Client đã ngắt kết nối.");
            }
        }
    }
}