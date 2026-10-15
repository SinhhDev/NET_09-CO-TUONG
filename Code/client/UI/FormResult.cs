using System;
using System.Drawing;
using System.Windows.Forms;

namespace CoTuongOnline.Client
{
    public class FormResult : Form
    {
        public bool IsReplay { get; private set; } = false;

        public FormResult(bool isWin)
        {
            this.Text = "Kết thúc";
            this.Size = new Size(300, 200);
            this.StartPosition = FormStartPosition.CenterScreen;

            Label lbl = new Label();
            lbl.Text = isWin ? "🎉 Bạn thắng!" : "💀 Bạn thua!";
            lbl.Font = new Font("Arial", 16, FontStyle.Bold);
            lbl.Dock = DockStyle.Top;
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            lbl.Height = 80;

            Button btnReplay = new Button();
            btnReplay.Text = "Chơi lại";
            btnReplay.Size = new Size(100, 40);
            btnReplay.Location = new Point(30, 100);
            btnReplay.Click += (s, e) =>
            {
                IsReplay = true;
                this.Close();
            };

            Button btnExit = new Button();
            btnExit.Text = "Thoát";
            btnExit.Size = new Size(100, 40);
            btnExit.Location = new Point(150, 100);
            btnExit.Click += (s, e) =>
            {
                Application.Exit();
            };

            this.Controls.Add(lbl);
            this.Controls.Add(btnReplay);
            this.Controls.Add(btnExit);
        }
    }
}