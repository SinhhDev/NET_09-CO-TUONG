using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CoTuongOnline.Network
{
    public enum PacketType : byte
    {
        // Auth
        Login = 10, LoginOK = 11, LoginFail = 12, Logout = 20,
        
        // Game
        Ready = 30,        // Sẵn sàng chơi
        Move = 31,         // Di chuyển: "x1,y1,x2,y2"
        Surrender = 32,    // Đầu hàng
        Chat = 33,         // Chat: "message"
        Timeout = 34,      // Hết giờ suy nghĩ
        
        // Response
        ReadyOK = 40, MoveOK = 41, MoveFail = 42,
        SurrenderOK = 43,
        
        GameStart = 50, GameEnd = 60,
        Heartbeat = 99
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketHeader
    {
        public PacketType Type;
        public int Length;
    }

    public class Protocol
    {
        public const int HEADER_SIZE = 5;

        // Tạo gói tin xác thực / đăng nhập
        public static byte[] CreateLogin(string username) => CreatePacket(PacketType.Login, username);
        public static byte[] CreateLoginOK(string message = "") => CreatePacket(PacketType.LoginOK, message);
        public static byte[] CreateLoginFail(string reason) => CreatePacket(PacketType.LoginFail, reason);
        public static byte[] CreateLogout() => CreatePacket(PacketType.Logout, "");

        // Tạo gói tin trận đấu
        public static byte[] CreateReady() => CreatePacket(PacketType.Ready, "");
        public static byte[] CreateMove(int x1, int y1, int x2, int y2) => CreatePacket(PacketType.Move, $"{x1},{y1},{x2},{y2}");
        public static byte[] CreateSurrender() => CreatePacket(PacketType.Surrender, "");
        public static byte[] CreateChat(string message) => CreatePacket(PacketType.Chat, message);
        public static byte[] CreateTimeout() => CreatePacket(PacketType.Timeout, "");

        public static byte[] CreateGameStart(string color) => CreatePacket(PacketType.GameStart, color);
        public static byte[] CreateGameEnd(string reason) => CreatePacket(PacketType.GameEnd, reason);

        public static byte[] CreatePacket(PacketType type, string data)
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data ?? "");
            byte[] packet = new byte[HEADER_SIZE + dataBytes.Length];
            packet[0] = (byte)type;
            byte[] lenBytes = BitConverter.GetBytes(dataBytes.Length);
            Array.Copy(lenBytes, 0, packet, 1, 4);
            Array.Copy(dataBytes, 0, packet, HEADER_SIZE, dataBytes.Length);
            return packet;
        }

        // Đọc gói tin
        public static PacketType ReadType(byte[] buffer) => buffer.Length < HEADER_SIZE ? PacketType.Heartbeat : (PacketType)buffer[0];
        public static string ReadData(byte[] buffer)
        {
            if (buffer == null || buffer.Length < HEADER_SIZE)
                throw new ArgumentException("Buffer không hợp lệ.");
            int length = BitConverter.ToInt32(buffer, 1);
            if (buffer.Length < HEADER_SIZE + length)
                throw new ArgumentException("Buffer không đủ dữ liệu.");
            return Encoding.UTF8.GetString(buffer, HEADER_SIZE, length);
        }
    }
}
