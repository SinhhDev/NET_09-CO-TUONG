using System;
using System.Buffers.Binary;
using System.Text;

namespace CoTuongOnline.Network
{
    public enum PacketType : byte
    {
        // Xác thực
        Login = 10,
        LoginOK = 11,
        LoginFail = 12,

        // Trận đấu
        Move = 31,         // Di chuyển: "x1,y1,x2,y2"
        Surrender = 32,    // Đầu hàng
        Chat = 33,         // Tin nhắn chat
        Timeout = 34,      // Hết giờ suy nghĩ
        DrawOffer = 35,    // Đề nghị hòa
        DrawResponse = 36, // Phản hồi hòa: "ACCEPT" hoặc "REJECT"

        // Trạng thái game
        GameStart = 50,    // Bắt đầu game: "COLOR|OpponentName"
        GameEnd = 60       // Kết thúc game: "reason"
    }

    public static class Protocol
    {
        public const int HEADER_SIZE = 5;

        // Tạo gói tin xác thực / đăng nhập
        public static byte[] CreateLogin(string username) => CreatePacket(PacketType.Login, username);
        public static byte[] CreateLoginOK(string message = "") => CreatePacket(PacketType.LoginOK, message);
        public static byte[] CreateLoginFail(string reason) => CreatePacket(PacketType.LoginFail, reason);

        // Tạo gói tin trận đấu
        public static byte[] CreateMove(int x1, int y1, int x2, int y2) => CreatePacket(PacketType.Move, $"{x1},{y1},{x2},{y2}");
        public static byte[] CreateSurrender() => CreatePacket(PacketType.Surrender, "");
        public static byte[] CreateChat(string message) => CreatePacket(PacketType.Chat, message);
        public static byte[] CreateTimeout() => CreatePacket(PacketType.Timeout, "");
        public static byte[] CreateDrawOffer() => CreatePacket(PacketType.DrawOffer, "");
        public static byte[] CreateDrawResponse(bool accept) => CreatePacket(PacketType.DrawResponse, accept ? "ACCEPT" : "REJECT");
        public static byte[] CreateGameStart(string colorAndOpponent) => CreatePacket(PacketType.GameStart, colorAndOpponent);
        public static byte[] CreateGameEnd(string reason) => CreatePacket(PacketType.GameEnd, reason);

        /// <summary>
        /// Đọc độ dài payload từ 5 byte header theo chuẩn Big-Endian (Network Byte Order).
        /// Trả về -1 nếu header không đủ kích thước.
        /// </summary>
        public static int ReadPayloadLength(ReadOnlySpan<byte> headerBytes)
        {
            if (headerBytes.Length < HEADER_SIZE) return -1;
            return BinaryPrimitives.ReadInt32BigEndian(headerBytes.Slice(1, 4));
        }

        /// <summary>
        /// Tạo gói tin theo cấu trúc chuẩn: 1 byte Type + 4 bytes Big-Endian Length + UTF-8 Payload
        /// </summary>
        public static byte[] CreatePacket(PacketType type, string data)
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data ?? "");
            byte[] packet = new byte[HEADER_SIZE + dataBytes.Length];
            packet[0] = (byte)type;
            BinaryPrimitives.WriteInt32BigEndian(packet.AsSpan(1, 4), dataBytes.Length);
            Array.Copy(dataBytes, 0, packet, HEADER_SIZE, dataBytes.Length);
            return packet;
        }

        /// <summary>
        /// Parse dữ liệu nước đi từ chuỗi "x1,y1,x2,y2".
        /// Trả về (fromRow, fromCol, toRow, toCol) hoặc null nếu lỗi.
        /// </summary>
        public static (int fromRow, int fromCol, int toRow, int toCol)? ParseMove(string data)
        {
            if (string.IsNullOrEmpty(data)) return null;

            string[] parts = data.Split(',');
            if (parts.Length != 4) return null;

            if (int.TryParse(parts[0], out int x1) &&
                int.TryParse(parts[1], out int y1) &&
                int.TryParse(parts[2], out int x2) &&
                int.TryParse(parts[3], out int y2))
            {
                return (x1, y1, x2, y2);
            }

            return null;
        }

        /// <summary>
        /// Parse dữ liệu GameStart: "RED|OpponentName" hoặc "BLACK|OpponentName".
        /// </summary>
        public static bool ParseGameStart(string data, out bool isRed, out string opponentName)
        {
            isRed = false;
            opponentName = "Đối thủ";
            if (string.IsNullOrEmpty(data)) return false;

            string[] parts = data.Split('|');
            string color = parts[0].Trim().ToUpper();

            if (color == "START" && parts.Length > 1)
            {
                color = parts[1].Trim().ToUpper();
                if (parts.Length > 2) opponentName = parts[2].Trim();
            }
            else if (parts.Length > 1)
            {
                opponentName = parts[1].Trim();
            }

            if (color == "RED")
            {
                isRed = true;
                return true;
            }
            else if (color == "BLACK")
            {
                isRed = false;
                return true;
            }

            return false;
        }
    }
}
