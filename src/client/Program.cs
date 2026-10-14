using System;
using System.Windows.Forms;

namespace CoTuongOnline.Client
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Chạy FormMenu đầu tiên khi mở ứng dụng
            Application.Run(new FormMenu()); 
        }
    }
}