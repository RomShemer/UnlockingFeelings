using System;
using System.IO.Ports;
using System.Net;
using System.Threading.Tasks;

class Program
{
    static SerialPort serial;

    static async Task Main()
    {
        serial = new SerialPort("COM4", 115200); // שימי את ה-COM של הלוח שלך
        try
        {
            serial.Open();
            Console.WriteLine("✅ Serial פתוח");
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ שגיאה בחיבור ל־Serial: " + ex.Message);
            return;
        }

        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://*:5000/");
        listener.Start();
        Console.WriteLine("🌐 מאזין על http://localhost:5000/");

        while (true)
        {
            var context = await listener.GetContextAsync();
            string path = context.Request.Url.AbsolutePath.ToLower();

            if (path == "/on")
            {
                serial.WriteLine("ON");
                Console.WriteLine("📤 נשלח ON");
            }
            else if (path == "/off")
            {
                serial.WriteLine("OFF");
                Console.WriteLine("📤 נשלח OFF");
            }

            byte[] response = System.Text.Encoding.UTF8.GetBytes("OK");
            context.Response.OutputStream.Write(response, 0, response.Length);
            context.Response.Close();
        }
    }
}
