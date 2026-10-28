using System;
using System.Collections.Generic;
using System.Text;
using CoTuongOnline.Network;

namespace CoTuongOnline.Client
{
    /// <summary>
    /// Bộ giải mã gói tin từ Server.
    /// Xử lý vấn đề TCP framing: gom các chunk rời rạc thành packet hoàn chỉnh
    /// dựa trên PacketHeader (5 byte: Type + Length).
    /// </summary>
    public class PacketParser
    {
        private const int HEADER_SIZE = Protocol.HEADER_SIZE;
        private readonly List<byte> _buffer = new List<byte>();

        /// <summary>
        /// Event được gọi mỗi khi parse xong 1 packet hoàn chỉnh.
        /// </summary>
        public event Action<PacketType, string>? OnPacketParsed;

        /// <summary>
        /// Nhận chunk dữ liệu thô từ SocketClient và tách thành các packet.
        /// Một chunk có thể chứa nhiều packet hoặc chỉ một phần packet.
        /// </summary>
        public void ReceiveData(byte[] chunk)
        {
            if (chunk == null || chunk.Length == 0) return;

            lock (_buffer)
            {
                _buffer.AddRange(chunk);
                ProcessBuffer();
            }
        }

        /// <summary>
        /// Xử lý buffer: đọc liên tục các packet cho đến khi không đủ dữ liệu.
        /// </summary>
        private void ProcessBuffer()
        {
            while (_buffer.Count >= HEADER_SIZE)
            {
                // Đọc header để biết độ dài payload theo chuẩn Network Byte Order
                byte[] headerBytes = _buffer.GetRange(0, HEADER_SIZE).ToArray();
                PacketType type = (PacketType)headerBytes[0];
                int length = Protocol.ReadPayloadLength(headerBytes);

                // Kiểm tra packet type và độ dài hợp lệ
                if (!Enum.IsDefined(typeof(PacketType), type) || length < 0 || length > 65536)
                {
                    // Packet lỗi, xóa 1 byte và thử lại
                    _buffer.RemoveAt(0);
                    continue;
                }

                int totalSize = HEADER_SIZE + length;

                // Chưa đủ dữ liệu cho packet này → chờ chunk tiếp
                if (_buffer.Count < totalSize)
                    break;

                // Đọc payload
                string data = length > 0
                    ? Encoding.UTF8.GetString(_buffer.GetRange(HEADER_SIZE, length).ToArray())
                    : string.Empty;

                // Xóa packet đã xử lý khỏi buffer
                _buffer.RemoveRange(0, totalSize);

                // Fire event
                OnPacketParsed?.Invoke(type, data);
            }
        }

        /// <summary>
        /// Xóa buffer (dùng khi disconnect/reconnect).
        /// </summary>
        public void Reset()
        {
            lock (_buffer)
            {
                _buffer.Clear();
            }
        }
    }
}
