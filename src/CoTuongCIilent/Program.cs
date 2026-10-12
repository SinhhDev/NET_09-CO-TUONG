using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

internal partial class Program
{
    private static async Task Main()
    {
        Client CoTuongClient = new CoTuongClient();
        Client.Connect("127.0.0.1", 5000);
        Console.ReadLine();
        string serverIP = "127.0.0.1";
        int port = 5000;
        

        using TcpClient client = new TcpClient();

        try
        {
            await client.ConnectAsync(serverIP, port);
            Console.WriteLine("Da ket noi toi server!");

            NetworkStream stream = client.GetStream();
            byte[] data = Encoding.UTF8.GetBytes("HELLO");

            await stream.WriteAsync(data);
            Console.WriteLine("Da gui du lieu toi server.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Loi ket noi: " + ex.Message);
        }

        Console.ReadKey();
    }
}
