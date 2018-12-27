using CCXTSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCXTSharpTest
{
	class Program
	{
		static async void f()
		{
			Stopwatch sw = new Stopwatch();
			CcxtAPI ccxtAPI = new CcxtAPI(@"..\..\..\CCXT\dist\ccxtAPI.exe");
			//CcxtAPI ccxtAPI = new CcxtAPI(@"D:\Documents\Visual studio 2017\Projects\CCXTSharp\CCXT\ccxtAPI.py", @"C:\Program Files (x86)\Microsoft Visual Studio\Shared\Python36_86\python.exe", true);
			var has = await ccxtAPI.GetExchangeHas(");

			ex.ForEach(e=>Console.WriteLine(e));
			await ccxtAPI.Close();
		}

		static void Main(string[] args)
		{
			f();
			Thread.Sleep(2000);
			Console.ReadKey();
		}
	}
}
