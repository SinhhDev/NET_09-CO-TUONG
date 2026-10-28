using System;
using System.Drawing;
using System.Windows.Forms;

namespace CoTuongOnline.Client
{
    public class FormMenu : Form
    {
        public FormMenu()
        {
            this.Text = "Cờ Tướng Online - Menu";
            this.Size = new Size(420, 360);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            Label lblTitle = new Label
            {
                Text = "♟️ CỜ TƯỚNG ONLINE",
                Font = new Font("Arial", 16F, FontStyle.Bold),
                ForeColor = Color.DarkRed,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60
            };

            Button btnOffline = new Button
            {
                Text = "Chơi Offline",
                Font = new Font("Arial", 11F, FontStyle.Bold),
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(200, 45),
                Location = new Point(105, 80)
            };

            Button btnOnline = new Button
            {
                Text = "Chơi Online",
                Font = new Font("Arial", 11F, FontStyle.Bold),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(200, 45),
                Location = new Point(105, 145)
            };

            Button btnExit = new Button
            {
                Text = "Thoát",
                Font = new Font("Arial", 11F, FontStyle.Bold),
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(200, 45),
                Location = new Point(105, 210)
            };

            btnOffline.Click += (s, e) =>
            {
                FormGame game = new FormGame(autoConnectOnline: false);
                game.FormClosed += (sender, args) => this.Close();
                game.Show();
                this.Hide();
            };

            btnOnline.Click += (s, e) =>
            {
                FormGame game = new FormGame(autoConnectOnline: true);
                game.FormClosed += (sender, args) => this.Close();
                game.Show();
                this.Hide();
            };

            btnExit.Click += (s, e) => Application.Exit();

            this.Controls.Add(lblTitle);
            this.Controls.Add(btnOffline);
            this.Controls.Add(btnOnline);
            this.Controls.Add(btnExit);
        }
    }
}
