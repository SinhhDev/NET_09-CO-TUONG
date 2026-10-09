using System;
using System.IO;

namespace ChessServer
{
    public class Logger
    {
        // Hàm này có chữ 'static' để gọi ở đâu cũng được mà không cần tạo mới đối tượng
        public static void WriteLog(string message)
        {
            // Lấy thời gian hiện tại (VD: 15/10/2023 20:30:00)
            string timeStamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            // Tạo chuỗi log hoàn chỉnh
            string logLine = $"[{timeStamp}] {message}";

            // 1. In ra màn hình Console cho mình dễ nhìn
            Console.WriteLine(logLine);

            // 2. Ghi nối tiếp vào file server_log.txt. Nếu file chưa có, nó tự tạo!
            // File này sẽ nằm trong thư mục bin/Debug/net... của project.
            File.AppendAllText("server_log.txt", logLine + Environment.NewLine);
        }
    }
}