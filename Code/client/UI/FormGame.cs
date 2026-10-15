using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoTuongOnline.Common;
using CoTuongOnline.Logic;
using CoTuongOnline.Final;
using CoTuongOnline.Network;

namespace CoTuongOnline.Client
{
    public partial class Form1 : Form
    {
        private const int Cell = 60;
        private const int StartX = 30;
        private const int StartY = 30;

        private Board _board = new Board();
        private Point? _selectedCell = null;
        private List<Point> _validMoves = new List<Point>();
        private bool _isRedTurn = true;

        // === NETWORK ===
        private ConnectionGuard? _connectionGuard;
        private bool _isOnline = false;
        private bool _isPlayerRed = true;

        private GameTimer _gameTimer = new GameTimer();
        private Label lblTimerRed = null!;
        private Label lblTimerBlack = null!;
        private TextBox txtStatus = null!;
        private ChatBox _chatBox = null!;

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

        public Form1()
        {
            InitializeComponent();
            this.ClientSize = new Size(StartX * 2 + 8 * Cell + 200, StartY * 2 + 9 * Cell);
            this.Text = "Cờ Tướng Online";
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Paint += Form1_Paint;
            this.Load += Form1_Load;
            this.MouseClick += Form1_MouseClick;
            LoadImages();
            _board.Init();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
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

            if (_selectedCell.HasValue)
            {
                int hx = StartX + _selectedCell.Value.X * Cell - Cell / 2;
                int hy = StartY + _selectedCell.Value.Y * Cell - Cell / 2;
                g.FillRectangle(new SolidBrush(Color.FromArgb(120, 255, 215, 0)), hx, hy, Cell, Cell);
            }

            foreach (var p in _validMoves)
            {
                int cx = StartX + p.X * Cell;
                int cy = StartY + p.Y * Cell;
                g.FillEllipse(new SolidBrush(Color.FromArgb(160, 0, 180, 0)), cx - 10, cy - 10, 20, 20);
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
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
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
                _validMoves = GetValidMovesFor(row, col);
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
                this.Invalidate();
                return;
            }

            if (clickedPiece != null && clickedPiece.IsRed == _isRedTurn)
            {
                _selectedCell = new Point(col, row);
                _validMoves = GetValidMovesFor(row, col);
                this.Invalidate();
                return;
            }

            bool moved = FinalLogic.TryMove(_board, fromRow, fromCol, row, col, _isRedTurn);
            if (moved)
            {
                if (clickedPiece != null)
                    SoundManager.PlayCapture();
                else
                    SoundManager.PlayMove();

                if (_isOnline && _connectionGuard?.Listener != null)
                    _ = _connectionGuard.Listener.SendMoveAsync(fromRow, fromCol, row, col);

                _isRedTurn = !_isRedTurn;
                _gameTimer.StopCountdown();
                _gameTimer.StartTurn();

                txtStatus.Text = _isRedTurn ? "🔴 Lượt Đỏ" : "⚫ Lượt Đen";
                txtStatus.ForeColor = _isRedTurn ? Color.DarkRed : Color.Black;

                _selectedCell = null;
                _validMoves.Clear();

                if (ChessRules.IsCheckmate(_board, _isRedTurn))
                    ShowGameResult(!_isRedTurn);
            }
            else
            {
                _selectedCell = null;
                _validMoves.Clear();
            }

            this.Invalidate();
        }

        private List<Point> GetValidMovesFor(int row, int col)
        {
            var moves = new List<Point>();
            Piece? piece = _board.grid[row, col];
            if (piece == null) return moves;

            for (int r = 0; r < 10; r++)
                for (int c = 0; c < 9; c++)
                    if (FinalLogic.TryMove(CloneBoard(_board), row, col, r, c, piece.IsRed))
                        moves.Add(new Point(c, r));

            return moves;
        }

        private Board CloneBoard(Board src)
        {
            var clone = new Board();
            for (int r = 0; r < 10; r++)
                for (int c = 0; c < 9; c++)
                {
                    var p = src.grid[r, c];
                    if (p != null)
                        clone.grid[r, c] = new Piece(p.Type, p.IsRed, p.Row, p.Col);
                }
            return clone;
        }

        private void DrawPiece(Graphics g, string key, int col, int row)
        {
            if (!_pieces.ContainsKey(key)) return;
            int size = Cell - 6;
            int cx = StartX + col * Cell;
            int cy = StartY + row * Cell;
            g.DrawImage(_pieces[key], cx - size / 2, cy - size / 2, size, size);
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
            if (col < 8 && row < 9) { g.DrawLine(pen, x + offset, y + offset, x + offset + arm, y + offset); g.DrawLine(pen, x + offset, y + offset, x + offset, y + offset + arm); }
        }

        private void LoadImages()
        {
            string basePath = Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "assets", "images"));

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

        private void Form1_Load(object sender, EventArgs e)
        {
            int btnX = StartX * 2 + 8 * Cell + 10;

            txtStatus = new TextBox
            {
                Text = "🔴 Lượt Đỏ",
                Location = new Point(btnX, 10),
                Size = new Size(160, 40),
                Font = new Font("Arial", 13, FontStyle.Bold),
                ReadOnly = true,
                TextAlign = HorizontalAlignment.Center,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = Color.DarkRed
            };
            txtStatus.TabStop = false;
            this.Controls.Add(txtStatus);

            lblTimerRed = new Label
            {
                Text = "🔴 Đỏ: 30s",
                Location = new Point(btnX, 60),
                Size = new Size(160, 30),
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.DarkRed,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTimerRed);

            lblTimerBlack = new Label
            {
                Text = "⚫ Đen: 30s",
                Location = new Point(btnX, 95),
                Size = new Size(160, 30),
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTimerBlack);

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

            Button btnThachDau = new Button
            {
                Text = "Thách Đấu",
                Size = new Size(160, 45),
                Location = new Point(btnX, 140),
                Font = new Font("Arial", 11, FontStyle.Bold),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnThachDau.Click += async (s, ev) =>
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
            };
            this.Controls.Add(btnThachDau);

            Button btnReplay = new Button
            {
                Text = "Chơi Lại",
                Size = new Size(160, 45),
                Location = new Point(btnX, 195),
                Font = new Font("Arial", 11, FontStyle.Bold),
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnReplay.Click += (s, ev) => ResetGame();
            this.Controls.Add(btnReplay);

            Button btnThoat = new Button
            {
                Text = "Thoát",
                Size = new Size(160, 45),
                Location = new Point(btnX, 250),
                Font = new Font("Arial", 11, FontStyle.Bold),
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnThoat.Click += (s, ev) => Application.Exit();
            this.Controls.Add(btnThoat);

            _chatBox = new ChatBox
            {
                Location = new Point(btnX, 310),
                Size = new Size(160, 220)
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
                        ShowGameResult(false);
                        break;
                    case "restart":
                        ResetGame();
                        _chatBox.AppendLog("[Hệ thống] Đã reset game!", Color.DarkGreen);
                        break;
                }
            };

            this.Controls.Add(_chatBox);
            _gameTimer.StartGame();
        }

        private void ShowGameResult(bool isWin)
        {
            _gameTimer.StopAll();
            FormResult f = new FormResult(isWin);
            f.ShowDialog();
            if (f.IsReplay) ResetGame();
        }

        private void ResetGame()
        {
            _board = new Board();
            _board.Init();
            _selectedCell = null;
            _validMoves.Clear();
            _isRedTurn = true;

            _gameTimer.StopAll();
            _gameTimer.StartGame();

            lblTimerRed.Text = "🔴 Đỏ: 30s";
            lblTimerBlack.Text = "⚫ Đen: 30s";
            txtStatus.Text = "🔴 Lượt Đỏ";
            txtStatus.ForeColor = Color.DarkRed;

            this.Invalidate();
        }

        private async Task ConnectToServer(string serverIp, int port)
        {
            _connectionGuard = new ConnectionGuard(this);

            _connectionGuard.StateChanged += (state) =>
            {
                _chatBox.AppendLog($"[Kết nối] {state}", Color.Gray);
            };

            await _connectionGuard.ConnectAsync(serverIp, port);
            WireUpGameEvents();
            _chatBox.AppendLog("[Kết nối] Đã kết nối! Đang chờ đối thủ...", Color.DarkGreen);
        }

        private void WireUpGameEvents()
        {
            var listener = _connectionGuard?.Listener;
            if (listener == null) return;

            listener.OnGameStart += (isRed) =>
            {
                _isPlayerRed = isRed;
                _isOnline = true;
                _isRedTurn = true;

                _board = new Board();
                _board.Init();
                _selectedCell = null;
                _validMoves.Clear();

                string color = isRed ? "Đỏ (đi trước)" : "Đen (đi sau)";
                _chatBox.AppendLog($"[Game] Bạn chơi quân {color}!", Color.DarkGreen);
                txtStatus.Text = "🔴 Lượt Đỏ";
                txtStatus.ForeColor = Color.DarkRed;

                _gameTimer.StopAll();
                _gameTimer.StartGame();
                this.Invalidate();
            };

            listener.OnOpponentMove += HandleOpponentMove;

            listener.OnChatReceived += (msg) =>
            {
                _chatBox.AppendOpponentMessage(msg);
            };

            listener.OnOpponentSurrender += () =>
            {
                _chatBox.AppendLog("[Game] Đối thủ đầu hàng! Bạn thắng!", Color.DarkGreen);
                ShowGameResult(true);
            };

            listener.OnGameEnd += (reason) =>
            {
                _chatBox.AppendLog($"[Game] {reason}", Color.OrangeRed);
                _isOnline = false;
            };
        }

        private void HandleOpponentMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            Piece? captured = _board.grid[toRow, toCol];

            bool moved = FinalLogic.TryMove(_board, fromRow, fromCol, toRow, toCol, !_isPlayerRed);
            if (!moved) return;

            if (captured != null)
                SoundManager.PlayCapture();
            else
                SoundManager.PlayMove();

            _isRedTurn = !_isRedTurn;
            _gameTimer.StopCountdown();
            _gameTimer.StartTurn();

            txtStatus.Text = _isRedTurn ? "🔴 Lượt Đỏ" : "⚫ Lượt Đen";
            txtStatus.ForeColor = _isRedTurn ? Color.DarkRed : Color.Black;

            _selectedCell = null;
            _validMoves.Clear();

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