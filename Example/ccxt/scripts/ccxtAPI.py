import json
import sys
import NamedPipes
import CCXTException
import asyncio
import cfscrape
import ccxt.async_support as ccxt
import os
from concurrent.futures import ThreadPoolExecutor

def run(corofn, *args):
        loop = asyncio.new_event_loop()
        try:
            coro = corofn(*args)
            asyncio.set_event_loop(loop)
            return loop.run_until_complete(coro)
        finally:
            loop.close()

async def Read(pipe):
    text = pipe.Read()
    if text == "exit":
        pipe.Close()
        os._exit(0)
    return text

class CCXTAPI:
    def __init__(self, pipeNameIn, pipeNameOut):
        self.__exchanges = {}
        self.__pipe = NamedPipes.Pipe(pipeNameIn, pipeNameOut)

    async def Run(self):
        while True:
            # reads data from other thread and awaits it
            loop = asyncio.get_event_loop()
            executor = ThreadPoolExecutor(max_workers=1)
            futures = [loop.run_in_executor(executor, run, Read, self.__pipe)]
            text = await asyncio.gather(*futures)
            try:
                data = json.loads(text[0])
                # some attributes cannot be awaited so we are switching between async and non async
                #if isinstance(getattr(self.__exchanges[data["exchange"]] load_markets, collections.Callable):
                if data["handlerType"] is 1:
                    # calls reciever which calls ccxt method based on read text but doesn't await it so it can read next data that comes through pipe
                    loop.create_task(self.recieverAsync(data))
                else:
                    self.reciever(data)
            except json.JSONDecodeError:
                continue
            except Exception as ex:
                self.__pipe.Write("error" + str(CCXTException.CCXTException(type(ex).__name__, str(ex)).__dict__) + str(data["callNumber"]))
                #logger.log(text[0])
                print("error")

           
            
    # gets called when pipe recieves data
    def reciever(self, data, retry = False):
        response = ""
        try:
            self.__addExchangeIfRequired(data["exchange"])
            # calling method with parameters or not
            if data["handlerType"] is 1:
                if data["method"]["param1"] is None:
                    response =  getattr(self.__exchanges[data["exchange"]], data["method"]["name"])()
                elif data["method"]["param2"] is None:
                    response =  getattr(self.__exchanges[data["exchange"]], data["method"]["name"])(data["method"]["param1"])
                elif data["method"]["param3"] is None:
                    response =  getattr(self.__exchanges[data["exchange"]], data["method"]["name"])(data["method"]["param1"], data["method"]["param2"])
                elif data["method"]["param4"] is None:
                    response =  getattr(self.__exchanges[data["exchange"]], data["method"]["name"])(data["method"]["param1"], data["method"]["param2"], data["method"]["param3"])
                elif data["method"]["param5"] is None:
                    response =  getattr(self.__exchanges[data["exchange"]], data["method"]["name"])(data["method"]["param1"], data["method"]["param2"], data["method"]["param3"], data["method"]["param4"])
                elif data["method"]["param6"] is None:
                    response =  getattr(self.__exchanges[data["exchange"]], data["method"]["name"])(data["method"]["param1"], data["method"]["param2"], data["method"]["param3"], data["method"]["param4"], data["method"]["param5"])
                elif data["method"]["param7"] is None:
                    response =  getattr(self.__exchanges[data["exchange"]], data["method"]["name"])(data["method"]["param1"], data["method"]["param2"], data["method"]["param3"], data["method"]["param4"], data["method"]["param5"], data["method"]["param6"])
                elif data["method"]["param8"] is None:
                    response =  getattr(self.__exchanges[data["exchange"]], data["method"]["name"])(data["method"]["param1"], data["method"]["param2"], data["method"]["param3"], data["method"]["param4"], data["method"]["param5"], data["method"]["param6"], data["method"]["param7"])
                elif data["method"]["param9"] is None:
                    response =  getattr(self.__exchanges[data["exchange"]], data["method"]["name"])(data["method"]["param1"], data["method"]["param2"], data["method"]["param3"], data["method"]["param4"], data["method"]["param5"], data["method"]["param6"], data["method"]["param7"], data["method"]["param8"])
                else:
                    response =  getattr(self.__exchanges[data["exchange"]], data["method"]["name"])(data["method"]["param1"], data["method"]["param2"], data["method"]["param3"], data["method"]["param4"], data["method"]["param5"], data["method"]["param6"], data["method"]["param7"], data["method"]["param8"], data["method"]["param9"])
            else: # setting or getting variable data that is either in exchange or in ccxt api
                if data["variable"]["type"] is 1: #set
                    if data["exchange"] == "ccxt":
                        setattr(ccxt, data["variable"]["name"], data["variable"]["data"])
                    else:
                        setattr(self.__exchanges[data["exchange"]], data["variable"]["name"], data["variable"]["data"])
                else:
                    if data["exchange"] == "ccxt":
                        response = getattr(ccxt, data["variable"]["name"])
                    else:
                        response = getattr(self.__exchanges[data["exchange"]], data["variable"]["name"])
            self.__pipe.Write(json.dumps(response) + str(data["callNumber"]))
        except ccxt.DDoSProtection as ex:
            if not retry:
                self.__addDDOSBypass(data["exchange"])
                if self.reciever(data, True) == False:
                    self.__pipe.Write("error" + str(CCXTException.CCXTException(type(ex).__name__, "DDos protection could not be bypassed.").__dict__) + str(data["callNumber"]))
            else:
                return False                
        except Exception as ex:
            self.__pipe.Write("error" + str(CCXTException.CCXTException(type(ex).__name__, str(ex)).__dict__) + str(data["callNumber"]))
            print("error")
        return True

    # gets called when pipe recieves data
    async def recieverAsync(self, data, retry = False):
        response = ""
        try:
            self.__addExchangeIfRequired(data["exchange"])
            # calling method with parameters or not
            if data["handlerType"] is 1:
                if data["method"]["param1"] is None:
                    response = await getattr(self.__exchanges[data["exchange"]], data["method"]["name"])()
                elif data["method"]["param2"] is None:
                    response = await getattr(self.__exchanges[data["exchange"]], data["method"]["name"])(data["method"]["param1"])
                elif data["method"]["param3"] is None:
                    response = await getattr(self.__exchanges[data["exchange"]], data["method"]["name"])(data["method"]["param1"], data["method"]["param2"])
                elif data["method"]["param4"] is None:
                    response = await getattr(self.__exchanges[data["exchange"]], data["method"]["name"])(data["method"]["param1"], data["method"]["param2"], data["method"]["param3"])
                elif data["method"]["param5"] is None:
                    response = await getattr(self.__exchanges[data["exchange"]], data["method"]["name"])(data["method"]["param1"], data["method"]["param2"], data["method"]["param3"], data["method"]["param4"])
                elif data["method"]["param6"] is None:
                    response = await getattr(self.__exchanges[data["exchange"]], data["method"]["name"])(data["method"]["param1"], data["method"]["param2"], data["method"]["param3"], data["method"]["param4"], data["method"]["param5"])
                elif data["method"]["param7"] is None:
                    response = await getattr(self.__exchanges[data["exchange"]], data["method"]["name"])(data["method"]["param1"], data["method"]["param2"], data["method"]["param3"], data["method"]["param4"], data["method"]["param5"], data["method"]["param6"])
                elif data["method"]["param8"] is None:
                    response = await getattr(self.__exchanges[data["exchange"]], data["method"]["name"])(data["method"]["param1"], data["method"]["param2"], data["method"]["param3"], data["method"]["param4"], data["method"]["param5"], data["method"]["param6"], data["method"]["param7"])
                elif data["method"]["param9"] is None:
                    response = await getattr(self.__exchanges[data["exchange"]], data["method"]["name"])(data["method"]["param1"], data["method"]["param2"], data["method"]["param3"], data["method"]["param4"], data["method"]["param5"], data["method"]["param6"], data["method"]["param7"], data["method"]["param8"])
                else:
                    response = await getattr(self.__exchanges[data["exchange"]], data["method"]["name"])(data["method"]["param1"], data["method"]["param2"], data["method"]["param3"], data["method"]["param4"], data["method"]["param5"], data["method"]["param6"], data["method"]["param7"], data["method"]["param8"], data["method"]["param9"])
            else: # setting or getting variable data that is either in exchange or in ccxt api
                if data["variable"]["type"] is 1: #set
                    if data["exchange"] == "ccxt":
                        await setattr(ccxt, data["variable"]["name"], data["variable"]["data"])
                    else:
                        await setattr(self.__exchanges[data["exchange"]], data["variable"]["name"], data["variable"]["data"])
                else:
                    if data["exchange"] == "ccxt":
                        response = await getattr(ccxt, data["variable"]["name"]) + str(data["callNumber"])
                    else:
                        response = await getattr(self.__exchanges[data["exchange"]], data["variable"]["name"]) + str(data["callNumber"])
            #print(json.dumps(response))
            self.__pipe.Write(json.dumps(response) + str(data["callNumber"]))
        except ccxt.DDoSProtection as ex:
            if not retry:
                self.__addDDOSBypass(data["exchange"])
                if await self.recieverAsync(data, True) == False:
                    self.__pipe.Write("error" + str(CCXTException.CCXTException(type(ex).__name__, "DDos protection could not be bypassed.").__dict__) + str(data["callNumber"]))
            else:
                return False                
        except Exception as ex:
            self.__pipe.Write("error" + str(CCXTException.CCXTException(type(ex).__name__, str(ex)).__dict__) + str(data["callNumber"]))
        return True
       

    def __addDDOSBypass(self, exchangeName):
        """
        adding async cloudflare scrapper
        from aiocfscrape import CloudflareScraper
        exchange.session = CloudflareScraper(loop=asyncio.get_event_loop())
        """
        #bypassing cloudflare with cookies
        url = self.__exchanges[exchangeName].urls['www']
        tokens, user_agent = cfscrape.get_tokens(url)
        self.__exchanges[exchangeName].headers = {
                'cookie': '; '.join([key + '=' + tokens[key] for key in tokens]),
                'user-agent': user_agent,
            }

    def __addExchangeIfRequired(self, exchangeName):
        if exchangeName not in list(self.__exchanges) and exchangeName != "ccxt":
            #assert exchangeName in ccxt.exchanges, "Exchange name is not supported!"
            exchange = getattr(ccxt, exchangeName)
            self.__exchanges[exchangeName] = exchange()


#ccxtAPI = CCXTAPI("ccxtAPICryptoWatcher-PythonPipeOut", "ccxtAPICryptoWatcher-PythonPipeIn")
ccxtAPI = CCXTAPI(sys.argv[1], sys.argv[2])
asyncio.get_event_loop().run_until_complete(ccxtAPI.Run())
print("close")

"""
{
  "handlerType": "variable",
  "variable":{
   "name":"secret" ,
   "type": "set"
  },
  "method":{
    "name":"cancel_order",
    "param1":1,
    "param2":"dd",
    "param3":1,
    "param4":1,
    "param5":1,
    "param6":1,
    "param7":1,
    "param8":1,
    "param9":1
  }, 
  "exchange":"binance"
}
"""