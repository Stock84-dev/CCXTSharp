using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCXTSharp;

namespace Example
{
	class Program
	{
		// TODO: remove failed to execute script after closing program
		static void Main(string[] args)
		{
			CcxtAPI ccxtAPI = new CcxtAPI(@"..\..\ccxt\ccxtAPI.exe");
			var ids = ccxtAPI.GetExchangIds().Result;
			ids.ForEach(s => Console.WriteLine(s));
			var markets = ccxtAPI.FetchMarkets("binance").Result;
			markets.ForEach(m => Console.WriteLine(m.symbol));
			ccxtAPI.Close();
		}
	}
}
