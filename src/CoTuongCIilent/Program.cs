```csharp
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

internal partial class Program
{
    // Hàm Main là điểm bắt đầu chạy chương trình
    private static async Task Main()
    {
        // Địa chỉ IP của máy chạy Server
        // Thay xxxx bằng IP thật của máy Server
        string serverIP = "xxxx";

        // Cổng kết nối mà Server đang sử dụng
        int port = 5000;

        // Tạo đối tượng Client TCP để kết nối đến Server
        using TcpClient client = new TcpClient();

        try
        {
            // Kết nối Client đến Server bằng IP và Port đã khai báo
            await client.ConnectAsync(serverIP, port);

            // Thông báo khi kết nối thành công
            Console.WriteLine("Da ket noi toi server!");

            // Lấy luồng dữ liệu để trao đổi với Server
            NetworkStream stream = client.GetStream();

            // Chuyển chuỗi HELLO thành mảng byte để gửi qua mạng
            byte[] data = Encoding.UTF8.GetBytes("HELLO");

            // Gửi dữ liệu từ Client đến Server
            await stream.WriteAsync(data);

            // Thông báo đã gửi dữ liệu thành công
            Console.WriteLine("Da gui du lieu toi server.");
        }
        catch (Exception ex)
        {
            // Hiển thị thông báo nếu kết nối hoặc gửi dữ liệu bị lỗi
            Console.WriteLine("Loi ket noi: " + ex.Message);
        }

        // Giữ cửa sổ Console mở để xem kết quả
        Console.ReadKey();
    }
}

