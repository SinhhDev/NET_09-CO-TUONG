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
        private Button btnXinHoa = null!;
        private Button btnReplay = null!;
        private Button btnThoat = null!;
        private ChatBox _chatBox = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                _gameTimer?.Dispose();
                _clientListener?.Dispose();
                foreach (var img in _pieces.Values)
                {
                    img?.Dispose();
                }
                _pieces.Clear();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            txtStatus = new TextBox();
            lblTimerRed = new Label();
            lblTimerBlack = new Label();
            btnThachDau = new Button();
            btnXinHoa = new Button();
            btnReplay = new Button();
            btnThoat = new Button();
            _chatBox = new ChatBox();
            SuspendLayout();

            txtStatus.BackColor = Color.White;
            txtStatus.BorderStyle = BorderStyle.FixedSingle;
            txtStatus.Font = new Font("Arial", 12F, FontStyle.Bold);
            txtStatus.ForeColor = Color.DarkRed;
            txtStatus.Location = new Point(545, 10);
            txtStatus.Name = "txtStatus";
            txtStatus.ReadOnly = true;
            txtStatus.Size = new Size(175, 34);
            txtStatus.TabStop = false;
            txtStatus.Text = "🔴 Lượt Đỏ";
            txtStatus.TextAlign = HorizontalAlignment.Center;

            lblTimerRed.Font = new Font("Arial", 11.5F, FontStyle.Bold);
            lblTimerRed.ForeColor = Color.DarkRed;
            lblTimerRed.Location = new Point(545, 48);
            lblTimerRed.Name = "lblTimerRed";
            lblTimerRed.Size = new Size(175, 26);
            lblTimerRed.Text = "🔴 Đỏ: 45s";
            lblTimerRed.TextAlign = ContentAlignment.MiddleCenter;

            lblTimerBlack.Font = new Font("Arial", 11.5F, FontStyle.Bold);
            lblTimerBlack.ForeColor = Color.Black;
            lblTimerBlack.Location = new Point(545, 76);
            lblTimerBlack.Name = "lblTimerBlack";
            lblTimerBlack.Size = new Size(175, 26);
            lblTimerBlack.Text = "⚫ Đen: 45s";
            lblTimerBlack.TextAlign = ContentAlignment.MiddleCenter;

            btnThachDau.BackColor = Color.SteelBlue;
            btnThachDau.FlatStyle = FlatStyle.Flat;
            btnThachDau.Font = new Font("Arial", 10.5F, FontStyle.Bold);
            btnThachDau.ForeColor = Color.White;
            btnThachDau.Location = new Point(545, 108);
            btnThachDau.Name = "btnThachDau";
            btnThachDau.Size = new Size(175, 33);
            btnThachDau.Text = "Thách Đấu";
            btnThachDau.UseVisualStyleBackColor = false;
            btnThachDau.Click += btnThachDau_Click;

            btnXinHoa.BackColor = Color.DarkGoldenrod;
            btnXinHoa.FlatStyle = FlatStyle.Flat;
            btnXinHoa.Font = new Font("Arial", 10.5F, FontStyle.Bold);
            btnXinHoa.ForeColor = Color.White;
            btnXinHoa.Location = new Point(545, 145);
            btnXinHoa.Name = "btnXinHoa";
            btnXinHoa.Size = new Size(175, 33);
            btnXinHoa.Text = "🤝 Xin Hòa";
            btnXinHoa.UseVisualStyleBackColor = false;
            btnXinHoa.Click += btnXinHoa_Click;

            btnReplay.BackColor = Color.SeaGreen;
            btnReplay.FlatStyle = FlatStyle.Flat;
            btnReplay.Font = new Font("Arial", 10.5F, FontStyle.Bold);
            btnReplay.ForeColor = Color.White;
            btnReplay.Location = new Point(545, 182);
            btnReplay.Name = "btnReplay";
            btnReplay.Size = new Size(175, 33);
            btnReplay.Text = "Chơi Lại";
            btnReplay.UseVisualStyleBackColor = false;
            btnReplay.Click += btnReplay_Click;

            btnThoat.BackColor = Color.IndianRed;
            btnThoat.FlatStyle = FlatStyle.Flat;
            btnThoat.Font = new Font("Arial", 10.5F, FontStyle.Bold);
            btnThoat.ForeColor = Color.White;
            btnThoat.Location = new Point(545, 219);
            btnThoat.Name = "btnThoat";
            btnThoat.Size = new Size(175, 33);
            btnThoat.Text = "Thoát";
            btnThoat.UseVisualStyleBackColor = false;
            btnThoat.Click += btnThoat_Click;

            _chatBox.Location = new Point(545, 258);
            _chatBox.Name = "_chatBox";
            _chatBox.Size = new Size(175, 317);

            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(745, 595);
            Controls.Add(txtStatus);
            Controls.Add(lblTimerRed);
            Controls.Add(lblTimerBlack);
            Controls.Add(btnThachDau);
            Controls.Add(btnXinHoa);
            Controls.Add(btnReplay);
            Controls.Add(btnThoat);
            Controls.Add(_chatBox);
            Name = "FormGame";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
