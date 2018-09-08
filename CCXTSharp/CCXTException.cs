using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCXTSharp
{
	public class CCXTException : Exception
	{
		public ExceptionType? exceptionType { get; set; } = null;
		public string OtherType { get; set; }
		public bool HandledByCCXT { get; set; } = true;
		public enum ExceptionType
		{
			BaseError,
			ExchangeError,
			NotSupported,
			AuthenticationError,
			PermissionDenied,
			InsufficientFunds,
			InvalidAddress,
			InvalidOrder,
			OrderNotFound,
			NetworkError,
			DDoSProtection,
			RequestTimeout,
			ExchangeNotAvailable,
			InvalidNonce
		}

		public CCXTException(string message, string type) : base(message)
		{
			if(Enum.GetNames(typeof(ExceptionType)).Any((name) => { if (name == type) return true; else return false; }))
				exceptionType = (ExceptionType)Enum.Parse(typeof(ExceptionType), type);
			else
			{
				HandledByCCXT = false;
				OtherType = type;
			}
			HelpLink = "https://github.com/ccxt/ccxt/wiki/Manual#error-handling";
		}
	}
}
