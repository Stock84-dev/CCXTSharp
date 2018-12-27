using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

// TODO: make wrapper if user skipped to define parameter e.g. FetchTrades("binance", "BTC/USDT", limit = 10), it will call function without limit, because parameter before limit is null
namespace CCXTSharp
{
	public enum Timeframe { NONE, min1 = 60, min3 = 180, min5 = 300, min15 = 900, min30 = 1800, h1 = 3600, h2 = 7200, h4 = 14400, h6 = 21600, h8 = 28800, h12 = 43200, d1 = 86400, d3 = 259200, w1 = 604800, M1 = 2419200 }

	public partial class CcxtAPI
	{
		private readonly string PIPE_NAME_IN, PIPE_NAME_OUT;
		private readonly object _msgDataLock = new object();
		private Process _ccxtAPIProcess;
		private Dictionary<int, MessageData> _msgData = new Dictionary<int, MessageData>();
		private NamedPipe _namedPipe;
		private Dictionary<string, int> _rateLimits = new Dictionary<string, int>()
		{
			// = not provided, ds = different system
			{ "_1broker", 1500 }, // ds
			{ "_1btcxe", 2000 }, // exchange down
			{ "acx", 50 },
			{ "allcoin", 1000 }, // 
			{ "anxpro", 200 },
			{ "anybits", 2000 }, // 
			{ "bcex", 2000 }, // 
			{ "bibox", 2000 }, // 
			{ "bigone", 10 }, 
			{ "binance", 864 }, // ds
			{ "bit2c", 3000 }, // 
			{ "bitbay", 1000 }, //
			{ "bitfinex", 1500 }, // ds
			{ "bitfinex2", 1500 }, // ds
		};


		/// <summary>
		/// Starts new process with ccxt API.
		/// </summary>
		/// <param name="pathToCcxtAPIScriptOrExe">Path to script or compiled .exe.</param>
		/// <param name="pathToPythonExe">Python 3.5+ supported.</param>
		/// <param name="showPythonConsole">Show python console if you are using python interpreter.</param>
		/// <param name="pipeNameIn">Rename if you run multiple instances.</param>
		/// <param name="pipeNameOut">Rename if you run multiple instances.</param>
		public CcxtAPI(string pathToCcxtAPIScriptOrExe, string pathToPythonExe = null, bool showPythonConsole = false, string pipeNameIn = "CCXTSharp-PipeIn", string pipeNameOut = "CCXTSharp-PipeOut")
		{
			bool usePythonInterpreter = pathToPythonExe != null ? true : false;

			if (!usePythonInterpreter && showPythonConsole)
				throw new ArgumentException("Cannot show console if python interpreter isn't used.");

			if (pathToCcxtAPIScriptOrExe.EndsWith(".py") && !usePythonInterpreter)
				throw new ArgumentException("Cannot use script without python interpreter.");

			if (pathToCcxtAPIScriptOrExe.EndsWith(".exe") && usePythonInterpreter)
				throw new ArgumentException("Cannot use python interpreter on executable.");

			PIPE_NAME_IN = pipeNameIn;
			PIPE_NAME_OUT = pipeNameOut;
			ProcessStartInfo myProcessStartInfo;

			if (usePythonInterpreter)
			{
				myProcessStartInfo = new ProcessStartInfo(pathToPythonExe);
				// "-i " in front of script path to freze window after exception
				if (showPythonConsole)
					pathToCcxtAPIScriptOrExe = "-i \"" + pathToCcxtAPIScriptOrExe + "\"";
				else
				{
					myProcessStartInfo.UseShellExecute = false;
					myProcessStartInfo.CreateNoWindow = true;
					pathToCcxtAPIScriptOrExe = "\"" + pathToCcxtAPIScriptOrExe + "\"";
				}
				myProcessStartInfo.Arguments = pathToCcxtAPIScriptOrExe + " " + PIPE_NAME_OUT + " " + PIPE_NAME_IN;
			}
			else
			{
				pathToCcxtAPIScriptOrExe = "\"" + pathToCcxtAPIScriptOrExe + "\"";
				myProcessStartInfo = new ProcessStartInfo(pathToCcxtAPIScriptOrExe);
				myProcessStartInfo.Arguments = PIPE_NAME_OUT + " " + PIPE_NAME_IN;
			}

			_ccxtAPIProcess = new Process();
			// assign start information to the process 
			_ccxtAPIProcess.StartInfo = myProcessStartInfo;

			// start the process 
			_ccxtAPIProcess.Start();

			// Open the named pipe.
			_namedPipe = new NamedPipe(PIPE_NAME_IN, PIPE_NAME_OUT);
			_namedPipe.OnMessage += OnMessage;
		}

		/// <summary>
		/// Show communication messages between programs in console.
		/// </summary>
		public bool ShowPipeData { get; set; } = false;

		/// <summary>
		/// Closes communication between python ccxt.
		/// </summary>
		public async Task Close()
		{
			await _namedPipe.Write("exit");

			lock (_msgDataLock)
			{
				foreach (var msg in _msgData.Values)
				{
					Task.Run(() => msg.TaskCompleted.SetResult(false));
				}
			}
		}
		/// <summary>
		/// Use this only if python program breaks.
		/// </summary>
		public void Kill()
		{
			_ccxtAPIProcess.Kill();
		}

		/// <summary>
		/// Returns a list of exchange ids.
		/// </summary>
		public async Task<List<string>> GetExchangeIds()
		{
			return await GetData<List<string>>("exchanges", "ccxt", null, false);
		}

		#region Exchange properties
		public async Task<string> GetExchangeName(string exchnageId)
		{
			return await GetData<string>("name", exchnageId, null, false);
		}

		public async Task<List<string>> GetExchangeCountries(string exchnageId)
		{
			return await GetData<List<string>>("countries", exchnageId, null, false);
		}

		public async Task<Has> GetExchangeHas(string exchnageId)
		{
			var data = await GetData<Dictionary<string, object>>("has", exchnageId, null, false);
			Has has = new Has();
			foreach (var property in has.GetType().GetProperties())
			{
				foreach (var capability in data)
				{
					if (capability.Key == property.Name)
					{
						if (capability.Value.GetType() == typeof(string))
							property.SetValue(has, Has.Capability.Emulated);
						else if ((bool)capability.Value == true)
							property.SetValue(has, Has.Capability.True);
						else
							property.SetValue(has, Has.Capability.False);
					}
				}
			}
			return has;
		}

		public async Task<Dictionary<string, string>> GetExchangeTimeframes(string exchnageId)
		{
			return await GetData<Dictionary<string, string>>("timeframes", exchnageId, null, false);
		}

		public async Task<int> ExchangeTimeout(string exchnageId, int? timeout = null)
		{
			return await GetData<int>("timeout", exchnageId, timeout, false);
		}

		public async Task<int> ExchangeRateLimit(string exchnageId, int? rateLimit = null)
		{
			return await GetData<int>("rateLimit", exchnageId, rateLimit, false);
		}

		public async Task<bool> ExchangeVerbose(string exchnageId, bool? verbose = null)
		{
			return await GetData<bool>("verbose", exchnageId, verbose, false);
		}

		public async Task<Dictionary<string, Market>> GetExchangeMarkets(string exchnageId)
		{
			return await GetData<Dictionary<string, Market>>("markets", exchnageId, null, false);
		}

		/// <summary>
		/// Returns orders cached by ccxt.
		/// </summary>
		public async Task<List<Order>> GetExchangeOrders(string exchnageId)
		{
			return await GetData<List<Order>>("orders", exchnageId, null, false);
		}

		public async Task<List<string>> GetExchangeSymbols(string exchnageId)
		{
			return await GetData<List<string>>("symbols", exchnageId, null, false);
		}

		public async Task<Dictionary<string, Market>> GetExchangeCurrencies(string exchnageId)
		{
			return await GetData<Dictionary<string, Market>>("currencies", exchnageId, null, false);
		}

		public async Task<Dictionary<string, Market>> GetExchangeMarketsById(string exchnageId)
		{
			return await GetData<Dictionary<string, Market>>("markets_by_id", exchnageId, null, false);
		}

		public async Task<string> ExchangeProxy(string exchnageId, string proxy = null)
		{
			return await GetData<string>("proxy", exchnageId, proxy, false);
		}

		public async Task<string> ExchangeApiKey(string exchnageId, string apiKey = null)
		{
			return await GetData<string>("apiKey", exchnageId, apiKey, false);
		}

		public async Task<string> ExchangeSecret(string exchnageId, string secret = null)
		{
			return await GetData<string>("secret", exchnageId, secret, false);
		}

		public async Task<string> ExchangePassword(string exchnageId, string password = null)
		{
			return await GetData<string>("password", exchnageId, password, false);
		}

		public async Task<string> ExchangeUserId(string exchnageId, string uId = null)
		{
			return await GetData<string>("uid", exchnageId, uId, false);
		}

		public async Task<bool> ExchangeEnableRateLimit(string exchnageId, bool? enambeRateLimit = null)
		{
			return await GetData<bool>("rateLimit", exchnageId, enambeRateLimit, false);
		}

		/// <summary>
		/// Clears .orders cache before millisecond timestamp.
		/// </summary>
		public async Task<string> ExchangePurgeCachedOrders(string exchnageId, long before)
		{
			return await GetData<string>("purgeCachedOrders", exchnageId, null, true, -1, false, before);
		}

		#endregion

		#region Public API
		public async Task<Dictionary<string, Market>> LoadMarkets(string exchange, bool reloadCache = false)
		{
			return await GetData<Dictionary<string, Market>>("load_markets", exchange, methodParameters: reloadCache);
		}

		public async Task<List<Market>> FetchMarkets(string exchangeId)
		{
			return await GetData<List<Market>>("fetch_markets", exchangeId);
		}

		/// <summary>
		/// Returns empty list if load markets hasn't been called.
		/// </summary>
		public async Task<Dictionary<string, Currency>> FetchCurrencies(string exchangeId)
		{
			return await GetData<Dictionary<string, Currency>>("fetch_currencies", exchangeId);
		}

		public async Task<Ticker> FetchTicker(string exchangeId, string symbol)
		{
			return await GetData<Ticker>("fetch_ticker", exchangeId, methodParameters: symbol);
		}

		/// <summary>
		/// Some exchanges doesn't support to fetch all tickers.
		/// </summary>
		public async Task<Dictionary<string, Ticker>> FetchTickers(string exchangeId)
		{
			return await GetData<Dictionary<string, Ticker>>("fetch_tickers", exchangeId);
		}

		/// <summary>
		/// Parameters are exchange specific and aren't unified.
		/// </summary>
		public async Task<OrderBook> FetchOrderBook(string exchangeId, string symbol, int? limit = null, Dictionary<string, object> parameters = null)
		{
			return new OrderBook(await GetData<string>("fetch_order_book", exchangeId, null, true, -1, false, symbol, limit, parameters));
		}

		/// <summary>
		/// Returns aggregated orderbook. Parameters are exchange specific and aren't unified.
		/// </summary>
		public async Task<OrderBook> FetchL2OrderBook(string exchangeId, string symbol, int? limit = null, Dictionary<string, object> parameters = null)
		{
			return new OrderBook(await GetData<string>("fetchL2OrderBook", exchangeId, null, true, -1, false, symbol, limit, parameters));
		}

		public async Task<List<Candlestick>> FetchOHLCV(string exchangeId, string symbol, Timeframe? timeframe = null, long? since = null, int? limit = null, Dictionary<string, object> parameters = null)
		{
			string timeframeKey = timeframe != null ? TimeframeToKey(timeframe.Value) : null;
			var response = JObject.Parse(await GetData<string>("fetchOHLCV", exchangeId, null, true, -1, false, symbol, timeframeKey, since, limit, parameters));
			return (from responseCandle in response.Children()
					select new Candlestick(responseCandle[0].ToObject<long>(), responseCandle[1].ToObject<float>(), responseCandle[2].ToObject<float>(), responseCandle[3].ToObject<float>(), responseCandle[4].ToObject<float>(), responseCandle[5].ToObject<float>())).ToList();

		}

		public async Task<List<Candlestick>> FetchOHLCV(string exchangeId, string symbol, string timeframe = null, long? since = null, int? limit = null, Dictionary<string, object> parameters = null)
		{
			string text = await GetData<string>("fetchOHLCV", exchangeId, null, true, -1, false, symbol, timeframe, since, limit, parameters);
			var response = JArray.Parse(text);
			return (from responseCandle in response
					select new Candlestick(responseCandle[0].ToObject<long>(), responseCandle[1].ToObject<float>(), responseCandle[2].ToObject<float>(), responseCandle[3].ToObject<float>(), responseCandle[4].ToObject<float>(), responseCandle[5].ToObject<float>())).ToList();

		}

		public async Task<List<Trade>> FetchTrades(string exchangeId, string symbol, long? since = null, int? limit = null, Dictionary<string, object> parameters = null)
		{
			return await GetData<List<Trade>>("fetch_trades", exchangeId, null, true, -1, true, symbol, since, limit, parameters);
		}

		#endregion

		#region Private API
		public async Task<Balances> FetchBalance(string exchangeId)
		{
			return new Balances(await GetData<string>("fetch_balance", exchangeId, null, true, -1, false));
		}

		public async Task<Order> CreateOrder(string exchangeId, string symbol, OrderType type, OrderSide side, float amount, float price, Dictionary<string, object> parameters = null)
		{
			return await GetData<Order>("create_order", exchangeId, null, true, -1, true, symbol, type.ToString(), side.ToString(), amount, price, parameters);
		}

		public async Task<Order> CancelOrder(string exchangeId, string id, string symbol, Dictionary<string, object> parameters = null)
		{
			return await GetData<Order>("cancel_order", exchangeId, null, true, -1, true, id, symbol, parameters);
		}

		public async Task<Order> FetchOrder(string exchangeId, string id, string symbol, Dictionary<string, object> parameters = null)
		{
			return await GetData<Order>("fetch_order", exchangeId, null, true, -1, true, id, symbol, parameters);
		}

		/// <summary>
		/// Fetching orders without specifying a symbol is rate-limited.
		/// </summary>
		public async Task<List<Order>> FetchOrders(string exchangeId, string symbol = null, long? since = null, int? limit = null, Dictionary<string, object> parameters = null)
		{
			return await GetData<List<Order>>("fetch_orders", exchangeId, null, true, -1, true, symbol, since, limit, parameters);
		}

		/// <summary>
		/// Fetching orders without specifying a symbol is rate-limited.
		/// </summary>
		public async Task<List<Order>> FetchOpenOrders(string exchangeId, string symbol = null, long? since = null, int? limit = null, Dictionary<string, object> parameters = null)
		{
			return await GetData<List<Order>>("fetchOpenOrders", exchangeId, null, true, -1, true, symbol, since, limit, parameters);
		}

		/// <summary>
		/// Fetching orders without specifying a symbol is rate-limited.
		/// </summary>
		public async Task<List<Order>> FetchClosedOrders(string exchangeId, string symbol = null, long? since = null, int? limit = null, Dictionary<string, object> parameters = null)
		{
			return await GetData<List<Order>>("fetchClosedOrders", exchangeId, null, true, -1, true, symbol, since, limit, parameters);
		}

		/// <summary>
		/// Fetching orders without specifying a symbol is rate-limited.
		/// </summary>
		public async Task<List<Trade>> FetchMyTrades(string exchangeId, string symbol, long? since = null, int? limit = null, Dictionary<string, object> parameters = null)
		{
			return await GetData<List<Trade>>("fetchMyTrades", exchangeId, null, true, -1, true, symbol, since, limit, parameters);
		}

		/// <param name="baseAsset">Can be found in Market.Base</param>
		public async Task<Address> FetchDepositAddress(string exchangeId, string baseAsset, Dictionary<string, object> parameters = null)
		{
			return await GetData<Address>("fetchDepositAddress", exchangeId, null, true, -1, true, baseAsset, parameters);
		}

		public async Task<Withdrawal> Withdraw(string exchangeId, string baseAsset, float amount, string address, string tag = null, Dictionary<string, object> parameters = null)
		{
			return await GetData<Withdrawal>("withdraw", exchangeId, null, true, -1, true, baseAsset, amount, address, tag, parameters);
		}

		public async Task<List<Transaction>> FetchDeposits(string exchangeId, string baseAsset = null, long? since = null, int? limit = null, Dictionary<string, object> parameters = null)
		{
			return await GetData<List<Transaction>>("fetchDeposits", exchangeId, null, true, -1, true, baseAsset, parameters);
		}

		public async Task<List<Transaction>> FetchWithdrawals(string exchangeId, string baseAsset = null, long? since = null, int? limit = null, Dictionary<string, object> parameters = null)
		{
			return await GetData<List<Transaction>>("fetchWithdrawals", exchangeId, null, true, -1, true, baseAsset, parameters);
		}

		public async Task<List<Transaction>> FetchTransactions(string exchangeId, string baseAsset = null, long? since = null, int? limit = null, Dictionary<string, object> parameters = null)
		{
			return await GetData<List<Transaction>>("fetchTransactions", exchangeId, null, true, -1, true, baseAsset, parameters);
		}
		#endregion

		/// <summary>
		/// Gets wait time in ms after each call it's less restictive than original ccxt raleLimit.
		/// </summary>
		//public Dictionary<string,int> RateLimits { get { return _rateLimits; } }

		public static string TimeframeToKey(Timeframe timeframe)
		{
			switch (timeframe)
			{
				case Timeframe.min1: return "1m";
				case Timeframe.min5: return "5m";
				case Timeframe.min15: return "15m";
				case Timeframe.min30: return "30m";
				case Timeframe.h1: return "1h";
				case Timeframe.h2: return "2h";
				case Timeframe.h4: return "4h";
				case Timeframe.h6: return "6h";
				case Timeframe.h8: return "8h";
				case Timeframe.h12: return "12h";
				case Timeframe.d1: return "1d";
				case Timeframe.d3: return "3d";
				case Timeframe.w1: return "1w";
				case Timeframe.M1: return "1M";
			}
			throw new ArgumentException();
		}

		/// <summary>
		/// Use this to request data from ccxt that isn't implemented in c#.
		/// </summary>
		/// <typeparam name="T">Message is automatically deserialized to desired object.</typeparam>
		/// <param name="name">Name of a method, variable to call.</param>
		/// <param name="parentName">Name of an object that contains method or variable to call.</param>
		/// <param name="callNumber">Used for awaiting asynchronous code, You probably don't need this.</param>
		/// <returns>Deserialized object.</returns>
		public async Task<T> GetData<T>(string name, string parentName, object setVariableData = null, bool isMethod = true, int callNumber = -1, bool deserialize = true, params object[] methodParameters)
		{
			if (callNumber == -1)
				callNumber = AddMessageToQueue();
			Message msg = new Message(name, parentName, callNumber, setVariableData, isMethod, methodParameters);
			await _namedPipe.Write(JsonConvert.SerializeObject(msg));
			if (deserialize)
				return JsonConvert.DeserializeObject<T>(CheckForError(await GetMessageData(msg.callNumber)));
			else
				return (T)Convert.ChangeType(CheckForError(await GetMessageData(msg.callNumber)), typeof(T));
		}

		/// <summary>
		/// Throws CCXTException if error is found, else returns message.
		/// </summary>
		private string CheckForError(MessageData messageData)
		{
			if (_ccxtAPIProcess.HasExited)
				throw new CCXTException("Trying to write while CCXTAPI is closed.", "ComunicationClosed");
			if (messageData.IsError)
			{
				var e = JsonConvert.DeserializeObject<ExceptionMessage>(messageData.Msg);
				throw new CCXTException(e.message, e.exceptionType);
			}
			else return messageData.Msg;
		}

		private async Task<MessageData> GetMessageData(int callNumber)
		{
			MessageData msgData;
			await _msgData[callNumber].TaskCompleted.Task;
			lock (_msgDataLock)
			{
				msgData = new MessageData(_msgData[callNumber].Msg, _msgData[callNumber].TaskCompleted, _msgData[callNumber].IsError);
				_msgData.Remove(callNumber);
			}
			return msgData;
		}

		/// <summary>
		/// Adds message to _msgData dictionary and returns key(call number).
		/// </summary>
		private int AddMessageToQueue()
		{
			lock (_msgDataLock)
			{
				for (int i = 0; i < int.MaxValue; i++)
				{
					if (!_msgData.ContainsKey(i))
					{
						_msgData.Add(i, new MessageData());
						return i;
					}
				}
			}
			throw new Exception("Not enough space in queue.");
		}

		private void OnMessage(object sender, NamedPipe.OnMessageEventArgs e)
		{
			if(ShowPipeData)
				Console.WriteLine(e.Message);
			int i;
			for (i = e.Message.Length - 1; i >= 0; i--)
			{
				if (!(e.Message[i] >= '0' && e.Message[i] <= '9'))
				{
					i++;
					break;
				}
			}
			int callNumber = int.Parse(e.Message.Substring(i));
			// if error occurred
			lock (_msgDataLock)
			{
				if (e.Message.IndexOf('{') != 0 && e.Message.IndexOf('[') != 0 && e.Message.IndexOf('\"') != 0)
				{
					_msgData[callNumber].Msg = e.Message.Substring(5, i - 5);
					_msgData[callNumber].IsError = true;
				}
				else _msgData[callNumber].Msg = e.Message.Substring(0, i);
			}
			_msgData[callNumber].TaskCompleted.SetResult(true);
		}

		private class MessageData
		{
			public MessageData() { }

			public MessageData(string msg, TaskCompletionSource<bool> taskCompleted, bool isError)
			{
				Msg = msg;
				TaskCompleted = taskCompleted;
				IsError = isError;
			}

			public string Msg { get; set; } = null;
			public TaskCompletionSource<bool> TaskCompleted { get; set; } = new TaskCompletionSource<bool>();
			public bool IsError { get; set; } = false;
		}

		private struct ExceptionMessage
		{
			public string exceptionType { get; set; }
			public string message { get; set; }
		}
	}

	public class Transaction
	{
		public string id { get; set; }
		public string txid { get; set; }
		public long? timestamp { get; set; }
		public DateTime? datetime { get; set; }
		public string address { get; set; }
		public string type { get; set; }
		public TransactionType transactionType { get { return (TransactionType)Enum.Parse(typeof(TransactionType), type); } }
		public float? amount { get; set; }
		public string currency { get; set; }
		public string status { get; set; }
		public TransactionStatus transactionStatus { get { return (TransactionStatus)Enum.Parse(typeof(TransactionStatus), status); } }
		public long? updated { get; set; }
		public Fee fee { get; set; }

		[JsonExtensionData]
		public Dictionary<string, object> info { get; set; }

		public enum TransactionType { deposit, withdrawal }
		public enum TransactionStatus { ok, failed, canceled}
	}

	public class Address
	{
		public string currency { get; set; }
		public string address { get; set; }
		public string tag { get; set; }
		[JsonExtensionData]
		public Dictionary<string, object> info { get; set; }
	}

	public class Withdrawal
	{
		public string id { get; set; }
		[JsonExtensionData]
		public Dictionary<string, object> info { get; set; }
	}

	public enum OrderSide { buy, sell }
	public enum OrderType { limit, market }
	public enum OrderStatus { open, closed, canceled }

	public class Order
	{
		public string id { get; set; }
		public long? timestamp { get; set; }
		public DateTime? datetime { get; set; }
		public long? lastTradeTimestamp { get; set; }
		public string symbol { get; set; }
		public string type { get; set; }
		public string side { get; set; }
		public float? price { get; set; }
		public float? amount { get; set; }
		public float? cost { get; set; }
		public float? filled { get; set; }
		public float? remaining { get; set; }
		public string status { get; set; }
		public OrderStatus OrderStatus { get { return (OrderStatus)Enum.Parse(typeof(OrderStatus), status); } }
		public Fee fee { get; set; }
		//TODO: unknown type
		public List<object> trades { get; set; }
		[JsonExtensionData]
		public Dictionary<string, object> info { get; set; }
	}

	public class Candlestick
	{
		[JsonProperty("time")]
		public long Timestamp { get; set; }
		public float open { get; set; }
		public float high { get; set; }
		public float low { get; set; }
		public float close { get; set; }
		/// <summary>
		/// Volume in quote currency.
		/// </summary>
		[JsonProperty("volumefrom")]
		public float volume { get; set; }

		public Candlestick(long timestamp, float open, float high, float low, float close, float volume)
		{
			this.Timestamp = timestamp;
			this.open = open;
			this.high = high;
			this.low = low;
			//if(close.HasValue)
			this.close = close;
			//else this.close
			this.volume = volume;
		}

		public static Candlestick operator *(Candlestick a, Candlestick b)
		{
			return new Candlestick(a.Timestamp, a.open * b.open, a.high * b.high, a.low * b.low, a.close * b.close, a.volume);
		}
	}

	public class Trade
	{
		public long? timestamp { get; set; }
		public DateTime? datetime { get; set; }
		public string symbol { get; set; }
		public string id { get; set; }
		public string order { get; set; }
		public string type { get; set; }
		public string takerOrMaker { get; set; }
		public string side { get; set; }
		public float? price { get; set; }
		public float? cost { get; set; }
		public float? amount { get; set; }
		public Fee fee { get; set; }
		[JsonExtensionData]
		public Dictionary<string, object> info { get; set; }
	}

	public class Fee
	{
		public float? cost { get; set; }
		public string currency { get; set; }
		public float? rate { get; set; }
	}

	public class Has
	{
		public Capability publicAPI { get; set; }
		public Capability privateAPI { get; set; }
		public Capability CORS { get; set; }
		public Capability cancelOrder { get; set; }
		public Capability cancelOrders { get; set; }
		public Capability createDepositAddress { get; set; }
		public Capability createOrder { get; set; }
		public Capability createMarketOrder { get; set; }
		public Capability createLimitOrder { get; set; }
		public Capability deposit { get; set; }
		public Capability editOrder { get; set; }
		public Capability fetchBalance { get; set; }
		public Capability fetchClosedOrders { get; set; }
		public Capability fetchCurrencies { get; set; }
		public Capability fetchDepositAddress { get; set; }
		public Capability fetchDeposits { get; set; }
		public Capability fetchFundingFees { get; set; }
		public Capability fetchL2OrderBook { get; set; }
		public Capability fetchMarkets { get; set; }
		public Capability fetchMyTrades { get; set; }
		public Capability fetchOHLCV { get; set; }
		public Capability fetchOpenOrders { get; set; }
		public Capability fetchOrder { get; set; }
		public Capability fetchOrderBook { get; set; }
		public Capability fetchOrderBooks { get; set; }
		public Capability fetchOrders { get; set; }
		public Capability fetchTicker { get; set; }
		public Capability fetchTickers { get; set; }
		public Capability fetchTrades { get; set; }
		public Capability fetchTradingFees { get; set; }
		public Capability fetchTradingLimits { get; set; }
		public Capability fetchTransactions { get; set; }
		public Capability fetchWithdrawals { get; set; }
		public Capability withdraw { get; set; }

		public enum Capability { True, False, Emulated }
	}

	public class OrderBook
	{
		public List<BookedOrder> bids { get; set; }
		public List<BookedOrder> asks { get; set; }
		public long? timestamp { get; set; }
		public DateTime? datetime { get; set; }
		public long? nonce { get; set; }

		public OrderBook(string json)
		{
			JObject jObject = JObject.Parse(json);
			timestamp = jObject["timestamp"].ToObject<long?>();
			datetime = jObject["datetime"].ToObject<DateTime?>();
			nonce = jObject["nonce"].ToObject<long?>();
			asks = new List<BookedOrder>();
			bids = new List<BookedOrder>();

			foreach (var ask in jObject["asks"])
				asks.Add(new BookedOrder(ask[1].ToObject<float>(), ask[0].ToObject<float>()));

			foreach (var bid in jObject["asks"])
				bids.Add(new BookedOrder(bid[1].ToObject<float>(), bid[0].ToObject<float>()));
		}

		public class BookedOrder
		{
			public BookedOrder(float? price, float? amount)
			{
				this.price = price;
				this.amount = amount;
			}

			public float? price { get; set; }
			public float? amount { get; set; }
		}
	}

	public class Ticker
	{
		public string symbol { get; set; }
		public long? timestamp { get; set; }
		public DateTime? datetime { get; set; }
		public float? high { get; set; }
		public float? low { get; set; }
		public float? bid { get; set; }
		public float? bidVolume { get; set; }
		public float? ask { get; set; }
		public float? askVolume { get; set; }
		public float? vwap { get; set; }
		public float? open { get; set; }
		public float? close { get; set; }
		public float? last { get; set; }
		public float? previousClose { get; set; }
		public float? change { get; set; }
		public float? percentage { get; set; }
		public float? average { get; set; }
		public float? baseVolume { get; set; }
		public float? quoteVolume { get; set; }
		[JsonExtensionData]
		public Dictionary<string, object> info { get; set; }
	}

	public class Currency
	{
		public int? id { get; set; }
		public int? numericId { get; set; }
		public string code { get; set; }
		public int? precision { get; set; }
	}
}
