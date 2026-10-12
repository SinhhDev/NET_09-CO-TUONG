﻿using System;

namespace ChessServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Server ChessGame_Online";

            // ===== STRESS TEST =====
            Console.WriteLine("========================================");
            Console.WriteLine("     CHẠY STRESS TEST CHAT HANDLER     ");
            Console.WriteLine("========================================");
            var chat = new ChatHandler();
            chat.RunStressTest();
            Console.WriteLine("========================================");
            Console.WriteLine("     STRESS TEST HOÀN THÀNH!           ");
            Console.WriteLine("========================================\n");
            Console.WriteLine("Nhấn Enter để khởi động Server...");
            Console.ReadLine();

            // ===== KHỞI ĐỘNG SERVER =====
            ServerManager server = new ServerManager();
            server.Start();

            Console.ReadLine();
        }
    }
}