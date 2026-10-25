using System;
using System.Collections.Generic;
using System.Timers;
using System.Threading;

namespace CoTuongOnline.Common
{
    // Trạng thái hiện tại của đồng hồ game
    public enum TimerState { Stopped, Running, Paused }

    /// <summary>
    /// Lưu trữ thông tin chi tiết của một nước đi trong ván cờ
    /// </summary>
    public class NuocDi
    {
        public DateTime Time;     // Thời điểm thực hiện nước đi
        public string MoTa;       // Mô tả dạng chữ (ví dụ: (1,2)->(3,4))
        public int[] From, To;    // Tọa độ ô đi (Từ đâu -> Đến đâu)
        public string QuanAn;     // Tên quân cờ bị ăn (nếu có)

        public NuocDi(DateTime time, string moTa, int[] from, int[] to, string quanAn = "")
        {
            Time = time; MoTa = moTa; From = from; To = to; QuanAn = quanAn;
        }
    }

    /// <summary>
    /// Quản lý thời gian ván đấu, thời gian đếm ngược từng lượt đi và lịch sử nước đi
    /// </summary>
    public class GameTimer : IDisposable
    {
        public const int TurnDurationSeconds = 45;

        // _gameTimer: Đếm tổng thời gian ván đấu (chu kỳ 100ms)
        // _countdownTimer: Đếm ngược thời gian suy nghĩ cho từng lượt (chu kỳ 1000ms = 1 giây)
        private System.Timers.Timer _gameTimer = new(100), _countdownTimer = new(1000);
        private long _gameTimeMs;                 // Tổng thời gian ván đấu tính bằng mili-giây
        private int _countdownSeconds = TurnDurationSeconds; // Thời gian suy nghĩ mỗi lượt
        private bool _countdownRunning;           // Trạng thái đếm ngược có đang chạy hay không
        private List<NuocDi> _lichSu = new();     // Danh sách lưu lại các nước đi trong ván
        private TimerState _state = TimerState.Stopped; // Trạng thái hiện tại của bộ đếm

        // Các sự kiện (Events) để thông báo ra bên ngoài giao diện (UI)
        public event Action<long>? GameTimeChanged;
        public event Action<int>? CountdownChanged;
        public event Action? CountdownEnd;
        public event Action<NuocDi>? NuocDiAdded;

        // Các thuộc tính (Properties) công khai để đọc dữ liệu
        public long GameTimeMs => _gameTimeMs;
        public int CountdownSeconds => _countdownSeconds;
        public bool CountdownRunning => _countdownRunning;
        public TimerState State => _state;

        public GameTimer()
        {
            // Cài đặt sự kiện khi bộ đếm tổng thời gian chạy mỗi 100ms
            _gameTimer.Elapsed += (s, e) => { 
                Interlocked.Add(ref _gameTimeMs, 100); 
                GameTimeChanged?.Invoke(_gameTimeMs); 
            };
            // Cài đặt sự kiện đếm ngược thời gian từng lượt
            _countdownTimer.Elapsed += OnCountdownTick;
        }

        /// <summary>
        /// Bắt đầu tính giờ cho toàn bộ ván đấu mới
        /// </summary>
        public void StartGame()
        {
            _state = TimerState.Running;
            _gameTimer.Start();
            _countdownSeconds = TurnDurationSeconds;
            _countdownTimer.Stop();
            _countdownTimer.Start();
            _countdownRunning = true;
        }

        /// <summary>
        /// Reset lại thời gian đếm ngược khi chuyển sang lượt đi mới
        /// </summary>
        public void StartTurn()
        {
            _countdownSeconds = TurnDurationSeconds;
            _countdownTimer.Stop();
            _countdownTimer.Start();
            _countdownRunning = true;
            CountdownChanged?.Invoke(TurnDurationSeconds);
        }

        /// <summary>
        /// Dừng đếm ngược thời gian của lượt hiện tại
        /// </summary>
        public void StopCountdown()
        {
            _countdownRunning = false;
            _countdownTimer.Stop();
        }

        /// <summary>
        /// Thêm một nước đi mới vào lịch sử ván cờ
        /// </summary>
        public void ThemNuocDi(int x1, int y1, int x2, int y2, string quanAn = "")
        {
            string moTa = $"({x1},{y1})->({x2},{y2})" + (quanAn != "" ? $" [Ăn {quanAn}]" : "");
            var nuocDi = new NuocDi(DateTime.Now, moTa, new[] { x1, y1 }, new[] { x2, y2 }, quanAn);
            _lichSu.Add(nuocDi); 
            NuocDiAdded?.Invoke(nuocDi);
        }

        /// <summary>
        /// Hoàn tác (Undo) nước đi gần nhất
        /// </summary>
        public void Undo() => _lichSu.RemoveAt(_lichSu.Count - 1);

        private void StartCountdown() { _countdownRunning = true; _countdownTimer.Start(); }
        
        /// <summary>
        /// Hàm xử lý mỗi khi đồng hồ đếm ngược trôi qua 1 giây
        /// </summary>
        private void OnCountdownTick(object? s, ElapsedEventArgs e)
        {
            if (_countdownSeconds > 0) 
            { 
                _countdownSeconds--; 
                CountdownChanged?.Invoke(_countdownSeconds); 
            }
            else 
            { 
                StopCountdown(); 
                CountdownEnd?.Invoke(); // Gọi sự kiện khi hết giờ suy nghĩ
            }
        }

        /// <summary>
        /// Dừng toàn bộ đồng hồ và đưa về trạng thái ban đầu
        /// </summary>
        public void StopAll()
        {
            _state = TimerState.Stopped; 
            _gameTimer.Stop(); 
            _countdownTimer.Stop();
            _gameTimeMs = 0; 
            _countdownSeconds = TurnDurationSeconds;
        }

        /// <summary>
        /// Tạm dừng ván đấu (Pause)
        /// </summary>
        public void Pause()
        {
            if (_state == TimerState.Running)
            {
                _state = TimerState.Paused;
                _gameTimer.Stop();
                StopCountdown();
            }
        }

        /// <summary>
        /// Lấy bản sao danh sách lịch sử các nước đi
        /// </summary>
        public List<NuocDi> GetLichSu() => new List<NuocDi>(_lichSu);

        /// <summary>
        /// Giải phóng tài nguyên bộ nhớ của Timer khi không sử dụng
        /// </summary>
        public void Dispose()
        {
            StopAll();
            _gameTimer.Dispose();
            _countdownTimer.Dispose();
        }
    }
}
