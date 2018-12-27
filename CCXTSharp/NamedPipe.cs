using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCXTSharp
{
	class NamedPipe
	{
		public delegate void OnMessageEventHandler(object sender, OnMessageEventArgs e);
		public event OnMessageEventHandler OnMessage;

		private NamedPipeServerStream _serverOut;
		private NamedPipeServerStream _serverIn;
		private BinaryWriter _binaryWriter;
		private BinaryReader _binaryReader;
		private readonly object _isWritingLock = new object();
		private bool _serverOutConnected = false;

		public NamedPipe(string pipeNameIn, string pipeNameOut)
		{
			_serverOut = new NamedPipeServerStream(pipeNameOut);
			_serverIn = new NamedPipeServerStream(pipeNameIn);
			_binaryWriter = new BinaryWriter(_serverOut);
			_binaryReader = new BinaryReader(_serverIn);
			Task.Factory.StartNew(Reader, TaskCreationOptions.LongRunning);
		}
		
		public async Task Write(string str)
		{
			await Task.Run(() =>
			{
				if (!_serverOutConnected)
				{
					_serverOut.WaitForConnection();
					_serverOutConnected = true;
				}
				lock (_isWritingLock)
				{
					//Console.WriteLine("write started: " + DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
					byte[] buf = Encoding.ASCII.GetBytes(str);     // Get ASCII byte array  

					_binaryWriter.Write((uint)buf.Length);         // Write string length
					_binaryWriter.Write(buf);
					//Console.WriteLine("write ended: " + DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
				}
			});

		}

		private void Reader()
		{
			_serverIn.WaitForConnection();

			while (true)
			{
				try
				{
					//Console.WriteLine("read started: " + DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
					var len = (int)_binaryReader.ReadUInt32();            // Read string length
					var str = new string(_binaryReader.ReadChars(len));    // Read string
					// invoking on another thread so it wouldn't stop reader loop
					Task.Run(()=> OnMessage?.Invoke(this, new OnMessageEventArgs(str)));
					//Console.WriteLine("read ended: " + DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
				}
				catch (EndOfStreamException)
				{
					break;                    // When client disconnects
				}
			}

			_serverIn.Close();
			_serverIn.Dispose();
			_serverOut.Close();
			_serverOut.Dispose();
		}

		public class OnMessageEventArgs : EventArgs
		{
			public OnMessageEventArgs(string message)
			{
				Message = message;
			}

			public string Message { get; set; }
		}
	}
}
