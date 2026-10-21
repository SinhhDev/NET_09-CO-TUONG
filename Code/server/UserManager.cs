using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace ChessServer
{
    public enum UserStatus
    {
        Online,     // Đã kết nối, đang rảnh
        Waiting,    // Đang trong hàng chờ ghép cặp
        InGame      // Đang tham gia ván đấu
    }

    /// <summary>
    /// Thông tin chi tiết của một người chơi (User Session)
    /// </summary>
    public class UserInfo
    {
        public string Username { get; set; } = string.Empty;
        public TcpClient Client { get; set; } = null!;
        public string EndPoint { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; } = DateTime.Now;
        public DateTime LastActiveAt { get; set; } = DateTime.Now;
        public UserStatus Status { get; set; } = UserStatus.Online;
        public string? CurrentRoomId { get; set; }

        public override string ToString()
        {
            return $"[{Username}] ({EndPoint}) - {Status}" + (CurrentRoomId != null ? $" - Room: {CurrentRoomId}" : "");
        }
    }

    /// <summary>
    /// Quản lý người dùng: Đăng ký, đăng nhập, theo dõi trạng thái và danh sách online
    /// (Nhiệm vụ: Trần Thế Hiển - Tuần 3)
    /// </summary>
    public class UserManager
    {
        private static UserManager? _instance;
        public static UserManager Instance => _instance ??= new UserManager();

        private readonly Dictionary<TcpClient, UserInfo> _users = new();
        private readonly object _lock = new();

        private UserManager() { }

        /// <summary>
        /// Đăng ký một Client mới khi kết nối vào server.
        /// Tự động sinh tên mặc định nếu chưa có tên người dùng.
        /// </summary>
        public UserInfo Register(TcpClient client, string? desiredUsername = null)
        {
            lock (_lock)
            {
                if (_users.TryGetValue(client, out var existingUser))
                    return existingUser;

                string endPoint = GetEndPointString(client);
                string username;

                if (!string.IsNullOrWhiteSpace(desiredUsername) && IsUsernameValid(desiredUsername, out _) && !IsUsernameTakenInternal(desiredUsername))
                {
                    username = desiredUsername.Trim();
                }
                else
                {
                    // Tự động tạo tên định danh theo cổng kết nối: NguoiChoi_xxxxx
                    int port = 0;
                    if (client.Client?.RemoteEndPoint is IPEndPoint ipEp)
                        port = ipEp.Port;
                    else
                        port = new Random().Next(1000, 9999);

                    username = $"NguoiChoi_{port}";

                    // Nếu lỡ trùng tên tự sinh thì thêm hậu tố
                    int suffix = 1;
                    string baseName = username;
                    while (IsUsernameTakenInternal(username))
                    {
                        username = $"{baseName}_{suffix++}";
                    }
                }

                UserInfo user = new UserInfo
                {
                    Username = username,
                    Client = client,
                    EndPoint = endPoint,
                    ConnectedAt = DateTime.Now,
                    LastActiveAt = DateTime.Now,
                    Status = UserStatus.Online
                };

                _users[client] = user;
                Logger.WriteLog($"[USER] Đăng ký thành công: {user.Username} ({user.EndPoint}). Hiện có {_users.Count} người online.");
                return user;
            }
        }

        /// <summary>
        /// Xử lý đăng nhập / đổi tên người dùng
        /// </summary>
        public bool Login(TcpClient client, string newUsername, out string errorMessage)
        {
            lock (_lock)
            {
                if (!_users.TryGetValue(client, out var user))
                {
                    errorMessage = "Chưa tìm thấy kết nối người dùng.";
                    return false;
                }

                if (!IsUsernameValid(newUsername, out errorMessage))
                    return false;

                newUsername = newUsername.Trim();

                if (IsUsernameTakenInternal(newUsername, client))
                {
                    errorMessage = "Tên đăng nhập đã có người sử dụng. Vui lòng chọn tên khác.";
                    return false;
                }

                string oldName = user.Username;
                user.Username = newUsername;
                user.LastActiveAt = DateTime.Now;
                errorMessage = string.Empty;

                Logger.WriteLog($"[USER] Người chơi '{oldName}' đổi tên thành '{user.Username}'.");
                return true;
            }
        }

        /// <summary>
        /// Xóa người dùng khi ngắt kết nối
        /// </summary>
        public void Unregister(TcpClient client)
        {
            lock (_lock)
            {
                if (_users.TryGetValue(client, out var user))
                {
                    _users.Remove(client);
                    Logger.WriteLog($"[USER] Người dùng ngắt kết nối: {user.Username} ({user.EndPoint}). Còn lại {_users.Count} người online.");
                }
            }
        }

        /// <summary>
        /// Cập nhật trạng thái người chơi (Online, Đang chờ, Đang chơi...)
        /// </summary>
        public void SetStatus(TcpClient client, UserStatus status, string? roomId = null)
        {
            lock (_lock)
            {
                if (_users.TryGetValue(client, out var user))
                {
                    user.Status = status;
                    user.CurrentRoomId = roomId;
                    user.LastActiveAt = DateTime.Now;
                    Logger.WriteLog($"[USER] Cập nhật trạng thái '{user.Username}': {status}" + (roomId != null ? $" (Phòng #{roomId})" : ""));
                }
            }
        }

        /// <summary>
        /// Cập nhật thời điểm hoạt động gần nhất
        /// </summary>
        public void UpdateActivity(TcpClient client)
        {
            lock (_lock)
            {
                if (_users.TryGetValue(client, out var user))
                    user.LastActiveAt = DateTime.Now;
            }
        }

        /// <summary>
        /// Lấy thông tin người dùng theo TcpClient
        /// </summary>
        public UserInfo? GetUser(TcpClient client)
        {
            lock (_lock)
            {
                _users.TryGetValue(client, out var user);
                return user;
            }
        }

        /// <summary>
        /// Tìm người dùng theo Username
        /// </summary>
        public UserInfo? GetUserByName(string username)
        {
            lock (_lock)
            {
                foreach (var user in _users.Values)
                {
                    if (string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase))
                        return user;
                }
                return null;
            }
        }

        /// <summary>
        /// Kiểm tra tên đăng nhập đã được sử dụng hay chưa
        /// </summary>
        public bool IsUsernameTaken(string username, TcpClient? excludeClient = null)
        {
            lock (_lock)
            {
                return IsUsernameTakenInternal(username, excludeClient);
            }
        }

        private bool IsUsernameTakenInternal(string username, TcpClient? excludeClient = null)
        {
            foreach (var pair in _users)
            {
                if (excludeClient != null && ReferenceEquals(pair.Key, excludeClient))
                    continue;

                if (string.Equals(pair.Value.Username, username, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ của tên người dùng:
        /// Độ dài 3 - 20 ký tự, không chứa ký tự đặc biệt nguy hiểm.
        /// </summary>
        public static bool IsUsernameValid(string? username, out string error)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                error = "Tên đăng nhập không được để trống.";
                return false;
            }

            string trimmed = username.Trim();
            if (trimmed.Length < 3 || trimmed.Length > 20)
            {
                error = "Tên đăng nhập phải có từ 3 đến 20 ký tự.";
                return false;
            }

            // Không cho phép ký tự điều khiển, ký tự phân cách dòng
            if (Regex.IsMatch(trimmed, @"[\r\n\t|]"))
            {
                error = "Tên đăng nhập không được chứa ký tự phân cách hoặc xuống dòng.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Số lượng người dùng đang online
        /// </summary>
        public int GetOnlineCount()
        {
            lock (_lock)
            {
                return _users.Count;
            }
        }

        /// <summary>
        /// Danh sách toàn bộ người dùng đang online
        /// </summary>
        public List<UserInfo> GetOnlineUsers()
        {
            lock (_lock)
            {
                return new List<UserInfo>(_users.Values);
            }
        }

        private static string GetEndPointString(TcpClient client)
        {
            try
            {
                return client.Client?.RemoteEndPoint?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
