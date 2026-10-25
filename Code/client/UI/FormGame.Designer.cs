#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace CoTuongOnline.Client
{
    partial class FormGame
    {
        private System.ComponentModel.IContainer? components = null;

        private TextBox txtStatus = null!;
        private Label lblTimerRed = null!;
        private Label lblTimerBlack = null!;
        private Button btnThachDau = null!;
        private Button btnReplay = null!;
        private Button btnThoat = null!;
        private ChatBox _chatBox = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            txtStatus = new TextBox();
            lblTimerRed = new Label();
            lblTimerBlack = new Label();
            btnThachDau = new Button();
            btnReplay = new Button();
            btnThoat = new Button();
            _chatBox = new ChatBox();
            SuspendLayout();

            txtStatus.BackColor = Color.White;
            txtStatus.BorderStyle = BorderStyle.FixedSingle;
            txtStatus.Font = new Font("Arial", 13F, FontStyle.Bold);
            txtStatus.ForeColor = Color.DarkRed;
            txtStatus.Location = new Point(550, 10);
            txtStatus.Name = "txtStatus";
            txtStatus.ReadOnly = true;
            txtStatus.Size = new Size(160, 40);
            txtStatus.TabStop = false;
            txtStatus.Text = "🔴 Lượt Đỏ";
            txtStatus.TextAlign = HorizontalAlignment.Center;

            lblTimerRed.Font = new Font("Arial", 12F, FontStyle.Bold);
            lblTimerRed.ForeColor = Color.DarkRed;
            lblTimerRed.Location = new Point(550, 60);
            lblTimerRed.Name = "lblTimerRed";
            lblTimerRed.Size = new Size(160, 30);
            lblTimerRed.Text = "🔴 Đỏ: 45s";
            lblTimerRed.TextAlign = ContentAlignment.MiddleCenter;

            lblTimerBlack.Font = new Font("Arial", 12F, FontStyle.Bold);
            lblTimerBlack.ForeColor = Color.Black;
            lblTimerBlack.Location = new Point(550, 95);
            lblTimerBlack.Name = "lblTimerBlack";
            lblTimerBlack.Size = new Size(160, 30);
            lblTimerBlack.Text = "⚫ Đen: 45s";
            lblTimerBlack.TextAlign = ContentAlignment.MiddleCenter;

            btnThachDau.BackColor = Color.SteelBlue;
            btnThachDau.FlatStyle = FlatStyle.Flat;
            btnThachDau.Font = new Font("Arial", 11F, FontStyle.Bold);
            btnThachDau.ForeColor = Color.White;
            btnThachDau.Location = new Point(550, 140);
            btnThachDau.Name = "btnThachDau";
            btnThachDau.Size = new Size(160, 45);
            btnThachDau.Text = "Thách Đấu";
            btnThachDau.UseVisualStyleBackColor = false;
            btnThachDau.Click += btnThachDau_Click;

            btnReplay.BackColor = Color.SeaGreen;
            btnReplay.FlatStyle = FlatStyle.Flat;
            btnReplay.Font = new Font("Arial", 11F, FontStyle.Bold);
            btnReplay.ForeColor = Color.White;
            btnReplay.Location = new Point(550, 195);
            btnReplay.Name = "btnReplay";
            btnReplay.Size = new Size(160, 45);
            btnReplay.Text = "Chơi Lại";
            btnReplay.UseVisualStyleBackColor = false;
            btnReplay.Click += btnReplay_Click;

            btnThoat.BackColor = Color.IndianRed;
            btnThoat.FlatStyle = FlatStyle.Flat;
            btnThoat.Font = new Font("Arial", 11F, FontStyle.Bold);
            btnThoat.ForeColor = Color.White;
            btnThoat.Location = new Point(550, 250);
            btnThoat.Name = "btnThoat";
            btnThoat.Size = new Size(160, 45);
            btnThoat.Text = "Thoát";
            btnThoat.UseVisualStyleBackColor = false;
            btnThoat.Click += btnThoat_Click;

            _chatBox.Location = new Point(550, 310);
            _chatBox.Name = "_chatBox";
            _chatBox.Size = new Size(160, 220);

            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(740, 600);
            Controls.Add(txtStatus);
            Controls.Add(lblTimerRed);
            Controls.Add(lblTimerBlack);
            Controls.Add(btnThachDau);
            Controls.Add(btnReplay);
            Controls.Add(btnThoat);
            Controls.Add(_chatBox);
            Name = "FormGame";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
