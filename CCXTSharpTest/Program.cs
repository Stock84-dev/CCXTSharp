using CCXTSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCXTSharpTest
{
	class Program
	{
		static async void f()
		{
			CcxtAPI ccxtAPI = new CcxtAPI(@"..\..\..\CCXT\dist\ccxtAPI.exe");
			//CcxtAPI ccxtAPI = new CcxtAPI(@"D:\Documents\Visual studio 2017\Projects\CCXTSharp\CCXT\ccxtAPI.py", @"D:\Program Files (x86)\Python36-32\python.exe", true);

			var e = ccxtAPI.GetExchangIds().Result;
			e.ForEach(ex => Console.WriteLine(ex));
			await ccxtAPI.Close();
		}

		static void Main(string[] args)
		{
			f();
		}
	}
}
