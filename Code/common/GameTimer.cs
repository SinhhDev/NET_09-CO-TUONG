using System;
using System.Timers;

namespace CoTuongOnline.Common
{
    // Trạng thái hiện tại của đồng hồ game
    public enum TimerState { Stopped, Running }

    /// <summary>
    /// Quản lý thời gian đếm ngược từng lượt đi (Turn)
    /// </summary>
    public class GameTimer : IDisposable
    {
        public const int TurnDurationSeconds = 45;

        private readonly System.Timers.Timer _countdownTimer = new(1000);
        private int _countdownSeconds = TurnDurationSeconds;
        private bool _countdownRunning;
        private TimerState _state = TimerState.Stopped;
        private readonly object _lock = new();

        // Các sự kiện thông báo ra bên ngoài UI
        public event Action<int>? CountdownChanged;
        public event Action? CountdownEnd;

        public TimerState State
        {
            get { lock (_lock) return _state; }
        }

        public GameTimer()
        {
            _countdownTimer.Elapsed += OnCountdownTick;
        }

        /// <summary>
        /// Bắt đầu tính giờ cho ván đấu
        /// </summary>
        public void StartGame()
        {
            StartTurn();
        }

        /// <summary>
        /// Bắt đầu lượt đi mới (Reset lại 45s)
        /// </summary>
        public void StartTurn()
        {
            lock (_lock)
            {
                _state = TimerState.Running;
                _countdownSeconds = TurnDurationSeconds;
                _countdownTimer.Stop();
                _countdownTimer.Start();
                _countdownRunning = true;
            }
            CountdownChanged?.Invoke(TurnDurationSeconds);
        }

        /// <summary>
        /// Dừng đếm ngược thời gian của lượt hiện tại
        /// </summary>
        public void StopCountdown()
        {
            lock (_lock)
            {
                _countdownRunning = false;
                _countdownTimer.Stop();
            }
        }

        /// <summary>
        /// Hàm xử lý mỗi khi đồng hồ đếm ngược trôi qua 1 giây
        /// </summary>
        private void OnCountdownTick(object? s, ElapsedEventArgs e)
        {
            int currentSeconds;
            bool ended = false;

            lock (_lock)
            {
                if (!_countdownRunning) return;

                if (_countdownSeconds > 0)
                {
                    _countdownSeconds--;
                    currentSeconds = _countdownSeconds;
                }
                else
                {
                    _countdownRunning = false;
                    _countdownTimer.Stop();
                    ended = true;
                    currentSeconds = 0;
                }
            }

            if (ended)
            {
                CountdownEnd?.Invoke();
            }
            else
            {
                CountdownChanged?.Invoke(currentSeconds);
            }
        }

        /// <summary>
        /// Dừng toàn bộ đồng hồ và đưa về trạng thái ban đầu
        /// </summary>
        public void StopAll()
        {
            lock (_lock)
            {
                _state = TimerState.Stopped;
                _countdownTimer.Stop();
                _countdownRunning = false;
                _countdownSeconds = TurnDurationSeconds;
            }
        }

        /// <summary>
        /// Giải phóng tài nguyên
        /// </summary>
        public void Dispose()
        {
            StopAll();
            _countdownTimer.Dispose();
        }
    }
}
