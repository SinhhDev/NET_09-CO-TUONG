using System;
using System.IO;

namespace ChessServer
{
    public class Logger
    {
        private static readonly object _fileLock = new object();

        // Hàm static để ghi log thread-safe từ mọi nơi mà không gây xung đột tài nguyên
        public static void WriteLog(string message)
        {
            // Lấy thời gian hiện tại (VD: 15/10/2023 20:30:00)
            string timeStamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            // Tạo chuỗi log hoàn chỉnh
            string logLine = $"[{timeStamp}] {message}";

            // 1. In ra màn hình Console cho dễ theo dõi
            Console.WriteLine(logLine);

            // 2. Ghi nối tiếp vào file server_log.txt an toàn giữa các luồng
            lock (_fileLock)
            {
                try
                {
                    File.AppendAllText("server_log.txt", logLine + Environment.NewLine);
                }
                catch
                {
                    // Tránh crash nếu file bị khóa bởi phần mềm bên ngoài
                }
            }
        }
    }
}
