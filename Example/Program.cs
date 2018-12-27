using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CCXTSharp;

namespace Example
{
	class Program
	{
		static async void Example1()
		{
			CcxtAPI ccxtAPI = new CcxtAPI(@"..\..\ccxt\ccxtAPI.exe");				// initialize and start process
			List<string> exchangeIds = await ccxtAPI.GetExchangIds();				// get list of avaliable exchanges
			exchangeIds.ForEach(id => Console.WriteLine(id));						// print all exchange ids
			await ccxtAPI.Close();													// it's preferred to exit other process if your program will close
		}

		static async void Example2()
		{
			// initialize and run script in python interpreter
			CcxtAPI ccxtAPI = new CcxtAPI(@"..\..\ccxt\scripts\ccxtAPI.py", @"D:\Program Files (x86)\Python36-32\python.exe");  
			// gets list of markets on Binance exchange and prints their symbols
			List<Market> markets = await ccxtAPI.FetchMarkets("binance");
			markets.ForEach(m => Console.WriteLine(m.symbol));
			await ccxtAPI.Close();
		}

		static async void Example3()
		{
			CcxtAPI ccxtAPI = new CcxtAPI(@"..\..\ccxt\ccxtAPI.exe");
			// if cryptopia has support for fetch tickers then fetch them
			if((await ccxtAPI.GetExchangeHas("cryptopia")).fetchTickers == Has.Capability.True)
			{
				Dictionary<string, Ticker> tickers = await ccxtAPI.FetchTickers("cryptopia");
				// foreach ticker print their symbol and change
				foreach (var ticker in tickers.Values)
				{
					Console.WriteLine(ticker.symbol + ": " + ticker.change);
				}
			}
			await ccxtAPI.Close();
		}

		static async void Example4()
		{
			CcxtAPI ccxtAPI = new CcxtAPI(@"..\..\ccxt\ccxtAPI.exe");
			// Authenticate
			await ccxtAPI.ExchangeApiKey("binance", "my_api_key");
			await ccxtAPI.ExchangeSecret("binance", "my_api_secret");
			// Place order
			Order order =  await ccxtAPI.CreateOrder("binance", "ETH/USDT", OrderType.limit, OrderSide.buy, 1, 210);
			// Cancel placed order
			await ccxtAPI.CancelOrder("binance", order.id, "ETH/USDT");
			await ccxtAPI.Close();
		}

		static async void Example5()
		{
			CcxtAPI ccxtAPI = new CcxtAPI(@"..\..\ccxt\ccxtAPI.exe");
			try
			{
				Dictionary<string, Market> markets = await ccxtAPI.LoadMarkets("_1broker");
			}
			catch (CCXTException ex)
			{
				// exception is handled by ccxt python library
				if (ex.HandledByCCXT)
					Console.WriteLine(ex.exceptionType.Value.ToString() + ex.Message);
				else
				{
					// there is a bug either in python ccxt or in CCXTSharp
					Console.WriteLine(ex.Message);
					// restart process
					ccxtAPI.Kill(); // message will popup "Failed to execute script ccxtAPI.py"
					// reinitialize and continue execution
					ccxtAPI = new CcxtAPI(@"..\..\ccxt\ccxtAPI.exe");
				}
			}
			// ...
			await ccxtAPI.Close();
		}

		static async void Test()
		{
			CcxtAPI ccxtAPI = new CcxtAPI(@"..\..\ccxt\ccxtAPI.exe");
			ccxtAPI.ShowPipeData = true;
			Console.WriteLine((await ccxtAPI.GetExchangeHas("yobit")).fetchTickers);
			var tickers = await ccxtAPI.FetchTickers("yobit");
			var list = from ticker in tickers.Values
					   orderby ticker.change
					   orderby ticker.quoteVolume
					   select ticker;
			

			foreach (var e in list)
			{
				Console.WriteLine(e.symbol + ": " + e.change + " " + e.quoteVolume);
			}
		}

		static void Main(string[] args)
		{
			Test();
			while (true)
			{
				Thread.Sleep(100);
			}
		}
	}
}
