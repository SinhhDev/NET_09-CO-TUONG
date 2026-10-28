using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CoTuongOnline.Common;

namespace CoTuongOnline.Client
{
    /// <summary>
    /// Lưu trữ snapshot của một trạng thái bàn cờ tại một thời điểm
    /// </summary>
    public class ReplayStep
    {
        public Piece?[,] Grid { get; }
        public Point? LastFrom { get; }
        public Point? LastTo { get; }
        public string Description { get; }

        public ReplayStep(Piece?[,] grid, Point? lastFrom, Point? lastTo, string description)
        {
            Grid = grid;
            LastFrom = lastFrom;
            LastTo = lastTo;
            Description = description;
        }
    }

    /// <summary>
    /// Cửa sổ xem lại toàn bộ diễn biến các nước đi của ván cờ
    /// </summary>
    public class FormReplay : Form
    {
        private const int Cell = 58;
        private const int StartX = 25;
        private const int StartY = 25;

        private readonly List<ReplayStep> _steps;
        private readonly Dictionary<string, Image> _pieces;
        private bool _flipBoard;
        private int _currentIndex = 0;
        private readonly System.Windows.Forms.Timer _autoTimer = new System.Windows.Forms.Timer();

        // Controls
        private Label lblTitle = null!;
        private Label lblStatus = null!;
        private ListBox lstMoves = null!;
        private Button btnFirst = null!;
        private Button btnPrev = null!;
        private Button btnNext = null!;
        private Button btnLast = null!;
        private Button btnAutoPlay = null!;
        private Button btnFlip = null!;
        private Button btnClose = null!;

        private int ToScreenRow(int row) => BoardRenderer.ToScreenRow(row, _flipBoard);
        private int ToScreenCol(int col) => BoardRenderer.ToScreenCol(col, _flipBoard);

        public FormReplay(List<ReplayStep> steps, Dictionary<string, Image> pieces, bool flipBoard = false)
        {
            _steps = steps ?? new List<ReplayStep>();
            _pieces = pieces;
            _flipBoard = flipBoard;

            this.Text = "Cờ Tướng - Xem Lại Nước Đi (Replay)";
            this.ClientSize = new Size(780, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.DoubleBuffered = true;
            this.KeyPreview = true;

            InitControls();

            _autoTimer.Interval = 1200;
            _autoTimer.Tick += (s, e) =>
            {
                if (_currentIndex < _steps.Count - 1)
                {
                    GoToStep(_currentIndex + 1);
                }
                else
                {
                    StopAutoPlay();
                }
            };

            this.Paint += FormReplay_Paint;
            this.KeyDown += FormReplay_KeyDown;
            this.FormClosing += (s, e) => _autoTimer.Stop();

            // Nhảy tới nước đi cuối cùng lúc mở form
            GoToStep(_steps.Count > 0 ? _steps.Count - 1 : 0);
        }

        private void InitControls()
        {
            int panelX = 515;

            lblTitle = new Label
            {
                Text = "📜 LỊCH SỬ NƯỚC ĐI",
                Font = new Font("Arial", 12F, FontStyle.Bold),
                ForeColor = Color.DarkRed,
                Location = new Point(panelX, 15),
                Size = new Size(245, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblStatus = new Label
            {
                Text = "Nước đi: 0 / 0",
                Font = new Font("Arial", 10F, FontStyle.Bold),
                ForeColor = Color.DarkSlateBlue,
                Location = new Point(panelX, 42),
                Size = new Size(245, 22),
                TextAlign = ContentAlignment.MiddleCenter
            };

            lstMoves = new ListBox
            {
                Location = new Point(panelX, 68),
                Size = new Size(245, 310),
                Font = new Font("Segoe UI", 9F),
                IntegralHeight = false
            };

            for (int i = 0; i < _steps.Count; i++)
            {
                string prefix = i == 0 ? "[Khởi đầu] " : $"{i}. ";
                lstMoves.Items.Add(prefix + _steps[i].Description);
            }

            lstMoves.SelectedIndexChanged += (s, e) =>
            {
                if (lstMoves.SelectedIndex >= 0 && lstMoves.SelectedIndex != _currentIndex)
                {
                    GoToStep(lstMoves.SelectedIndex);
                }
            };

            // 4 Nút điều hướng nước đi: [|<<] [<] [>] [>>|]
            int btnY = 388;
            int btnW = 58;
            int btnH = 34;

            btnFirst = new Button
            {
                Text = "⏮",
                Font = new Font("Arial", 11F, FontStyle.Bold),
                Location = new Point(panelX, btnY),
                Size = new Size(btnW, btnH),
                BackColor = Color.LightSlateGray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnFirst.Click += (s, e) => GoToStep(0);

            btnPrev = new Button
            {
                Text = "◀",
                Font = new Font("Arial", 11F, FontStyle.Bold),
                Location = new Point(panelX + btnW + 4, btnY),
                Size = new Size(btnW, btnH),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnPrev.Click += (s, e) => GoToStep(_currentIndex - 1);

            btnNext = new Button
            {
                Text = "▶",
                Font = new Font("Arial", 11F, FontStyle.Bold),
                Location = new Point(panelX + (btnW + 4) * 2, btnY),
                Size = new Size(btnW, btnH),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnNext.Click += (s, e) => GoToStep(_currentIndex + 1);

            btnLast = new Button
            {
                Text = "⏭",
                Font = new Font("Arial", 11F, FontStyle.Bold),
                Location = new Point(panelX + (btnW + 4) * 3, btnY),
                Size = new Size(btnW, btnH),
                BackColor = Color.LightSlateGray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnLast.Click += (s, e) => GoToStep(_steps.Count - 1);

            btnAutoPlay = new Button
            {
                Text = "▶ Tự Động Chạy (1.2s)",
                Font = new Font("Arial", 9.5F, FontStyle.Bold),
                Location = new Point(panelX, 430),
                Size = new Size(245, 34),
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAutoPlay.Click += (s, e) => ToggleAutoPlay();

            btnFlip = new Button
            {
                Text = "🔄 Lật Góc Nhìn Bàn Cờ",
                Font = new Font("Arial", 9.5F, FontStyle.Bold),
                Location = new Point(panelX, 470),
                Size = new Size(245, 34),
                BackColor = Color.Goldenrod,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnFlip.Click += (s, e) =>
            {
                _flipBoard = !_flipBoard;
                this.Invalidate();
            };

            btnClose = new Button
            {
                Text = "Đóng",
                Font = new Font("Arial", 9.5F, FontStyle.Bold),
                Location = new Point(panelX, 510),
                Size = new Size(245, 34),
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnClose.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[]
            {
                lblTitle, lblStatus, lstMoves,
                btnFirst, btnPrev, btnNext, btnLast,
                btnAutoPlay, btnFlip, btnClose
            });
        }

        private void GoToStep(int stepIndex)
        {
            if (_steps.Count == 0) return;

            _currentIndex = Math.Clamp(stepIndex, 0, _steps.Count - 1);
            lblStatus.Text = $"Nước đi: {_currentIndex} / {_steps.Count - 1}";

            if (lstMoves.SelectedIndex != _currentIndex)
            {
                lstMoves.SelectedIndex = _currentIndex;
            }

            btnFirst.Enabled = _currentIndex > 0;
            btnPrev.Enabled = _currentIndex > 0;
            btnNext.Enabled = _currentIndex < _steps.Count - 1;
            btnLast.Enabled = _currentIndex < _steps.Count - 1;

            this.Invalidate();
        }

        private void ToggleAutoPlay()
        {
            if (_autoTimer.Enabled)
            {
                StopAutoPlay();
            }
            else
            {
                if (_currentIndex >= _steps.Count - 1)
                {
                    GoToStep(0);
                }
                _autoTimer.Start();
                btnAutoPlay.Text = "⏸ Tạm Dừng";
                btnAutoPlay.BackColor = Color.OrangeRed;
            }
        }

        private void StopAutoPlay()
        {
            _autoTimer.Stop();
            btnAutoPlay.Text = "▶ Tự Động Chạy (1.2s)";
            btnAutoPlay.BackColor = Color.SeaGreen;
        }

        private void FormReplay_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    GoToStep(_currentIndex - 1);
                    e.Handled = true;
                    break;
                case Keys.Right:
                    GoToStep(_currentIndex + 1);
                    e.Handled = true;
                    break;
                case Keys.Home:
                    GoToStep(0);
                    e.Handled = true;
                    break;
                case Keys.End:
                    GoToStep(_steps.Count - 1);
                    e.Handled = true;
                    break;
                case Keys.Space:
                    ToggleAutoPlay();
                    e.Handled = true;
                    break;
                case Keys.Escape:
                    this.Close();
                    break;
            }
        }

        private void FormReplay_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. Vẽ khung bàn cờ bằng renderer dùng chung
            BoardRenderer.DrawBoard(g, StartX, StartY, Cell, _flipBoard);

            if (_steps.Count == 0 || _currentIndex >= _steps.Count) return;

            var step = _steps[_currentIndex];

            // 2. Highlight nước đi gần nhất (Từ ô nào -> Đến ô nào)
            if (step.LastFrom.HasValue)
            {
                int sc = ToScreenCol(step.LastFrom.Value.X);
                int sr = ToScreenRow(step.LastFrom.Value.Y);
                BoardRenderer.DrawLastMoveHighlight(g, StartX, StartY, Cell, sc, sr, isDestination: false);
            }
            if (step.LastTo.HasValue)
            {
                int sc = ToScreenCol(step.LastTo.Value.X);
                int sr = ToScreenRow(step.LastTo.Value.Y);
                BoardRenderer.DrawLastMoveHighlight(g, StartX, StartY, Cell, sc, sr, isDestination: true);
            }

            // 3. Vẽ các quân cờ tại nước đi hiện tại
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    Piece? p = step.Grid[r, c];
                    if (p == null) continue;
                    string key = BoardRenderer.GetPieceKey(p.Type, p.IsRed);
                    if (_pieces.TryGetValue(key, out var img))
                    {
                        BoardRenderer.DrawPiece(g, img, StartX, StartY, Cell, ToScreenCol(c), ToScreenRow(r));
                    }
                }
            }

            // Khung 4 góc nổi bật ôm trên quân cờ vừa đi
            if (step.LastTo.HasValue)
            {
                int sc = ToScreenCol(step.LastTo.Value.X);
                int sr = ToScreenRow(step.LastTo.Value.Y);
                int cx = StartX + sc * Cell;
                int cy = StartY + sr * Cell;
                BoardRenderer.DrawCornerBrackets(g, BoardRenderer.LastMovePen, cx, cy, Cell - 2, 10);
            }
        }
    }
}
