using System;
using System.IO;
using System.Media;
using System.Windows.Forms;

namespace CoTuongOnline.Client
{
    public static class SoundManager
    {
        private static readonly SoundPlayer? _movePlayer;
        private static readonly SoundPlayer? _capturePlayer;

        static SoundManager()
        {
            string basePath = Path.Combine(Application.StartupPath, "Sounds");
            if (!Directory.Exists(basePath))
            {
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 5; i++)
                {
                    string testPath = Path.Combine(currentDir, "Sounds");
                    if (Directory.Exists(testPath))
                    {
                        basePath = testPath;
                        break;
                    }
                    string testPath2 = Path.Combine(currentDir, "Code", "client", "Sounds");
                    if (Directory.Exists(testPath2))
                    {
                        basePath = testPath2;
                        break;
                    }
                    var parent = Directory.GetParent(currentDir);
                    if (parent == null) break;
                    currentDir = parent.FullName;
                }
            }

            try
            {
                string movePath = Path.Combine(basePath, "move.wav");
                if (File.Exists(movePath))
                {
                    _movePlayer = new SoundPlayer(movePath);
                    _movePlayer.Load();
                }

                string capturePath = Path.Combine(basePath, "capture.wav");
                if (File.Exists(capturePath))
                {
                    _capturePlayer = new SoundPlayer(capturePath);
                    _capturePlayer.Load();
                }
            }
            catch
            {
                // Bỏ qua lỗi âm thanh để không làm crash game
            }
        }

        public static void PlayMove()
        {
            try
            {
                _movePlayer?.Play();
            }
            catch
            {
                // Bỏ qua lỗi âm thanh
            }
        }

        public static void PlayCapture()
        {
            try
            {
                _capturePlayer?.Play();
            }
            catch
            {
                // Bỏ qua lỗi âm thanh
            }
        }
    }
}
