using System;
using System.Drawing;
using System.Windows.Forms;

namespace CoTuongOnline.Client
{
    public class FormMenu : Form
    {
        public FormMenu()
        {
            this.Text = "Menu Chính";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;

            Button btnStart = new Button
            {
                Text = "Bắt Đầu",
                Size = new Size(150, 50),
                Location = new Point(120, 80)
            };

            Button btnExit = new Button
            {
                Text = "Thoát",
                Size = new Size(150, 50),
                Location = new Point(120, 150)
            };

            btnStart.Click += (s, e) =>
            {
                Form1 game = new Form1();
                game.Show();
                this.Hide();
            };

            btnExit.Click += (s, e) => Application.Exit();

            this.Controls.Add(btnStart);
            this.Controls.Add(btnExit);
        }
    }
}