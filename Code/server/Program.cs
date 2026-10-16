using System;

namespace ChessServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Server ChessGame_Online";

            // Khởi động Server
            ServerManager server = new ServerManager();
            server.Start();
        }
    }
}