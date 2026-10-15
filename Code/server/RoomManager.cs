using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ChessServer
{
	/// <summary>
	/// Quản lý phòng chơi — ghép cặp 2 người chơi vào 1 phòng
	/// </summary>
	public class RoomManager
	{
		// Danh sách phòng đang chạy
		private readonly List<GameRoom> _rooms = new List<GameRoom>();

		// Hàng chờ — người chơi đang đợi đối thủ
		private TcpClient? _waitingClient = null;
		private readonly object _lock = new object();

		// Singleton
		private static RoomManager? _instance;
		public static RoomManager Instance => _instance ??= new RoomManager();

		/// <summary>
		/// Khi có client mới kết nối — thử ghép cặp
		/// </summary>
		public void HandleNewClient(TcpClient client)
		{
			lock (_lock)
			{
				if (_waitingClient == null)
				{
					// Chưa có ai chờ → cho vào hàng chờ
					_waitingClient = client;
					Logger.WriteLog($"[ROOM] Người chơi đang chờ đối thủ...");

					// Báo client biết đang chờ
					SendMessage(client, "WAITING|Đang chờ đối thủ...");
				}
				else
				{
					// Đã có người chờ → ghép cặp tạo phòng
					TcpClient player1 = _waitingClient;
					TcpClient player2 = client;
					_waitingClient = null;

					// Tạo phòng mới
					GameRoom room = new GameRoom(player1, player2);
					_rooms.Add(room);

					Logger.WriteLog($"[ROOM] Ghép cặp thành công! Phòng #{_rooms.Count} bắt đầu.");

					// Chạy phòng trên luồng riêng
					Task.Run(() => room.Start());
				}
			}
		}

		/// <summary>
		/// Gửi tin nhắn tới 1 client
		/// </summary>
		public static void SendMessage(TcpClient client, string message)
		{
			try
			{
				byte[] data = System.Text.Encoding.UTF8.GetBytes(message);
				client.GetStream().Write(data, 0, data.Length);
			}
			catch (Exception ex)
			{
				Logger.WriteLog($"[ROOM] Lỗi gửi tin: {ex.Message}");
			}
		}
	}
}