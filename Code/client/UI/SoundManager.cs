using System;
using System.IO;
using System.Media;
using System.Windows.Forms;

namespace CoTuongOnline.Client
{
    public static class SoundManager
    {
        private static readonly string basePath = Path.Combine(Application.StartupPath, "Sounds");

        public static void PlayMove()
        {
            try
            {
                string path = Path.Combine(basePath, "move.wav");
                if (File.Exists(path))
                {
                    using SoundPlayer player = new SoundPlayer(path);
                    player.Play();
                }
            }
            catch
            {
                // Bỏ qua lỗi âm thanh để không làm crash game
            }
        }

        public static void PlayCapture()
        {
            try
            {
                string path = Path.Combine(basePath, "capture.wav");
                if (File.Exists(path))
                {
                    using SoundPlayer player = new SoundPlayer(path);
                    player.Play();
                }
            }
            catch
            {
                // Bỏ qua lỗi âm thanh để không làm crash game
            }
        }
    }
}
