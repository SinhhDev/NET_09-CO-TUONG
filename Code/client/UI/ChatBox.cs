using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace CoTuongOnline.Client
{
    public class ChatBox : UserControl
    {
        private RichTextBox _rtbMessages = null!;
        private TextBox _txtInput = null!;
        private Button _btnSend = null!;
        private string _opponentName = "Đối thủ";

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string OpponentName
        {
            get => _opponentName;
            set => _opponentName = string.IsNullOrWhiteSpace(value) ? "Đối thủ" : value;
        }

        public event Action<string>? OnChatMessage;
        public event Action<string, string?>? OnCommand;

        public ChatBox()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _rtbMessages = new RichTextBox
            {
                Dock = DockStyle.Top,
                Height = 180,
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };

            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 35
            };

            _txtInput = new TextBox
            {
                Location = new Point(0, 5),
                Width = 105,
                Font = new Font("Segoe UI", 9F)
            };
            _txtInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    SendMessage();
                }
            };

            _btnSend = new Button
            {
                Text = "Gửi",
                Location = new Point(110, 3),
                Size = new Size(50, 28),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnSend.Click += (s, e) => SendMessage();

            pnlBottom.Controls.Add(_txtInput);
            pnlBottom.Controls.Add(_btnSend);

            this.Controls.Add(_rtbMessages);
            this.Controls.Add(pnlBottom);
        }

        private void SendMessage()
        {
            string text = _txtInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            _txtInput.Clear();

            if (text.StartsWith("/"))
            {
                string[] parts = text.Substring(1).Split(' ', 2);
                string cmd = parts[0].ToLowerInvariant();
                string? arg = parts.Length > 1 ? parts[1] : null;
                OnCommand?.Invoke(cmd, arg);
            }
            else
            {
                AppendLog($"[Tôi] {text}", Color.Blue);
                OnChatMessage?.Invoke(text);
            }
        }

        public void AppendLog(string message, Color color)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((Action)(() => AppendLog(message, color)));
                return;
            }

            _rtbMessages.SelectionStart = _rtbMessages.TextLength;
            _rtbMessages.SelectionLength = 0;
            _rtbMessages.SelectionColor = color;
            _rtbMessages.AppendText(message + Environment.NewLine);
            _rtbMessages.ScrollToCaret();
        }

        public void AppendOpponentMessage(string message)
        {
            AppendLog($"[{_opponentName}] {message}", Color.DarkViolet);
        }
    }
}
