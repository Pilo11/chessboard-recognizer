using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace ChessClient
{
    class Program
    {
        private static readonly HttpClient _client = new HttpClient();
        private static bool _isCurrentlyWorking;
        private static string _player = "w";

        static void Main(string[] args)
        {
            Console.Write("White (w) (default) or black (b): ");
            _player = Console.ReadLine().ToLower() == "b" ? "b" : "w";
            var timer = new Timer();
            timer.Interval = 16;
            timer.Elapsed += new ElapsedEventHandler(Test);
            timer.Start();
            Console.Read();
        }

        private static async void Test(object sender, ElapsedEventArgs e)
        {
            if (!_isCurrentlyWorking)
            {
                if (WindowsApi.IsUKeyClicked())
                {
                    _isCurrentlyWorking = true;
                    try
                    {
                        await DoAnalyze();                        
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        _isCurrentlyWorking = false;
                    }
                }
            }
        }

        private static async Task DoAnalyze()
        {
            var handle = GetChessWindowHandle();
            using Stream imageStream = new MemoryStream();
            using Bitmap image = WindowsApi.CaptureWindow(handle);
            using Bitmap imageCopy = image.Clone(new Rectangle(500, 140, 1180, 1180), image.PixelFormat);
            imageCopy.Save(imageStream, ImageFormat.Png);
            imageStream.Seek(0, SeekOrigin.Begin);
            var content = new StreamContent(imageStream);
            _client.DefaultRequestHeaders.Remove("player");
            _client.DefaultRequestHeaders.Add("player", _player);
            var response = await _client.PostAsync("http://10.35.17.1:5000/chess/result", content);
            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
        }        

        private static IntPtr GetChessWindowHandle()
        {
            Process[] processes = Process.GetProcessesByName("chrome");
            foreach (Process p in processes.Where(p => !string.IsNullOrWhiteSpace(p.MainWindowTitle)))
            {
                var title = p.MainWindowTitle;
                if (title.ToLower().Contains("chess.com"))
                {
                    return p.MainWindowHandle;
                }
            }
            throw new Exception("Could not find Chess.com window");
        }

    }
}
