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
        private bool _flipBoard = false;
        private bool _hasOfferedDraw = false;
        private bool _isProcessingMove = false;

        // Lưu trữ lịch sử toàn bộ các nước đi để xem lại (Replay)
        private readonly List<ReplayStep> _replaySteps = new List<ReplayStep>();

        private static Piece?[,] CloneGrid(Piece?[,] source)
        {
            var target = new Piece?[10, 9];
            for (int r = 0; r < 10; r++)
                for (int c = 0; c < 9; c++)
                {
                    var p = source[r, c];
                    if (p != null)
                        target[r, c] = new Piece(p.Type, p.IsRed, p.Row, p.Col);
                }
            return target;
        }

        private int ToScreenRow(int row) => BoardRenderer.ToScreenRow(row, _flipBoard);
        private int ToScreenCol(int col) => BoardRenderer.ToScreenCol(col, _flipBoard);
        private int ToBoardRow(int screenRow) => BoardRenderer.ToBoardRow(screenRow, _flipBoard);
        private int ToBoardCol(int screenCol) => BoardRenderer.ToBoardCol(screenCol, _flipBoard);

        private static string GetPieceName(PieceType type) => type switch
        {
            PieceType.General => "Tướng",
            PieceType.Advisor => "Sĩ",
            PieceType.Elephant => "Tượng",
            PieceType.Horse => "Mã",
            PieceType.Rook => "Xe",
            PieceType.Cannon => "Pháo",
            PieceType.Soldier => "Tốt",
            _ => "Quân"
        };

        // === NETWORK ===
        private ClientEventListener? _clientListener;
        private bool _isOnline = false;
        private bool _isPlayerRed = true;
        private string _myUsername = "Tôi";
        private readonly bool _autoConnectOnline;
        private string _lastServerIp = "127.0.0.1";
        private int _lastPort = 54000;

        private GameTimer _gameTimer = new GameTimer();

        private readonly Dictionary<string, Image> _pieces = new Dictionary<string, Image>();

        // Cache các Pen / Brush / Font đặc thù của FormGame (các resource bàn cờ dùng chung nằm ở BoardRenderer)
        private static readonly Brush _checkBgBrush = new SolidBrush(Color.FromArgb(190, 255, 60, 60));
        private static readonly Pen _checkBorderPen = new Pen(Color.Red, 3.5f);
        private static readonly Brush _selBrush = new SolidBrush(Color.FromArgb(150, 255, 215, 0));
        private static readonly Pen _selPen = new Pen(Color.Goldenrod, 3);
        private static readonly Brush _dotBrush = new SolidBrush(Color.FromArgb(160, 0, 180, 0));
        private static readonly Brush _blockedDotBrush = new SolidBrush(Color.FromArgb(230, 220, 20, 20));
        private static readonly Pen _blockedBorderPen = new Pen(Color.DarkRed, 1.5f);
        private static readonly Pen _generalCirclePen = new Pen(Color.Red, 3f);
        private static readonly Pen _ringPen = new Pen(Color.FromArgb(230, 220, 30, 30), 3);
        private static readonly Pen _blockedRingPen = new Pen(Color.FromArgb(180, 160, 0, 0), 2.5f);
        private static readonly Pen _blockedCrossPen = new Pen(Color.Red, 2.5f);

        public FormGame(bool autoConnectOnline = false)
        {
            _autoConnectOnline = autoConnectOnline;
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

        private void SafeInvoke(Action action)
        {
            try
            {
                if (this.IsDisposed || this.Disposing || !this.IsHandleCreated) return;
                this.Invoke(action);
            }
            catch
            {
                // Bỏ qua lỗi giao diện khi Form đang đóng
            }
        }

        private void FormGame_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 1. Vẽ khung bàn cờ bằng renderer dùng chung
            BoardRenderer.DrawBoard(g, StartX, StartY, Cell, _flipBoard);

            // 2. Highlight nước đi vừa đi (ô xuất phát: điểm chấm nhỏ; ô đích đến: vầng sáng + khung 4 góc)
            if (_lastFrom.HasValue)
            {
                int sc = ToScreenCol(_lastFrom.Value.X);
                int sr = ToScreenRow(_lastFrom.Value.Y);
                BoardRenderer.DrawLastMoveHighlight(g, StartX, StartY, Cell, sc, sr, isDestination: false);
            }
            if (_lastTo.HasValue)
            {
                int sc = ToScreenCol(_lastTo.Value.X);
                int sr = ToScreenRow(_lastTo.Value.Y);
                BoardRenderer.DrawLastMoveHighlight(g, StartX, StartY, Cell, sc, sr, isDestination: true);
            }

            // Kiểm tra Tướng bị chiếu 1 lần duy nhất cho mỗi bên để tái sử dụng, tối ưu hiệu năng
            Point? redGenPos = ChessRules.IsUnderCheck(_board, true) ? ChessRules.FindGeneral(_board, true) : null;
            Point? blackGenPos = ChessRules.IsUnderCheck(_board, false) ? ChessRules.FindGeneral(_board, false) : null;

            // Chiếu tướng: Hiện nền tròn đỏ bao quanh quân Tướng bị chiếu
            foreach (var genPos in new[] { redGenPos, blackGenPos })
            {
                if (genPos.HasValue)
                {
                    int sc = ToScreenCol(genPos.Value.X);
                    int sr = ToScreenRow(genPos.Value.Y);
                    int cx = StartX + sc * Cell;
                    int cy = StartY + sr * Cell + BoardRenderer.GetPieceVisualCenterOffsetY(Cell);
                    int size = BoardRenderer.GetPieceCircleSize(Cell);
                    int left = cx - size / 2;
                    int top = cy - size / 2;
                    g.FillEllipse(_checkBgBrush, left, top, size, size);
                    g.DrawEllipse(_checkBorderPen, left, top, size, size);
                }
            }

            if (_selectedCell.HasValue)
            {
                int sc = ToScreenCol(_selectedCell.Value.X);
                int sr = ToScreenRow(_selectedCell.Value.Y);
                int cx = StartX + sc * Cell;
                int cy = StartY + sr * Cell + BoardRenderer.GetPieceVisualCenterOffsetY(Cell);
                int size = BoardRenderer.GetPieceCircleSize(Cell);
                int left = cx - size / 2;
                int top = cy - size / 2;
                g.FillEllipse(_selBrush, left, top, size, size);
                g.DrawEllipse(_selPen, left, top, size, size);
            }

            // Nước đi hợp lệ: chấm màu xanh lá
            foreach (var p in _validMoves)
            {
                if (_board.grid[p.Y, p.X] != null) continue;
                int cx = StartX + ToScreenCol(p.X) * Cell;
                int cy = StartY + ToScreenRow(p.Y) * Cell;
                g.FillEllipse(_dotBrush, cx - 10, cy - 10, 20, 20);
            }

            // Nước đi làm Tướng phe mình bị chiếu (bị chặn): chấm màu đỏ
            foreach (var p in _blockedMoves)
            {
                if (_board.grid[p.Y, p.X] != null) continue;
                int cx = StartX + ToScreenCol(p.X) * Cell;
                int cy = StartY + ToScreenRow(p.Y) * Cell;
                g.FillEllipse(_blockedDotBrush, cx - 10, cy - 10, 20, 20);
                g.DrawEllipse(_blockedBorderPen, cx - 10, cy - 10, 20, 20);
            }

            for (int row = 0; row < 10; row++)
                for (int col = 0; col < 9; col++)
                {
                    Piece? p = _board.grid[row, col];
                    if (p == null) continue;
                    string key = BoardRenderer.GetPieceKey(p.Type, p.IsRed);
                    if (_pieces.TryGetValue(key, out var img))
                    {
                        BoardRenderer.DrawPiece(g, img, StartX, StartY, Cell, ToScreenCol(col), ToScreenRow(row));
                    }
                }

            // Khung 4 góc nổi bật ôm trên quân cờ vừa đi và quân cờ đang chọn
            if (_lastTo.HasValue)
            {
                int sc = ToScreenCol(_lastTo.Value.X);
                int sr = ToScreenRow(_lastTo.Value.Y);
                int cx = StartX + sc * Cell;
                int cy = StartY + sr * Cell;
                BoardRenderer.DrawCornerBrackets(g, BoardRenderer.LastMovePen, cx, cy, Cell - 2, 10);
            }
            if (_selectedCell.HasValue)
            {
                int sc = ToScreenCol(_selectedCell.Value.X);
                int sr = ToScreenRow(_selectedCell.Value.Y);
                int cx = StartX + sc * Cell;
                int cy = StartY + sr * Cell;
                BoardRenderer.DrawCornerBrackets(g, _selPen, cx, cy, Cell - 2, 10);
            }

            // Viền đỏ hình tròn bao quanh Tướng bị chiếu
            foreach (var genPos in new[] { redGenPos, blackGenPos })
            {
                if (genPos.HasValue)
                {
                    int cx = StartX + ToScreenCol(genPos.Value.X) * Cell;
                    int cy = StartY + ToScreenRow(genPos.Value.Y) * Cell + BoardRenderer.GetPieceVisualCenterOffsetY(Cell);
                    int size = BoardRenderer.GetPieceCircleSize(Cell);
                    g.DrawEllipse(_generalCirclePen, cx - size / 2, cy - size / 2, size, size);
                }
            }

            // Vòng tròn đỏ cho các nước ăn quân hợp lệ
            foreach (var p in _validMoves)
            {
                if (_board.grid[p.Y, p.X] == null) continue;
                int cx = StartX + ToScreenCol(p.X) * Cell;
                int cy = StartY + ToScreenRow(p.Y) * Cell + BoardRenderer.GetPieceVisualCenterOffsetY(Cell);
                int size = BoardRenderer.GetPieceCircleSize(Cell);
                g.DrawEllipse(_ringPen, cx - size / 2, cy - size / 2, size, size);
            }

            // Nước ăn quân nhưng bị chặn: vẽ vòng tròn và dấu chéo
            foreach (var p in _blockedMoves)
            {
                if (_board.grid[p.Y, p.X] == null) continue;
                int cx = StartX + ToScreenCol(p.X) * Cell;
                int cy = StartY + ToScreenRow(p.Y) * Cell + BoardRenderer.GetPieceVisualCenterOffsetY(Cell);
                int size = BoardRenderer.GetPieceCircleSize(Cell);
                g.DrawEllipse(_blockedRingPen, cx - size / 2, cy - size / 2, size, size);
                g.DrawLine(_blockedCrossPen, cx - 10, cy - 10, cx + 10, cy + 10);
                g.DrawLine(_blockedCrossPen, cx - 10, cy + 10, cx + 10, cy - 10);
            }
        }

        private async void FormGame_MouseClick(object? sender, MouseEventArgs e)
        {
            if (_isProcessingMove) return;

            int screenCol = (e.X - StartX + Cell / 2) / Cell;
            int screenRow = (e.Y - StartY + Cell / 2) / Cell;
            if (screenCol < 0 || screenCol > 8 || screenRow < 0 || screenRow > 9) return;

            int col = ToBoardCol(screenCol);
            int row = ToBoardRow(screenRow);

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

            Point targetPoint = new Point(col, row);
            if (_blockedMoves.Contains(targetPoint))
            {
                _chatBox.AppendLog("[Hệ thống] Nước đi này bị chặn vì làm Tướng bị chiếu!", Color.OrangeRed);
                return;
            }

            bool moved = MoveValidator.TryMove(_board, fromRow, fromCol, row, col, _isRedTurn);
            if (moved)
            {
                _isProcessingMove = true;
                _lastFrom = new Point(fromCol, fromRow);
                _lastTo = new Point(col, row);
                if (clickedPiece != null)
                    SoundManager.PlayCapture();
                else
                    SoundManager.PlayMove();

                string moveDesc = FormatMoveDescription(_board.grid[row, col], fromRow, fromCol, row, col, clickedPiece);
                _replaySteps.Add(new ReplayStep(CloneGrid(_board.grid), _lastFrom, _lastTo, moveDesc));

                // Cập nhật lượt đi và xóa vùng chọn ngay lập tức để chống click trùng
                _selectedCell = null;
                _validMoves.Clear();
                _blockedMoves.Clear();

                if (!_isOnline && _gameTimer.State == TimerState.Stopped)
                {
                    _gameTimer.StartGame();
                }

                _isRedTurn = !_isRedTurn;
                _gameTimer.StopCountdown();
                _gameTimer.StartTurn();
                UpdateTimerLabelsAfterTurnChange();

                RefreshStatus();
                this.Invalidate();

                // Kiểm tra thế cờ lặp lại 3 lần
                CheckThreefoldRepetition();

                // Gửi nước đi sang Server (nếu chơi Online)
                if (_isOnline && _clientListener != null)
                {
                    try
                    {
                        await _clientListener.SendMoveAsync(fromRow, fromCol, row, col);
                    }
                    catch (Exception ex)
                    {
                        _chatBox.AppendLog($"[Mạng] Lỗi gửi nước đi: {ex.Message}", Color.Red);
                    }
                    finally
                    {
                        _isProcessingMove = false;
                    }
                }
                else
                {
                    _isProcessingMove = false;
                }

                if (ChessRules.IsCheckmate(_board, _isRedTurn))
                {
                    if (_isOnline)
                    {
                        _chatBox.AppendLog("[Game] Chiếu bí! Bạn đã chiến thắng!", Color.DarkGreen);
                        if (_clientListener != null)
                        {
                            try
                            {
                                await _clientListener.SendGameEndAsync($"Chiếu bí! {_myUsername} đã giành chiến thắng!");
                            }
                            catch { }
                        }
                        ShowGameResult(true, "🎉 Chiếu bí! Bạn đã chiến thắng!");
                    }
                    else
                    {
                        string winnerMsg = !_isRedTurn ? "🎉 Chiếu bí! Bên Đỏ thắng cuộc!" : "🎉 Chiếu bí! Bên Đen thắng cuộc!";
                        _chatBox.AppendLog($"[Game] {winnerMsg}", Color.DarkGreen);
                        ShowGameResult(!_isRedTurn, winnerMsg);
                    }
                }
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
                        Piece? originalTarget = _board.grid[r, c];
                        _board.grid[r, c] = piece;
                        _board.grid[row, col] = null;
                        int oldR = piece.Row, oldC = piece.Col;
                        piece.Row = r; piece.Col = c;

                        bool underCheck = ChessRules.IsUnderCheck(_board, piece.IsRed);

                        // Hoàn tác
                        _board.grid[row, col] = piece;
                        _board.grid[r, c] = originalTarget;
                        piece.Row = oldR; piece.Col = oldC;

                        if (underCheck)
                        {
                            _blockedMoves.Add(new Point(c, r));
                        }
                        else
                        {
                            _validMoves.Add(new Point(c, r));
                        }
                    }
                }
            }
        }

        private void LoadImages()
        {
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Extra", "images");
            if (!Directory.Exists(basePath))
            {
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 5; i++)
                {
                    string testPath = Path.Combine(currentDir, "Extra", "images");
                    if (Directory.Exists(testPath))
                    {
                        basePath = testPath;
                        break;
                    }
                    var parent = Directory.GetParent(currentDir);
                    if (parent == null) break;
                    currentDir = parent.FullName;
                }
            }

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
                {
                    byte[] bytes = File.ReadAllBytes(fullPath);
                    using var ms = new MemoryStream(bytes);
                    _pieces[item.Key] = new Bitmap(ms);
                }
            }
        }

        private void FormGame_Load(object? sender, EventArgs e)
        {
            _gameTimer.CountdownChanged += (seconds) =>
            {
                SafeInvoke(() =>
                {
                    if (_isRedTurn)
                        lblTimerRed.Text = $"🔴 Đỏ: {seconds}s";
                    else
                        lblTimerBlack.Text = $"⚫ Đen: {seconds}s";
                });
            };

            _gameTimer.CountdownEnd += () =>
            {
                SafeInvoke(async () =>
                {
                    if (_isOnline)
                    {
                        if (_isRedTurn == _isPlayerRed)
                        {
                            _chatBox.AppendLog("[Hệ thống] Bạn đã hết thời gian suy nghĩ! Bạn thua!", Color.OrangeRed);
                            if (_clientListener != null)
                                _ = _clientListener.SendTimeoutAsync();

                            ShowGameResult(false, "💀 Hết giờ! Bạn đã thua!");
                        }
                        else
                        {
                            // Đợi thêm 5 giây dự phòng độ trễ mạng (Grace Period) trước khi xử lý đối thủ hết giờ
                            _chatBox.AppendLog("[Hệ thống] Đối thủ đã dùng hết 45s, đang đợi xử lý...", Color.Goldenrod);
                            await Task.Delay(5000);
                            if (_isOnline && _isRedTurn != _isPlayerRed)
                            {
                                _chatBox.AppendLog("[Hệ thống] Đối thủ quá thời gian suy nghĩ! Bạn giành chiến thắng!", Color.DarkGreen);
                                if (_clientListener != null)
                                    _ = _clientListener.SendGameEndAsync($"Hết giờ! {_myUsername} đã giành chiến thắng!");

                                ShowGameResult(true, "🎉 Đối thủ hết giờ! Bạn thắng!");
                            }
                        }
                    }
                    else
                    {
                        string msg = _isRedTurn ? "🎉 Đỏ hết giờ! Đen thắng cuộc!" : "🎉 Đen hết giờ! Đỏ thắng cuộc!";
                        MessageBox.Show(msg, "Hết Giờ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ShowGameResult(!_isRedTurn, msg);
                    }
                });
            };

            _chatBox.OnChatMessage += (msg) =>
            {
                if (_isOnline && _clientListener != null)
                    _ = _clientListener.SendChatAsync(msg);
            };

            _chatBox.OnCommand += (cmd, arg) =>
            {
                switch (cmd)
                {
                    case "exit":
                        Application.Exit();
                        break;
                    case "surrender":
                        if (_isOnline)
                        {
                            _chatBox.AppendLog("[Hệ thống] Bạn đã đầu hàng!", Color.OrangeRed);
                            if (_clientListener != null)
                                _ = _clientListener.SendSurrenderAsync();

                            ShowGameResult(false, "💀 Bạn đã đầu hàng!");
                        }
                        else
                        {
                            string surrenderMsg = _isRedTurn ? "🎉 Bên Đỏ đầu hàng! Bên Đen thắng!" : "🎉 Bên Đen đầu hàng! Bên Đỏ thắng!";
                            _chatBox.AppendLog($"[Hệ thống] {surrenderMsg}", Color.OrangeRed);
                            ShowGameResult(!_isRedTurn, surrenderMsg);
                        }
                        break;
                    case "draw":
                        btnXinHoa.PerformClick();
                        break;
                    case "flip":
                        _flipBoard = !_flipBoard;
                        this.Invalidate();
                        _chatBox.AppendLog($"[Hệ thống] Đã {(_flipBoard ? "lật" : "đặt lại")} góc nhìn bàn cờ.", Color.SteelBlue);
                        break;
                    case "restart":
                        if (_isOnline)
                        {
                            _chatBox.AppendLog("[Hệ thống] Không thể restart khi đang chơi Online! Hãy gõ /surrender nếu muốn nhận thua.", Color.OrangeRed);
                            break;
                        }
                        ResetGame();
                        _chatBox.AppendLog("[Hệ thống] Đã reset ván cờ mới!", Color.DarkGreen);
                        break;
                    case "replay":
                    case "review":
                        OpenReplayViewer();
                        break;
                    default:
                        _chatBox.AppendLog($"[Hệ thống] Lệnh '/{cmd}' không hợp lệ. Lệnh khả dụng: /replay, /draw, /flip, /surrender, /restart, /exit", Color.SteelBlue);
                        break;
                }
            };

            _replaySteps.Clear();
            _replaySteps.Add(new ReplayStep(CloneGrid(_board.grid), null, null, "Bàn cờ ban đầu"));

            lblTimerRed.Text = $"🔴 Đỏ: {GameTimer.TurnDurationSeconds}s";
            lblTimerBlack.Text = $"⚫ Đen: {GameTimer.TurnDurationSeconds}s";
            RefreshStatus();

            if (_autoConnectOnline)
            {
                this.BeginInvoke((Action)(() => btnThachDau.PerformClick()));
            }
        }

        private void UpdateTimerLabelsAfterTurnChange()
        {
            lblTimerRed.Text = $"🔴 Đỏ: {GameTimer.TurnDurationSeconds}s";
            lblTimerBlack.Text = $"⚫ Đen: {GameTimer.TurnDurationSeconds}s";
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
                MessageBox.Show("Bạn đang trong trận đấu online rồi!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var connInfo = PromptConnectionInfo();
            if (connInfo == null) return;

            btnThachDau.Enabled = false;
            btnThachDau.Text = "Đang nối...";

            try
            {
                _lastServerIp = connInfo.Value.serverIp;
                _lastPort = connInfo.Value.port;
                _myUsername = connInfo.Value.username;
                this.Text = $"Cờ Tướng Online - Người chơi: {_myUsername}";
                await ConnectToServer(_lastServerIp, _lastPort, _myUsername);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể kết nối máy chủ: {ex.Message}", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnThachDau.Enabled = true;
                btnThachDau.Text = "Thách Đấu";
            }
        }

        private void btnReplay_Click(object? sender, EventArgs e)
        {
            if (_isOnline)
            {
                var confirm = MessageBox.Show("Trận đấu online đang diễn ra. Bạn có muốn nhận thua để chơi ván mới không?", "Xác nhận đầu hàng", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm == DialogResult.Yes)
                {
                    if (_clientListener != null)
                        _ = _clientListener.SendSurrenderAsync();
                    ShowGameResult(false, "💀 Bạn đã đầu hàng!");
                }
                return;
            }

            ResetGame();
            _gameTimer.StartGame();
        }

        private void OpenReplayViewer()
        {
            if (_isOnline || (_gameTimer != null && _gameTimer.State == TimerState.Running))
            {
                MessageBox.Show(this, "Ván đấu đang diễn ra! Bạn chỉ có thể xem lại nước đi sau khi trận đấu kết thúc.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_replaySteps.Count <= 1)
            {
                MessageBox.Show(this, "Chưa có nước đi nào trong ván cờ để xem lại!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var formReplay = new FormReplay(_replaySteps, _pieces, _flipBoard);
            formReplay.ShowDialog(this);
        }

        private void btnThoat_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private async void btnXinHoa_Click(object? sender, EventArgs e)
        {
            if (_isOnline)
            {
                if (_hasOfferedDraw)
                {
                    _chatBox.AppendLog("[Hệ thống] Bạn đã gửi lời đề nghị hòa rồi, vui lòng đợi phản hồi!", Color.DarkGoldenrod);
                    return;
                }

                var confirm = MessageBox.Show(this, "Bạn có muốn gửi lời đề nghị HÒA cờ đến đối thủ không?", "Xác nhận xin hòa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes) return;

                _hasOfferedDraw = true;
                _chatBox.AppendLog("[Hệ thống] Đang gửi lời đề nghị hòa tới đối thủ...", Color.DarkGoldenrod);

                if (_clientListener != null)
                {
                    try
                    {
                        await _clientListener.SendDrawOfferAsync();
                    }
                    catch (Exception ex)
                    {
                        _chatBox.AppendLog($"[Mạng] Lỗi gửi đề nghị hòa: {ex.Message}", Color.Red);
                        _hasOfferedDraw = false;
                    }
                }
            }
            else
            {
                var confirm = MessageBox.Show(this, "Hai bên có đồng ý kết thúc ván cờ với kết quả HÒA không?", "Xác nhận hòa ván cờ", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm == DialogResult.Yes)
                {
                    _chatBox.AppendLog("[Game] Hai bên đã thống nhất HÒA cờ!", Color.DarkSlateBlue);
                    ShowGameResult(false, "🤝 Ván cờ kết thúc HÒA!");
                }
            }
        }

        private string FormatMoveDescription(Piece? movedPiece, int fromRow, int fromCol, int toRow, int toCol, Piece? capturedPiece)
        {
            if (movedPiece == null) return $"({fromRow},{fromCol}) ➜ ({toRow},{toCol})";

            string side = movedPiece.IsRed ? "🔴 Đỏ" : "⚫ Đen";
            string pieceName = GetPieceName(movedPiece.Type);
            string desc = $"[{side}] {pieceName} ({fromRow},{fromCol}) ➜ ({toRow},{toCol})";

            if (capturedPiece != null)
            {
                desc += $" ⚔️ Ăn {GetPieceName(capturedPiece.Type)}";
            }

            bool opponentUnderCheck = ChessRules.IsUnderCheck(_board, !movedPiece.IsRed);
            if (opponentUnderCheck)
            {
                desc += " ⚠️ CHIẾU!";
            }

            return desc;
        }



        private void ShowGameResult(bool isWin, string? customMessage = null)
        {
            bool wasOnline = _isOnline;
            _isOnline = false;
            btnThachDau.Enabled = true;
            btnThachDau.Text = "Thách Đấu";
            _gameTimer.StopAll();
            RefreshStatus();

            FormResult f = new FormResult(isWin, customMessage);
            f.ShowDialog(this);

            if (f.IsReviewMoves)
            {
                OpenReplayViewer();
            }
            else if (f.IsReplay)
            {
                if (wasOnline)
                {
                    _chatBox.AppendLog("[Hệ thống] Tự động tìm trận đấu mới...", Color.SteelBlue);
                    _ = AutoReplayOnlineAsync();
                }
                else
                {
                    ResetGame();
                    _gameTimer.StartGame();
                }
            }
        }

        private async Task AutoReplayOnlineAsync()
        {
            ResetGame();
            btnThachDau.Enabled = false;
            btnThachDau.Text = "Đang nối...";
            try
            {
                await ConnectToServer(_lastServerIp, _lastPort, _myUsername);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể kết nối lại máy chủ: {ex.Message}", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnThachDau.Enabled = true;
                btnThachDau.Text = "Thách Đấu";
            }
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
            _hasOfferedDraw = false;
            _flipBoard = false;

            _replaySteps.Clear();
            _replaySteps.Add(new ReplayStep(CloneGrid(_board.grid), null, null, "Bàn cờ ban đầu"));

            _gameTimer.StopAll();

            lblTimerRed.Text = $"🔴 Đỏ: {GameTimer.TurnDurationSeconds}s";
            lblTimerBlack.Text = $"⚫ Đen: {GameTimer.TurnDurationSeconds}s";
            RefreshStatus();

            this.Invalidate();
        }

        private async Task ConnectToServer(string serverIp, int port, string username)
        {
            _clientListener?.Dispose();
            _clientListener = new ClientEventListener();

            _clientListener.StateChanged += (state) =>
            {
                SafeInvoke(() =>
                {
                    _chatBox.AppendLog($"[Kết nối] {state}", Color.Gray);
                });
            };

            WireUpGameEvents();
            await _clientListener.ConnectAsync(serverIp, port, username);
        }

        private void WireUpGameEvents()
        {
            var listener = _clientListener;
            if (listener == null) return;

            listener.OnLoginOK += (msg) =>
            {
                SafeInvoke(() =>
                {
                    btnThachDau.Text = "Đang chờ...";
                    _chatBox.AppendLog($"[Server] {msg}", Color.DarkGreen);
                    _chatBox.AppendLog("[Kết nối] Đang trong hàng chờ tìm đối thủ...", Color.Gray);
                    SetStatus("⏳ Đang chờ đối thủ...", Color.DimGray);
                });
            };

            listener.OnLoginFail += (reason) =>
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show($"Đăng nhập không thành công: {reason}", "Lỗi Tên Đăng Nhập", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _isOnline = false;
                    btnThachDau.Enabled = true;
                    btnThachDau.Text = "Thách Đấu";
                    SetStatus("🔴 Chưa kết nối", Color.DimGray);
                });
            };

            listener.OnGameStart += (isRed, opponentName) =>
            {
                SafeInvoke(() =>
                {
                    _isPlayerRed = isRed;
                    _isOnline = true;
                    _isRedTurn = true;
                    _flipBoard = !isRed; // Tự động lật bàn cờ khi cầm quân Đen
                    _hasOfferedDraw = false;

                    _board = new Board();
                    _board.Init();
                    _selectedCell = null;
                    _validMoves.Clear();
                    _blockedMoves.Clear();
                    _lastFrom = null;
                    _lastTo = null;

                    _replaySteps.Clear();
                    _replaySteps.Add(new ReplayStep(CloneGrid(_board.grid), null, null, "Bàn cờ ban đầu"));

                    _chatBox.OpponentName = opponentName;
                    string color = isRed ? "Đỏ (đi trước)" : "Đen (đi sau)";
                    _chatBox.AppendLog($"[Game] Đối thủ: {opponentName}. Bạn cầm quân {color}!", Color.DarkGreen);
                    btnThachDau.Text = "Đang chơi";
                    RefreshStatus();

                    _gameTimer.StopAll();
                    _gameTimer.StartGame();
                    UpdateTimerLabelsAfterTurnChange();
                    this.Invalidate();
                });
            };

            listener.OnOpponentMove += (fromRow, fromCol, toRow, toCol) =>
            {
                SafeInvoke(() =>
                {
                    HandleOpponentMove(fromRow, fromCol, toRow, toCol);
                });
            };

            listener.OnChatReceived += (msg) =>
            {
                SafeInvoke(() =>
                {
                    if (msg.StartsWith("[Hệ thống]", StringComparison.OrdinalIgnoreCase) || 
                        msg.StartsWith("[Server]", StringComparison.OrdinalIgnoreCase))
                    {
                        _chatBox.AppendLog(msg, Color.OrangeRed);

                        // Nếu server báo nước đi không hợp lệ hoặc sai lượt, tự động rollback để đồng bộ bàn cờ
                        if (msg.Contains("không hợp lệ", StringComparison.OrdinalIgnoreCase) ||
                            msg.Contains("Chưa đến lượt", StringComparison.OrdinalIgnoreCase))
                        {
                            RollbackLastMoveIfDesynced();
                        }
                    }
                    else
                    {
                        _chatBox.AppendOpponentMessage(msg);
                    }
                });
            };

            listener.OnOpponentSurrender += () =>
            {
                SafeInvoke(() =>
                {
                    _chatBox.AppendLog($"[Game] {_chatBox.OpponentName} đã đầu hàng! Bạn thắng!", Color.DarkGreen);
                    ShowGameResult(true, $"🎉 {_chatBox.OpponentName} đã đầu hàng! Bạn thắng!");
                });
            };

            listener.OnOpponentTimeout += () =>
            {
                SafeInvoke(() =>
                {
                    _chatBox.AppendLog($"[Game] {_chatBox.OpponentName} đã hết thời gian suy nghĩ! Bạn thắng!", Color.DarkGreen);
                    ShowGameResult(true, $"🎉 {_chatBox.OpponentName} hết giờ! Bạn thắng!");
                });
            };

            listener.OnDrawOfferReceived += () =>
            {
                SafeInvoke(async () =>
                {
                    _chatBox.AppendLog($"[Game] {_chatBox.OpponentName} gửi lời đề nghị HÒA cờ!", Color.DarkGoldenrod);
                    var choice = MessageBox.Show(this, $"{_chatBox.OpponentName} muốn xin hòa ván cờ này.\nBạn có đồng ý HÒA không?", "Đối thủ xin hòa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    bool accept = (choice == DialogResult.Yes);

                    if (_clientListener != null)
                    {
                        try
                        {
                            await _clientListener.SendDrawResponseAsync(accept);
                        }
                        catch { }
                    }

                    if (accept)
                    {
                        _chatBox.AppendLog("[Game] Bạn đã đồng ý hòa!", Color.DarkSlateBlue);
                        ShowGameResult(false, "🤝 Ván đấu kết thúc với kết quả HÒA!");
                    }
                    else
                    {
                        _chatBox.AppendLog("[Game] Bạn đã từ chối lời đề nghị hòa.", Color.OrangeRed);
                    }
                });
            };

            listener.OnDrawResponseReceived += (accepted) =>
            {
                SafeInvoke(() =>
                {
                    _hasOfferedDraw = false;
                    if (accepted)
                    {
                        _chatBox.AppendLog($"[Game] {_chatBox.OpponentName} đã chấp nhận lời đề nghị hòa!", Color.DarkSlateBlue);
                        ShowGameResult(false, "🤝 Đối thủ đã đồng ý HÒA cờ!");
                    }
                    else
                    {
                        _chatBox.AppendLog($"[Game] {_chatBox.OpponentName} đã từ chối lời đề nghị hòa!", Color.OrangeRed);
                    }
                });
            };

            listener.OnGameEnd += (reason) =>
            {
                SafeInvoke(() =>
                {
                    _chatBox.AppendLog($"[Game] {reason}", Color.OrangeRed);
                    bool wasPlaying = _isOnline;
                    _isOnline = false;
                    btnThachDau.Enabled = true;
                    btnThachDau.Text = "Thách Đấu";
                    _gameTimer.StopAll();
                    RefreshStatus();

                    if (wasPlaying)
                    {
                        if (reason.Contains("mất kết nối", StringComparison.OrdinalIgnoreCase) || reason.Contains("rời khỏi", StringComparison.OrdinalIgnoreCase))
                        {
                            ShowGameResult(true, "🎉 Đối thủ mất kết nối! Bạn thắng!");
                        }
                        else
                        {
                            ShowGameResult(false, $"💀 {reason}");
                        }
                    }
                });
            };

            listener.OnConnectionError += (ex) =>
            {
                SafeInvoke(() =>
                {
                    if (_isOnline || !btnThachDau.Enabled)
                    {
                        _chatBox.AppendLog("[Mạng] Mất kết nối tới máy chủ!", Color.Red);
                        _isOnline = false;
                        btnThachDau.Enabled = true;
                        btnThachDau.Text = "Thách Đấu";
                        _gameTimer.StopAll();
                        RefreshStatus();
                    }
                });
            };
        }

        private void HandleOpponentMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            Piece? captured = _board.grid[toRow, toCol];

            bool moved = MoveValidator.TryMove(_board, fromRow, fromCol, toRow, toCol, !_isPlayerRed);
            if (!moved)
            {
                _chatBox.AppendLog("[Cảnh báo] Lỗi đồng bộ nước đi từ đối thủ!", Color.Red);
                return;
            }

            _lastFrom = new Point(fromCol, fromRow);
            _lastTo = new Point(toCol, toRow);

            if (captured != null)
                SoundManager.PlayCapture();
            else
                SoundManager.PlayMove();

            string moveDesc = FormatMoveDescription(_board.grid[toRow, toCol], fromRow, fromCol, toRow, toCol, captured);
            _replaySteps.Add(new ReplayStep(CloneGrid(_board.grid), _lastFrom, _lastTo, moveDesc));

            _isRedTurn = !_isRedTurn;
            _gameTimer.StopCountdown();
            _gameTimer.StartTurn();
            UpdateTimerLabelsAfterTurnChange();

            RefreshStatus();

            _selectedCell = null;
            _validMoves.Clear();
            _blockedMoves.Clear();

            CheckThreefoldRepetition();

            if (ChessRules.IsCheckmate(_board, _isRedTurn))
            {
                _chatBox.AppendLog("[Game] Chiếu bí! Bạn đã bị đánh bại!", Color.OrangeRed);
                ShowGameResult(false, "💀 Chiếu bí! Bạn đã bị đánh bại!");
            }

            this.Invalidate();
        }

        private void RollbackLastMoveIfDesynced()
        {
            if (_replaySteps.Count >= 2)
            {
                _replaySteps.RemoveAt(_replaySteps.Count - 1);
                var prevStep = _replaySteps[^1];
                _board.grid = CloneGrid(prevStep.Grid);
                _lastFrom = prevStep.LastFrom;
                _lastTo = prevStep.LastTo;
                _isRedTurn = !_isRedTurn;
                _selectedCell = null;
                _validMoves.Clear();
                _blockedMoves.Clear();
                _gameTimer.StopCountdown();
                _gameTimer.StartTurn();
                UpdateTimerLabelsAfterTurnChange();
                RefreshStatus();
                this.Invalidate();
                _chatBox.AppendLog("[Hệ thống] Đã tự động hoàn tác nước đi về trạng thái đồng bộ với Server.", Color.DarkGoldenrod);
            }
        }

        private void CheckThreefoldRepetition()
        {
            if (_replaySteps.Count < 6) return;

            var currentGrid = _board.grid;
            int count = 0;
            for (int i = 0; i < _replaySteps.Count; i++)
            {
                if (IsSameGrid(_replaySteps[i].Grid, currentGrid))
                {
                    count++;
                }
            }

            if (count >= 3)
            {
                _chatBox.AppendLog("[Trọng tài] Cảnh báo: Thế cờ đã lặp lại 3 lần!", Color.DarkGoldenrod);
            }
        }

        private static bool IsSameGrid(Piece?[,] g1, Piece?[,] g2)
        {
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    var p1 = g1[r, c];
                    var p2 = g2[r, c];
                    if (p1 == null && p2 == null) continue;
                    if (p1 == null || p2 == null) return false;
                    if (p1.Type != p2.Type || p1.IsRed != p2.IsRed) return false;
                }
            }
            return true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isOnline && _clientListener != null)
            {
                try
                {
                    _ = _clientListener.SendSurrenderAsync();
                }
                catch { }
            }
            _gameTimer?.StopAll();
            base.OnFormClosing(e);
        }

        private (string serverIp, int port, string username)? PromptConnectionInfo()
        {
            using var dialog = new Form();
            dialog.Text = "Kết nối Server Cờ Tướng";
            dialog.Size = new Size(360, 235);
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;

            var lblIp = new Label { Text = "Địa chỉ IP Server:", Location = new Point(20, 15), AutoSize = true };
            var txtIp = new TextBox { Text = _lastServerIp, Location = new Point(20, 35), Size = new Size(300, 25) };

            var lblUser = new Label { Text = "Tên người chơi (3 - 20 ký tự):", Location = new Point(20, 75), AutoSize = true };
            string defaultName = string.IsNullOrEmpty(_myUsername) || _myUsername == "Tôi" ? "KyThu_" + new Random().Next(100, 999) : _myUsername;
            var txtUser = new TextBox { Text = defaultName, Location = new Point(20, 95), Size = new Size(300, 25) };

            var btnOk = new Button
            {
                Text = "Vào Trận",
                DialogResult = DialogResult.OK,
                Location = new Point(55, 140),
                Size = new Size(110, 35),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var btnCancel = new Button
            {
                Text = "Hủy Bỏ",
                DialogResult = DialogResult.Cancel,
                Location = new Point(185, 140),
                Size = new Size(110, 35),
                FlatStyle = FlatStyle.Flat
            };

            btnOk.Click += (s, e) =>
            {
                string u = txtUser.Text.Trim();
                if (u.Length < 3 || u.Length > 20)
                {
                    MessageBox.Show(dialog, "Tên người chơi phải có từ 3 đến 20 ký tự!", "Tên không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dialog.DialogResult = DialogResult.None;
                    return;
                }
                if (u.Contains('|') || u.Contains('\n') || u.Contains('\r'))
                {
                    MessageBox.Show(dialog, "Tên người chơi không được chứa ký tự phân cách (|) hoặc xuống dòng!", "Tên không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dialog.DialogResult = DialogResult.None;
                    return;
                }
            };

            dialog.Controls.AddRange(new Control[] { lblIp, txtIp, lblUser, txtUser, btnOk, btnCancel });
            dialog.AcceptButton = btnOk;
            dialog.CancelButton = btnCancel;

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                string ip = txtIp.Text.Trim();
                if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";

                int port = _lastPort > 0 ? _lastPort : 54000;

                string user = txtUser.Text.Trim();
                if (string.IsNullOrEmpty(user))
                    user = defaultName;

                return (ip, port, user);
            }

            return null;
        }
    }
}
