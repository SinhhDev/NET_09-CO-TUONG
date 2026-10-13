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
        private const int HEADER_SIZE = 5;

        // Tạo gói tin
        public static byte[] CreateReady() => CreatePacket(PacketType.Ready, "");
        public static byte[] CreateMove(int x1, int y1, int x2, int y2) => CreatePacket(PacketType.Move, $"{x1},{y1},{x2},{y2}");
        public static byte[] CreateSurrender() => CreatePacket(PacketType.Surrender, "");
        public static byte[] CreateChat(string message) => CreatePacket(PacketType.Chat, message);

        public static byte[] CreateGameStart(string color) => CreatePacket(PacketType.GameStart, color);
        public static byte[] CreateGameEnd(string reason) => CreatePacket(PacketType.GameEnd, reason);


        private static byte[] CreatePacket(PacketType type, string data)
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            PacketHeader header = new() { Type = type, Length = dataBytes.Length };
            
            byte[] headerBytes = StructToBytes(header);
            byte[] packet = new byte[HEADER_SIZE + dataBytes.Length];
            
            Array.Copy(headerBytes, packet, HEADER_SIZE);
            Array.Copy(dataBytes, 0, packet, HEADER_SIZE, dataBytes.Length);
            return packet;
        }

        // Đọc gói tin
        public static PacketType ReadType(byte[] buffer) => buffer.Length < HEADER_SIZE ? PacketType.Heartbeat : BytesToStruct<PacketHeader>(buffer).Type;
        public static string ReadData(byte[] buffer)
{
    if (buffer == null || buffer.Length < HEADER_SIZE)
        throw new ArgumentException("Buffer không hợp lệ.");
    var header = BytesToStruct<PacketHeader>(buffer);
    if (buffer.Length < HEADER_SIZE + header.Length)
        throw new ArgumentException("Buffer không đủ dữ liệu.");
    return Encoding.UTF8.GetString(buffer, HEADER_SIZE, header.Length);
}

        // Helper
        private static byte[] StructToBytes<T>(T data) where T : struct
        {
            int size = Marshal.SizeOf(data);
            byte[] bytes = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, bytes, 0, size);
            Marshal.FreeHGlobal(ptr);
            return bytes;
        }

        private static T BytesToStruct<T>(byte[] bytes) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(bytes, 0, ptr, size);
            T result = Marshal.PtrToStructure<T>(ptr);
            Marshal.FreeHGlobal(ptr);
            return result;
        }
    }
}
