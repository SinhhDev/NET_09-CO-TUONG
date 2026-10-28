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
