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
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 32
            };

            _btnSend = new Button
            {
                Text = "Gửi",
                Dock = DockStyle.Right,
                Width = 48,
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            _btnSend.Click += (s, e) => SendMessage();

            _txtInput = new TextBox
            {
                Dock = DockStyle.Fill,
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

            pnlBottom.Controls.Add(_txtInput);
            pnlBottom.Controls.Add(_btnSend);

            _rtbMessages = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };

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
            if (this.IsDisposed || !this.IsHandleCreated) return;

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
