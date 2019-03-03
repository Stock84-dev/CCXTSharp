using CCXTSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCXTSharpTest
{
	class Program
	{
        static bool loop = true;
		static async void f()
		{

            Stopwatch sw = new Stopwatch();

			//CcxtAPI ccxtAPI = new CcxtAPI(@"/home/leon/Documents/projects/CCXTSharp/CCXT/ccxtAPI.py", @"/bin/python");
			CcxtAPI ccxtAPI = new CcxtAPI(@"D:\Documents\New folder\CCXTSharp\CCXT\dist\ccxtAPI.exe");
			//CcxtAPI ccxtAPI = new CcxtAPI(@"D:\Documents\New folder\CCXTSharp\CCXT\ccxtAPI.py", @"C:\Program Files (x86)\Microsoft Visual Studio\Shared\Python36_86\python.exe", true);
			long dt = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds();
            List<Candlestick> c = await ccxtAPI.FetchOHLCV("coinbasepro", "BTC/USD", Timeframe.min1, dt);
            foreach (var item in c)
            {
                Console.WriteLine(item.close);
            }
            var markets = await ccxtAPI.FetchMarkets("binance");
            markets.ForEach(m => Console.WriteLine(m.symbol));
            await ccxtAPI.Close();
            Console.WriteLine("closed");
            loop = false;
        }

        static void Main(string[] args)
		{ 
            f();
            while (loop)
            {
                Task.Delay(1000).Wait();
            }
            Task.Delay(10000).Wait();
		}
	}
}
