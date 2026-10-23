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
            SoundPlayer player = new SoundPlayer(Path.Combine(basePath, "move.wav"));
            player.Play();
        }

        public static void PlayCapture()
        {
            SoundPlayer player = new SoundPlayer(Path.Combine(basePath, "capture.wav"));
            player.Play();
        }
    }
}