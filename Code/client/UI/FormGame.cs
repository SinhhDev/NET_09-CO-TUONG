using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoTuongOnline.Common;
using CoTuongOnline.Network;

namespace CoTuongOnline.Client
{
    public partial class FormGame : Form
    {
        private const int Cell = 60;
        private const int StartX = 30;
        private const int StartY = 30;

        private Board _board = new Board();
        private Point? _selectedCell = null;
        private List<Point> _validMoves = new List<Point>();
        private List<Point> _blockedMoves = new List<Point>();
        private Point? _lastFrom = null;
        private Point? _lastTo = null;
        private bool _isRedTurn = true;

        // === NETWORK ===
        private ConnectionGuard? _connectionGuard;
        private bool _isOnline = false;
        private bool _isPlayerRed = true;

        private GameTimer _gameTimer = new GameTimer();

        private readonly Dictionary<string, Image> _pieces = new Dictionary<string, Image>();

        private static readonly Dictionary<PieceType, string> _pieceKeyMap = new()
        {
            { PieceType.General,  "vua"   },
            { PieceType.Advisor,  "si"    },
            { PieceType.Elephant, "tuong" },
            { PieceType.Horse,    "ma"    },
            { PieceType.Rook,     "xe"    },
            { PieceType.Cannon,   "phao"  },
            { PieceType.Soldier,  "tot"   },
        };

        public FormGame()
        {
            InitializeComponent();
            this.ClientSize = new Size(StartX * 2 + 8 * Cell + 200, StartY * 2 + 9 * Cell);
            this.Text = "Cờ Tướng Online";
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Paint += FormGame_Paint;
            this.Load += FormGame_Load;
            this.MouseClick += FormGame_MouseClick;
            LoadImages();
            _board.Init();
        }

        private void FormGame_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 222, 173)),
                            StartX, StartY, 8 * Cell, 9 * Cell);

            Pen pen = new Pen(Color.Black, 2);
            g.DrawRectangle(pen, StartX, StartY, 8 * Cell, 9 * Cell);

            for (int i = 1; i < 9; i++)
                g.DrawLine(pen, StartX, StartY + i * Cell, StartX + 8 * Cell, StartY + i * Cell);

            for (int j = 0; j <= 8; j++)
            {
                if (j == 0 || j == 8)
                    g.DrawLine(pen, StartX + j * Cell, StartY, StartX + j * Cell, StartY + 9 * Cell);
                else
                {
                    g.DrawLine(pen, StartX + j * Cell, StartY, StartX + j * Cell, StartY + 4 * Cell);
                    g.DrawLine(pen, StartX + j * Cell, StartY + 5 * Cell, StartX + j * Cell, StartY + 9 * Cell);
                }
            }

            g.DrawLine(pen, StartX + 3 * Cell, StartY, StartX + 5 * Cell, StartY + 2 * Cell);
            g.DrawLine(pen, StartX + 5 * Cell, StartY, StartX + 3 * Cell, StartY + 2 * Cell);
            g.DrawLine(pen, StartX + 3 * Cell, StartY + 7 * Cell, StartX + 5 * Cell, StartY + 9 * Cell);
            g.DrawLine(pen, StartX + 5 * Cell, StartY + 7 * Cell, StartX + 3 * Cell, StartY + 9 * Cell);

            int[][] marks = {
                new[]{1,2}, new[]{7,2},
                new[]{0,3}, new[]{2,3}, new[]{4,3}, new[]{6,3}, new[]{8,3},
                new[]{1,7}, new[]{7,7},
                new[]{0,6}, new[]{2,6}, new[]{4,6}, new[]{6,6}, new[]{8,6}
            };
            foreach (var m in marks) DrawMark(g, pen, m[0], m[1]);

            Font fontRiver = new Font("SimSun", 24, FontStyle.Bold);
            g.DrawString("楚河", fontRiver, Brushes.DarkRed, StartX + 1.4f * Cell, StartY + 4.15f * Cell);
            g.DrawString("漢界", fontRiver, Brushes.DarkRed, StartX + 4.8f * Cell, StartY + 4.15f * Cell);

            using (var lastBrush = new SolidBrush(Color.FromArgb(110, 100, 180, 255)))
            {
                foreach (var c in new[] { _lastFrom, _lastTo })
                    if (c.HasValue)
                        g.FillRectangle(lastBrush, StartX + c.Value.X * Cell - Cell / 2, StartY + c.Value.Y * Cell - Cell / 2, Cell, Cell);
            }

            // [YÊU CẦU 1] Chiếu tướng: Hiện nền đỏ bao quanh quân Tướng bị chiếu
            foreach (bool isRed in new[] { true, false })
            {
                if (ChessRules.IsUnderCheck(_board, isRed))
                {
                    Point? genPos = ChessRules.FindGeneral(_board, isRed);
                    if (genPos.HasValue)
                    {
                        int gx = StartX + genPos.Value.X * Cell - Cell / 2;
                        int gy = StartY + genPos.Value.Y * Cell - Cell / 2;
                        using var checkBgBrush = new SolidBrush(Color.FromArgb(190, 255, 60, 60));
                        using var checkBorderPen = new Pen(Color.Red, 3.5f);
                        g.FillRectangle(checkBgBrush, gx, gy, Cell, Cell);
                        g.DrawRectangle(checkBorderPen, gx + 1, gy + 1, Cell - 2, Cell - 2);
                    }
                }
            }

            if (_selectedCell.HasValue)
            {
                int hx = StartX + _selectedCell.Value.X * Cell - Cell / 2;
                int hy = StartY + _selectedCell.Value.Y * Cell - Cell / 2;
                using var selBrush = new SolidBrush(Color.FromArgb(150, 255, 215, 0));
                using var selPen = new Pen(Color.Goldenrod, 3);
                g.FillRectangle(selBrush, hx, hy, Cell, Cell);
                g.DrawRectangle(selPen, hx + 1, hy + 1, Cell - 2, Cell - 2);
            }

            // Nước đi hợp lệ: chấm màu xanh lá
            using (var dotBrush = new SolidBrush(Color.FromArgb(160, 0, 180, 0)))
            {
                foreach (var p in _validMoves)
                {
                    if (_board.grid[p.Y, p.X] != null) continue;
                    int cx = StartX + p.X * Cell;
                    int cy = StartY + p.Y * Cell;
                    g.FillEllipse(dotBrush, cx - 10, cy - 10, 20, 20);
                }
            }

            // [YÊU CẦU 2] Nước đi làm Tướng team mình bị chiếu đổi từ chấm xanh qua chấm đỏ (bị chặn)
            using (var blockedDotBrush = new SolidBrush(Color.FromArgb(230, 220, 20, 20)))
            using (var blockedBorderPen = new Pen(Color.DarkRed, 1.5f))
            {
                foreach (var p in _blockedMoves)
                {
                    if (_board.grid[p.Y, p.X] != null) continue;
                    int cx = StartX + p.X * Cell;
                    int cy = StartY + p.Y * Cell;
                    g.FillEllipse(blockedDotBrush, cx - 10, cy - 10, 20, 20);
                    g.DrawEllipse(blockedBorderPen, cx - 10, cy - 10, 20, 20);
                }
            }

            for (int row = 0; row < 10; row++)
                for (int col = 0; col < 9; col++)
                {
                    Piece? p = _board.grid[row, col];
                    if (p == null) continue;
                    string color = p.IsRed ? "do" : "den";
                    string key = $"{_pieceKeyMap[p.Type]}_{color}";
                    DrawPiece(g, key, col, row);
                }

            // Viền đỏ hình tròn bao quanh Tướng bị chiếu để làm nổi bật hơn nữa
            using (var generalCirclePen = new Pen(Color.Red, 3f))
            {
                foreach (bool isRed in new[] { true, false })
                {
                    if (ChessRules.IsUnderCheck(_board, isRed))
                    {
                        Point? genPos = ChessRules.FindGeneral(_board, isRed);
                        if (genPos.HasValue)
                        {
                            int cx = StartX + genPos.Value.X * Cell;
                            int cy = StartY + genPos.Value.Y * Cell;
                            g.DrawEllipse(generalCirclePen, cx - 26, cy - 26, 52, 52);
                        }
                    }
                }
            }

            // Vòng tròn đỏ cho các nước ăn quân hợp lệ
            using (var ringPen = new Pen(Color.FromArgb(230, 220, 30, 30), 3))
            {
                foreach (var p in _validMoves)
                {
                    if (_board.grid[p.Y, p.X] == null) continue;
                    g.DrawEllipse(ringPen, StartX + p.X * Cell - 23, StartY + p.Y * Cell - 23, 46, 46);
                }
            }

            // Nước ăn quân nhưng bị chặn (khiến Tướng phe mình bị chiếu): vẽ vòng tròn và dấu chéo
            using (var blockedRingPen = new Pen(Color.FromArgb(180, 160, 0, 0), 2.5f))
            using (var blockedCrossPen = new Pen(Color.Red, 2.5f))
            {
                foreach (var p in _blockedMoves)
                {
                    if (_board.grid[p.Y, p.X] == null) continue;
                    int cx = StartX + p.X * Cell;
                    int cy = StartY + p.Y * Cell;
                    g.DrawEllipse(blockedRingPen, cx - 23, cy - 23, 46, 46);
                    g.DrawLine(blockedCrossPen, cx - 10, cy - 10, cx + 10, cy + 10);
                    g.DrawLine(blockedCrossPen, cx - 10, cy + 10, cx + 10, cy - 10);
                }
            }
        }

        private void FormGame_MouseClick(object? sender, MouseEventArgs e)
        {
            int col = (e.X - StartX + Cell / 2) / Cell;
            int row = (e.Y - StartY + Cell / 2) / Cell;
            if (col < 0 || col > 8 || row < 0 || row > 9) return;

            if (_isOnline && _isRedTurn != _isPlayerRed) return;

            Piece? clickedPiece = _board.grid[row, col];

            if (_selectedCell == null)
            {
                if (clickedPiece == null) return;
                if (clickedPiece.IsRed != _isRedTurn) return;
                _selectedCell = new Point(col, row);
                ComputeMovesFor(row, col);
                SoundManager.PlayMove();
                this.Invalidate();
                return;
            }

            int fromCol = _selectedCell.Value.X;
            int fromRow = _selectedCell.Value.Y;

            if (fromRow == row && fromCol == col)
            {
                _selectedCell = null;
                _validMoves.Clear();
                _blockedMoves.Clear();
                this.Invalidate();
                return;
            }

            if (clickedPiece != null && clickedPiece.IsRed == _isRedTurn)
            {
                _selectedCell = new Point(col, row);
                ComputeMovesFor(row, col);
                this.Invalidate();
                return;
            }

            // [YÊU CẦU 2] Nếu click vào ô bị chặn (chấm đỏ) -> Bị chặn, không cho đi
            Point targetPoint = new Point(col, row);
            if (_blockedMoves.Contains(targetPoint))
            {
                _chatBox.AppendLog("[Hệ thống] Nước đi này bị chặn vì làm Tướng bị chiếu!", Color.OrangeRed);
                return;
            }

            bool moved = MoveValidator.TryMove(_board, fromRow, fromCol, row, col, _isRedTurn);
            if (moved)
            {
                _lastFrom = new Point(fromCol, fromRow);
                _lastTo = new Point(col, row);
                if (clickedPiece != null)
                    SoundManager.PlayCapture();
                else
                    SoundManager.PlayMove();

                if (_isOnline && _connectionGuard?.Listener != null)
                    _ = _connectionGuard.Listener.SendMoveAsync(fromRow, fromCol, row, col);

                _isRedTurn = !_isRedTurn;
                _gameTimer.StopCountdown();
                _gameTimer.StartTurn();
                UpdateTimerLabelsAfterTurnChange();

                RefreshStatus();

                _selectedCell = null;
                _validMoves.Clear();
                _blockedMoves.Clear();

                if (ChessRules.IsCheckmate(_board, _isRedTurn))
                    ShowGameResult(!_isRedTurn);
            }
            else
            {
                _selectedCell = null;
                _validMoves.Clear();
                _blockedMoves.Clear();
            }

            this.Invalidate();
        }

        private void ComputeMovesFor(int row, int col)
        {
            _validMoves.Clear();
            _blockedMoves.Clear();

            Piece? piece = _board.grid[row, col];
            if (piece == null) return;

            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    Piece? target = _board.grid[r, c];
                    if (target != null && target.IsRed == piece.IsRed) continue;

                    if (MoveValidator.IsValidMove(_board, piece, row, col, r, c))
                    {
                        Board sim = _board.Clone();
                        sim.Move(row, col, r, c);
                        if (ChessRules.IsUnderCheck(sim, piece.IsRed))
                        {
                            // Nước đi làm Tướng phe mình bị chiếu -> BỊ CHẶN (Chấm đỏ)
                            _blockedMoves.Add(new Point(c, r));
                        }
                        else
                        {
                            // Nước đi hợp lệ (Chấm xanh hoặc Vòng đỏ ăn quân)
                            _validMoves.Add(new Point(c, r));
                        }
                    }
                }
            }
        }

        private void DrawPiece(Graphics g, string key, int col, int row)
        {
            if (!_pieces.ContainsKey(key)) return;
            int size = Cell - 6;
            int cx = StartX + col * Cell;
            int cy = StartY + row * Cell;
            g.DrawImage(_pieces[key], cx - size / 2, cy - size / 2 + 2, size, size);
        }

        private void DrawMark(Graphics g, Pen pen, int col, int row)
        {
            int x = StartX + col * Cell;
            int y = StartY + row * Cell;
            int arm = Cell / 6;
            int offset = Cell / 8;
            if (col > 0 && row > 0) { g.DrawLine(pen, x - offset, y - offset, x - offset - arm, y - offset); g.DrawLine(pen, x - offset, y - offset, x - offset, y - offset - arm); }
            if (col < 8 && row > 0) { g.DrawLine(pen, x + offset, y - offset, x + offset + arm, y - offset); g.DrawLine(pen, x + offset, y - offset, x + offset, y - offset - arm); }
            if (col > 0 && row < 9) { g.DrawLine(pen, x - offset, y + offset, x - offset - arm, y + offset); g.DrawLine(pen, x - offset, y + offset, x - offset, y + offset + arm); }
            if (col < 8 && row < 9) { g.DrawLine(pen, x + offset, y + offset, x + offset + arm, y + offset); g.DrawLine(pen, x + offset, y + offset, x + offset, y + offset - arm); }
        }

        private void LoadImages()
        {
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string? foundDir = null;
            for (int i = 0; i < 7; i++)
            {
                string testPath = Path.Combine(currentDir, "Extra", "images");
                if (Directory.Exists(testPath))
                {
                    foundDir = testPath;
                    break;
                }
                var parent = Directory.GetParent(currentDir);
                if (parent == null) break;
                currentDir = parent.FullName;
            }
            string basePath = foundDir ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Extra", "images");

            var files = new Dictionary<string, string>
            {
                { "vua_do", "Vuado.png" }, { "tuong_do", "Tuongdo1.png" },
                { "xe_do", "Xedo1.png" }, { "ma_do", "Mado1.png" },
                { "phao_do", "Phaodo1.png" }, { "si_do", "Sido1.png" }, { "tot_do", "Totdo1.png" },
                { "vua_den", "Vuaden.png" }, { "tuong_den", "Tuongden1.png" },
                { "xe_den", "Xeden1.png" }, { "ma_den", "Maden1.png" },
                { "phao_den", "Phaoden1.png" }, { "si_den", "Siden1.png" }, { "tot_den", "Totden1.png" },
            };

            foreach (var item in files)
            {
                string fullPath = Path.Combine(basePath, item.Value);
                if (File.Exists(fullPath))
                    _pieces[item.Key] = Image.FromFile(fullPath);
                else
                    Console.WriteLine($"Không tìm thấy ảnh: {fullPath}");
            }
        }

        private void FormGame_Load(object? sender, EventArgs e)
        {
            _gameTimer.CountdownChanged += (seconds) =>
            {
                if (this.IsDisposed) return;
                this.Invoke((Action)(() =>
                {
                    if (_isRedTurn)
                        lblTimerRed.Text = $"🔴 Đỏ: {seconds}s";
                    else
                        lblTimerBlack.Text = $"⚫ Đen: {seconds}s";
                }));
            };

            _gameTimer.CountdownEnd += () =>
            {
                if (this.IsDisposed) return;
                this.Invoke((Action)(() =>
                {
                    string msg = _isRedTurn ? "🔴 Đỏ hết giờ! Đen thắng!" : "⚫ Đen hết giờ! Đỏ thắng!";
                    MessageBox.Show(msg, "Hết Giờ");
                    ShowGameResult(!_isRedTurn);
                }));
            };

            _chatBox.OnChatMessage += (msg) =>
            {
                if (_isOnline && _connectionGuard?.Listener != null)
                    _ = _connectionGuard.Listener.SendChatAsync(msg);
            };

            _chatBox.OnCommand += (cmd, arg) =>
            {
                switch (cmd)
                {
                    case "exit":
                        Application.Exit();
                        break;
                    case "surrender":
                        _chatBox.AppendLog("[Hệ thống] Bạn đã đầu hàng!", Color.OrangeRed);
                        if (_isOnline && _connectionGuard?.Listener != null)
                            _ = _connectionGuard.Listener.SendSurrenderAsync();
                        ShowGameResult(false);
                        break;
                    case "restart":
                        if (_isOnline)
                        {
                            _chatBox.AppendLog("[Hệ thống] Không thể restart khi đang chơi Online!", Color.OrangeRed);
                            break;
                        }
                        ResetGame();
                        _chatBox.AppendLog("[Hệ thống] Đã reset game!", Color.DarkGreen);
                        break;
                }
            };

            RefreshStatus();
            _gameTimer.StartGame();
        }

        private void UpdateTimerLabelsAfterTurnChange()
        {
            if (_isRedTurn)
            {
                lblTimerBlack.Text = $"⚫ Đen: {GameTimer.TurnDurationSeconds}s";
                lblTimerRed.Text = $"🔴 Đỏ: {GameTimer.TurnDurationSeconds}s";
            }
            else
            {
                lblTimerRed.Text = $"🔴 Đỏ: {GameTimer.TurnDurationSeconds}s";
                lblTimerBlack.Text = $"⚫ Đen: {GameTimer.TurnDurationSeconds}s";
            }
        }

        private void SetStatus(string text, Color color)
        {
            txtStatus.Text = text;
            txtStatus.ForeColor = color;
        }

        private void RefreshStatus()
        {
            bool underCheck = ChessRules.IsUnderCheck(_board, _isRedTurn);
            string checkNotice = underCheck ? " ⚠️ [BỊ CHIẾU TƯỚNG!]" : "";

            if (_isOnline)
            {
                bool myTurn = _isRedTurn == _isPlayerRed;
                SetStatus(myTurn ? $"✅ Đến lượt bạn{checkNotice}" : "⏳ Đang chờ đối thủ...",
                          myTurn ? (underCheck ? Color.Red : Color.SeaGreen) : Color.DimGray);
            }
            else
            {
                SetStatus(_isRedTurn ? $"🔴 Lượt Đỏ{checkNotice}" : $"⚫ Lượt Đen{checkNotice}",
                          underCheck ? Color.Red : (_isRedTurn ? Color.DarkRed : Color.Black));
            }
        }

        private async void btnThachDau_Click(object? sender, EventArgs e)
        {
            if (_isOnline)
            {
                MessageBox.Show("Bạn đã kết nối rồi!", "Thông báo");
                return;
            }
            string? serverIp = PromptServerIp();
            if (string.IsNullOrEmpty(serverIp)) return;
            btnThachDau.Enabled = false;
            btnThachDau.Text = "Đang nối...";
            try
            {
                await ConnectToServer(serverIp, 54000);
                btnThachDau.Text = "Đã kết nối";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể kết nối: {ex.Message}", "Lỗi");
                btnThachDau.Enabled = true;
                btnThachDau.Text = "Thách Đấu";
            }
        }

        private void btnReplay_Click(object? sender, EventArgs e)
        {
            if (_isOnline)
            {
                MessageBox.Show("Trận đấu online đang diễn ra. Nếu muốn nhận thua, hãy gõ /surrender!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ResetGame();
        }

        private void btnThoat_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ShowGameResult(bool isWin)
        {
            _gameTimer.StopAll();
            FormResult f = new FormResult(isWin);
            f.ShowDialog();
            if (f.IsReplay && !_isOnline) ResetGame();
        }

        private void ResetGame()
        {
            _board = new Board();
            _board.Init();
            _selectedCell = null;
            _validMoves.Clear();
            _blockedMoves.Clear();
            _lastFrom = null;
            _lastTo = null;
            _isRedTurn = true;

            _gameTimer.StopAll();
            _gameTimer.StartGame();

            lblTimerRed.Text = $"🔴 Đỏ: {GameTimer.TurnDurationSeconds}s";
            lblTimerBlack.Text = $"⚫ Đen: {GameTimer.TurnDurationSeconds}s";
            RefreshStatus();

            this.Invalidate();
        }

        private async Task ConnectToServer(string serverIp, int port)
        {
            _connectionGuard = new ConnectionGuard(this);

            _connectionGuard.StateChanged += (state) =>
            {
                if (this.IsDisposed) return;
                this.Invoke((Action)(() =>
                {
                    _chatBox.AppendLog($"[Kết nối] {state}", Color.Gray);
                }));
            };

            await _connectionGuard.ConnectAsync(serverIp, port);
            WireUpGameEvents();
            _chatBox.AppendLog("[Kết nối] Đã kết nối! Đang chờ đối thủ...", Color.DarkGreen);
            SetStatus("⏳ Đang chờ đối thủ...", Color.DimGray);
        }

        private void WireUpGameEvents()
        {
            var listener = _connectionGuard?.Listener;
            if (listener == null) return;

            listener.OnGameStart += (isRed) =>
            {
                if (this.IsDisposed) return;
                this.Invoke((Action)(() =>
                {
                    _isPlayerRed = isRed;
                    _isOnline = true;
                    _isRedTurn = true;

                    _board = new Board();
                    _board.Init();
                    _selectedCell = null;
                    _validMoves.Clear();
                    _blockedMoves.Clear();
                    _lastFrom = null;
                    _lastTo = null;

                    string color = isRed ? "Đỏ (đi trước)" : "Đen (đi sau)";
                    _chatBox.AppendLog($"[Game] Bạn chơi quân {color}!", Color.DarkGreen);
                    RefreshStatus();

                    _gameTimer.StopAll();
                    _gameTimer.StartGame();
                    UpdateTimerLabelsAfterTurnChange();
                    this.Invalidate();
                }));
            };

            listener.OnOpponentMove += (fromRow, fromCol, toRow, toCol) =>
            {
                if (this.IsDisposed) return;
                this.Invoke((Action)(() =>
                {
                    HandleOpponentMove(fromRow, fromCol, toRow, toCol);
                }));
            };

            listener.OnChatReceived += (msg) =>
            {
                if (this.IsDisposed) return;
                this.Invoke((Action)(() =>
                {
                    if (msg.StartsWith("[Hệ thống]") || msg.StartsWith("[Server]"))
                        _chatBox.AppendLog(msg, Color.OrangeRed);
                    else
                        _chatBox.AppendOpponentMessage(msg);
                }));
            };

            listener.OnOpponentSurrender += () =>
            {
                if (this.IsDisposed) return;
                this.Invoke((Action)(() =>
                {
                    _chatBox.AppendLog("[Game] Đối thủ đầu hàng! Bạn thắng!", Color.DarkGreen);
                    ShowGameResult(true);
                }));
            };

            listener.OnGameEnd += (reason) =>
            {
                if (this.IsDisposed) return;
                this.Invoke((Action)(() =>
                {
                    _chatBox.AppendLog($"[Game] {reason}", Color.OrangeRed);
                    _isOnline = false;
                    btnThachDau.Enabled = true;
                    btnThachDau.Text = "Thách Đấu";
                    RefreshStatus();
                }));
            };
        }

        private void HandleOpponentMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            Piece? captured = _board.grid[toRow, toCol];

            bool moved = MoveValidator.TryMove(_board, fromRow, fromCol, toRow, toCol, !_isPlayerRed);
            if (!moved) return;

            _lastFrom = new Point(fromCol, fromRow);
            _lastTo = new Point(toCol, toRow);

            if (captured != null)
                SoundManager.PlayCapture();
            else
                SoundManager.PlayMove();

            _isRedTurn = !_isRedTurn;
            _gameTimer.StopCountdown();
            _gameTimer.StartTurn();
            UpdateTimerLabelsAfterTurnChange();

            RefreshStatus();

            _selectedCell = null;
            _validMoves.Clear();
            _blockedMoves.Clear();

            if (ChessRules.IsCheckmate(_board, _isRedTurn))
                ShowGameResult(false);

            this.Invalidate();
        }

        private static string? PromptServerIp()
        {
            using var dialog = new Form();
            dialog.Text = "Kết nối Server";
            dialog.Size = new Size(320, 160);
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;

            var label = new Label { Text = "Nhập IP server:", Location = new Point(15, 15), AutoSize = true };
            var textBox = new TextBox { Text = "127.0.0.1", Location = new Point(15, 40), Size = new Size(270, 25) };
            var btnOk = new Button
            {
                Text = "Kết nối",
                DialogResult = DialogResult.OK,
                Location = new Point(110, 75),
                Size = new Size(90, 32)
            };

            dialog.Controls.AddRange(new Control[] { label, textBox, btnOk });
            dialog.AcceptButton = btnOk;

            return dialog.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : null;
        }
    }
}
