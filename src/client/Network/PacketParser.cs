
using System;
using System.Collections.Generic;
using System.Buffers.Binary;
using System.Text;
using CoTuongOnline.Network;

namespace CoTuongOnline.Client
{
    /// <summary>
    /// Packet đã được tách hoàn chỉnh từ luồng TCP.
    /// </summary>
    public sealed class ParsedPacket
    {
        public PacketType Type { get; }
        public string Data { get; }

        public ParsedPacket(PacketType type, string data)
        {
            Type = type;
            Data = data;
        }
    }

    /// <summary>
    /// Đọc packet theo Protocol.cs của Quốc An:
    /// Header = 1 byte Type + 4 byte Length.
    /// Payload = chuỗi UTF-8 có độ dài Length byte.
    /// </summary>
    public sealed class PacketParser
    {
        private const int HeaderSize = 5;
        private const int MaxPayloadSize = 1024 * 1024; // 1 MB

        private readonly List<byte> _buffer = new List<byte>();
        private readonly object _lock = new object();

        public event Action<ParsedPacket>? PacketReceived;
        public event Action<string>? ParseError;

        /// <summary>
        /// Đưa dữ liệu vừa nhận từ SocketClient vào parser.
        /// Không giả định mỗi lần gọi tương ứng một packet.
        /// </summary>
        public void Feed(byte[] chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));

            lock (_lock)
            {
                _buffer.AddRange(chunk);

                while (_buffer.Count >= HeaderSize)
                {
                    byte typeValue = _buffer[0];

                    // Protocol.cs dùng Marshal ghi int vào header.
                    // Little-endian tương ứng môi trường Windows/.NET thông thường.
                    int length = BinaryPrimitives.ReadInt32LittleEndian(
                        new byte[]
                        {
                            _buffer[1],
                            _buffer[2],
                            _buffer[3],
                            _buffer[4]
                        });

                    if (length < 0 || length > MaxPayloadSize)
                    {
                        _buffer.Clear();
                        ParseError?.Invoke(
                            $"Độ dài packet không hợp lệ: {length}");
                        return;
                    }

                    int packetSize = HeaderSize + length;

                    // Chưa nhận đủ payload, đợi lần Feed tiếp theo.
                    if (_buffer.Count < packetSize)
                        return;

                    byte[] payload = new byte[length];

                    if (length > 0)
                    {
                        _buffer.CopyTo(
                            HeaderSize, payload, 0, length);
                    }

                    _buffer.RemoveRange(0, packetSize);

                    if (!Enum.IsDefined(typeof(PacketType), typeValue))
                    {
                        ParseError?.Invoke(
                            $"PacketType không hợp lệ: {typeValue}");
                        continue;
                    }

                    var packet = new ParsedPacket(
                        (PacketType)typeValue,
                        Encoding.UTF8.GetString(payload));

                    // Phát sự kiện ngay theo thứ tự packet được tách.
                    // Handler không nên chặn lâu trong callback này.
                    try
                    {
                        PacketReceived?.Invoke(packet);
                    }
                    catch (Exception ex)
                    {
                        ParseError?.Invoke(
                            $"Lỗi xử lý packet: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Xóa dữ liệu còn tồn đọng, ví dụ khi bắt đầu trận mới
        /// hoặc kết nối lại.
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _buffer.Clear();
            }
        }
    }
}
