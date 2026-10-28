using System;
using System.Drawing;
using System.Windows.Forms;

namespace CoTuongOnline.Client
{
    public class FormResult : Form
    {
        public bool IsReplay { get; private set; } = false;
        public bool IsReviewMoves { get; private set; } = false;

        public FormResult(bool isWin, string? customMessage = null)
        {
            this.Text = "Kết thúc ván cờ";
            this.Size = new Size(385, 220);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            bool isDraw = customMessage != null && (customMessage.Contains("hòa", StringComparison.OrdinalIgnoreCase) || customMessage.Contains("HÒA"));
            Color textColor = isDraw ? Color.DarkSlateBlue : (isWin ? Color.DarkGreen : Color.DarkRed);

            Label lbl = new Label
            {
                Text = customMessage ?? (isWin ? "🎉 Bạn thắng!" : "💀 Bạn thua!"),
                Font = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = textColor,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 85
            };

            Button btnReview = new Button
            {
                Text = "📜 Xem Lại",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Size = new Size(105, 42),
                Location = new Point(20, 110),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnReview.Click += (s, e) =>
            {
                IsReviewMoves = true;
                this.Close();
            };

            Button btnReplay = new Button
            {
                Text = "Chơi lại",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Size = new Size(105, 42),
                Location = new Point(135, 110),
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnReplay.Click += (s, e) =>
            {
                IsReplay = true;
                this.Close();
            };

            Button btnExit = new Button
            {
                Text = "Thoát",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Size = new Size(105, 42),
                Location = new Point(250, 110),
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnExit.Click += (s, e) =>
            {
                Application.Exit();
            };

            this.Controls.Add(lbl);
            this.Controls.Add(btnReview);
            this.Controls.Add(btnReplay);
            this.Controls.Add(btnExit);
        }
    }
}
