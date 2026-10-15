using System;
using System.Drawing;
using System.Windows.Forms;

namespace CoTuongOnline.Client
{
    public partial class Form1 : Form
    {
        private const int Cell = 60;
        private const int StartX = 30;
        private const int StartY = 30;

        private bool _isRedTurn = true;
        private TextBox txtStatus = null!;

        public Form1()
        {
            // InitializeComponent(); // Bỏ comment nếu có file Form1.designer.cs
            this.ClientSize = new Size(StartX * 2 + 8 * Cell + 200, StartY * 2 + 9 * Cell);
            this.Text = "Cờ Tướng Online (Chỉ Giao Diện)";
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Paint += Form1_Paint;
            this.Load += Form1_Load;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Vẽ nền bàn cờ
            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 222, 173)), StartX, StartY, 8 * Cell, 9 * Cell);
            Pen pen = new Pen(Color.Black, 2);
            g.DrawRectangle(pen, StartX, StartY, 8 * Cell, 9 * Cell);

            // Vẽ lưới ngang dọc
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

            // Vẽ đường chéo cung tướng
            g.DrawLine(pen, StartX + 3 * Cell, StartY, StartX + 5 * Cell, StartY + 2 * Cell);
            g.DrawLine(pen, StartX + 5 * Cell, StartY, StartX + 3 * Cell, StartY + 2 * Cell);
            g.DrawLine(pen, StartX + 3 * Cell, StartY + 7 * Cell, StartX + 5 * Cell, StartY + 9 * Cell);
            g.DrawLine(pen, StartX + 5 * Cell, StartY + 7 * Cell, StartX + 3 * Cell, StartY + 9 * Cell);

            // Vẽ chữ sông
            Font fontRiver = new Font("SimSun", 24, FontStyle.Bold);
            g.DrawString("楚河", fontRiver, Brushes.DarkRed, StartX + 1.4f * Cell, StartY + 4.15f * Cell);
            g.DrawString("漢界", fontRiver, Brushes.DarkRed, StartX + 4.8f * Cell, StartY + 4.15f * Cell);
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
            this.Controls.Add(txtStatus);

            Button btnReplay = new Button
            {
                Text = "Chơi Lại",
                Size = new Size(160, 45),
                Location = new Point(btnX, 60),
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
                Location = new Point(btnX, 115),
                Font = new Font("Arial", 11, FontStyle.Bold),
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnThoat.Click += (s, ev) => Application.Exit();
            this.Controls.Add(btnThoat);
        }

        private void ShowGameResult(bool isWin)
        {
            FormResult f = new FormResult(isWin);
            f.ShowDialog();
            if (f.IsReplay) ResetGame();
        }

        private void ResetGame()
        {
            _isRedTurn = true;
            txtStatus.Text = "🔴 Lượt Đỏ";
            txtStatus.ForeColor = Color.DarkRed;
            this.Invalidate();
        }
    }
}