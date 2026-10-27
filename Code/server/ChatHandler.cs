using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using CoTuongOnline.Network;

namespace ChessServer
{
    public class ChatHandler
    {
        private static ChatHandler? _instance;
        public static ChatHandler Instance => _instance ??= new ChatHandler();

        // ── Cấu hình chống spam ──
        public const int MinMessageIntervalMs = 700;       // Thời gian tối thiểu giữa 2 tin nhắn liên tiếp (ms)
        public const int MaxMessageLength = 200;           // Độ dài tối đa của 1 tin nhắn
        public const int SpamWindowSeconds = 5;            // Cửa sổ thời gian đếm số lượng tin nhắn (giây)
        public const int MaxMessagesPerWindow = 4;         // Tối đa 4 tin nhắn trong 5 giây
        public const int MuteDurationSeconds = 10;         // Thời gian khóa chat tạm thời khi vi phạm spam (giây)

        // ── Danh sách từ khóa cấm / nhạy cảm (Censorship list) ──
        private static readonly string[] BadWords = new[]
        {
            "dm", "dmm", "cl", "vcl", "vl", "cc", "loz", "lon", "buoi",
            "fuck", "shit", "bitch", "asshole", "ngu", "oc cho", "cho chet"
        };

        // Lưu thông tin phiên chat của từng client để kiểm soát spam
        private class ClientChatSession
        {
            public DateTime LastMessageTime = DateTime.MinValue;
            public string LastMessageText = string.Empty;
            public Queue<DateTime> RecentTimestamps = new();
            public DateTime MutedUntil = DateTime.MinValue;
        }

        private readonly Dictionary<TcpClient, ClientChatSession> _sessions = new();
        private readonly object _lock = new();

        private ChatHandler() { }

        /// <summary>
        /// Xử lý gói tin Chat trong GameRoom:
        /// 1. Kiểm tra spam & giới hạn tần suất gửi tin.
        /// 2. Lọc từ ngữ phản cảm (Bad words filter).
        /// 3. Chuyển tiếp tin nhắn sạch tới đối thủ hoặc gửi cảnh báo về người gửi.
        /// </summary>
        public bool HandleRoomChat(TcpClient sender, TcpClient receiver, string rawMessage, string senderRole, string roomId)
        {
            var (isAllowed, warning, cleanMessage) = ValidateAndFilterMessage(sender, rawMessage);

            if (!isAllowed)
            {
                // Nếu bị chặn bởi bộ lọc spam: gửi thông báo cảnh báo về lại người gửi
                if (!string.IsNullOrEmpty(warning))
                {
                    RoomManager.SendBytes(sender, Protocol.CreateChat(warning));
                    Logger.WriteLog($"[CHAT][ROOM {roomId}] {senderRole} bị chặn: {warning}");
                }
                return false;
            }

            // Gửi tin nhắn hợp lệ (đã lọc từ cấm) sang đối thủ
            RoomManager.SendBytes(receiver, Protocol.CreateChat(cleanMessage));
            Logger.WriteLog($"[CHAT][ROOM {roomId}] {senderRole}: {cleanMessage}");
            return true;
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ của tin nhắn, chống spam và lọc từ khóa phản cảm.
        /// </summary>
        public (bool isAllowed, string? warning, string filteredMessage) ValidateAndFilterMessage(TcpClient client, string? rawMessage)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
                return (false, null, string.Empty);

            string text = rawMessage.Trim();

            // 1. Kiểm tra độ dài tin nhắn
            if (text.Length > MaxMessageLength)
                text = text.Substring(0, MaxMessageLength);

            // Ngăn chặn người chơi giả mạo thông báo hệ thống
            if (text.StartsWith("[Hệ thống]", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("[Server]", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Replace("[Hệ thống]", "", StringComparison.OrdinalIgnoreCase)
                           .Replace("[Server]", "", StringComparison.OrdinalIgnoreCase).Trim();
                if (string.IsNullOrEmpty(text))
                    return (false, null, string.Empty);
            }

            DateTime now = DateTime.Now;

            lock (_lock)
            {
                if (!_sessions.TryGetValue(client, out var session))
                {
                    session = new ClientChatSession();
                    _sessions[client] = session;
                }

                // 2. Kiểm tra nếu người chơi đang bị mute (tạm khóa chat do spam)
                if (now < session.MutedUntil)
                {
                    int remainingSeconds = (int)Math.Ceiling((session.MutedUntil - now).TotalSeconds);
                    return (false, $"[Hệ thống] Bạn đang bị tạm khóa chat do spam! Còn {remainingSeconds} giây.", string.Empty);
                }

                // 3. Kiểm tra gửi tin quá nhanh (tần suất liên tiếp)
                double msSinceLast = (now - session.LastMessageTime).TotalMilliseconds;
                if (msSinceLast < MinMessageIntervalMs)
                {
                    return (false, "[Hệ thống] Bạn đang gửi tin nhắn quá nhanh, vui lòng chậm lại!", string.Empty);
                }

                // 4. Kiểm tra tin nhắn trùng lặp liên tục
                if (string.Equals(text, session.LastMessageText, StringComparison.OrdinalIgnoreCase) && msSinceLast < 3000)
                {
                    return (false, "[Hệ thống] Vui lòng không gửi nội dung trùng lặp liên tục!", string.Empty);
                }

                // 5. Kiểm tra cửa sổ trượt (sliding window) chống spam dồn dập
                while (session.RecentTimestamps.Count > 0 && (now - session.RecentTimestamps.Peek()).TotalSeconds > SpamWindowSeconds)
                {
                    session.RecentTimestamps.Dequeue();
                }

                session.RecentTimestamps.Enqueue(now);

                if (session.RecentTimestamps.Count > MaxMessagesPerWindow)
                {
                    session.MutedUntil = now.AddSeconds(MuteDurationSeconds);
                    return (false, $"[Hệ thống] Phát hiện spam! Bạn bị tạm khóa chat {MuteDurationSeconds} giây.", string.Empty);
                }

                // Cập nhật thông tin tin nhắn hợp lệ
                session.LastMessageTime = now;
                session.LastMessageText = text;
            }

            // 6. Lọc từ ngữ phản cảm (Bad words filter)
            string cleanText = CensorProfanity(text);

            return (true, null, cleanText);
        }

        /// <summary>
        /// Bộ lọc từ cấm: Thay thế các từ nhạy cảm / chửi bậy bằng dấu sao (***)
        /// </summary>
        public static string CensorProfanity(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string result = text;

            foreach (var badWord in BadWords)
            {
                // Dùng regex khớp theo ranh giới từ để tránh thay thế nhầm từ chứa chuỗi con
                string pattern = $@"\b{Regex.Escape(badWord)}\b";
                result = Regex.Replace(result, pattern, new MatchEvaluator(m => new string('*', m.Length)), RegexOptions.IgnoreCase);
            }

            return result;
        }

        /// <summary>
        /// Dọn dẹp session khi người chơi rời khỏi server
        /// </summary>
        public void RemoveClient(TcpClient client)
        {
            lock (_lock)
            {
                _sessions.Remove(client);
            }
        }
    }
}
