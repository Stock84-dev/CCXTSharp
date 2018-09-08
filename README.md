# CCXTSharp
This is a c# wrapper for popular trading library [ccxt](https://github.com/ccxt/ccxt). Methods and documentation are pretty simmilar to [ccxt](https://github.com/ccxt/ccxt). 

## Instalation
Package manager
````
Install-Package CCXTSharp
````

## Updating ccxt library
1. Install python 3.5+ (python 3.7 isn't currentyl supported by pyinstaller). And add python to system PATH if not already added by instalation.
2. Open cmd.
3. pip install ccxt         // if libraries are already installed then type pip install ccxt --upgrade
4. pip install cfscrape

## Updating ccxtAPI program
1. Update ccxt library
2. Navigate to\your\project\ccxt\scripts in file explorer
3. shift + right click, Open command window here
4. pyinstaller -w -F ccxtAPI.py         // program will be created in dist folder
5. replace old file

## Architecture
Package contains compiled python code that has original ccxt api. C# part creates new proccess and runs compiled python code. Inter process communication is done with named pipes.


## Examples
List supported exchanges.
```
// initialize and start process
CcxtAPI ccxtAPI = new CcxtAPI(@"..\..\ccxt\ccxtAPI.exe");	 
// get list of avaliable exchanges
List<string> exchangeIds = await ccxtAPI.GetExchangIds();		
// print all exchange ids
exchangeIds.ForEach(id => Console.WriteLine(id));
// it's preferred to exit other process if your program will close
await ccxtAPI.Close();
```
List all markets.
```
// initialize and run script in python interpreter
CcxtAPI ccxtAPI = new CcxtAPI(@"..\..\ccxt\scripts\ccxtAPI.py", @"D:\Program Files (x86)\Python36-32\python.exe");  
// gets list of markets on Binance exchange and prints their symbols
List<Market> markets = await ccxtAPI.FetchMarkets("binance");
markets.ForEach(m => Console.WriteLine(m.symbol));
await ccxtAPI.Close();
```
Checking for implementation.
```
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
```
Private API.
```
CcxtAPI ccxtAPI = new CcxtAPI(@"..\..\ccxt\ccxtAPI.exe");
// Authenticate
await ccxtAPI.ExchangeApiKey("binance", "my_api_key");
await ccxtAPI.ExchangeSecret("binance", "my_api_secret");
// Place order
Order order =  await ccxtAPI.CreateOrder("binance", "ETH/USDT", OrderType.limit, OrderSide.buy, 1, 210);
// Cancel placed order
await ccxtAPI.CancelOrder("binance", order.id, "ETH/USDT");
await ccxtAPI.Close();
```
Exception handling
```
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
```



