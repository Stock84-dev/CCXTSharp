import asyncio
import ccxt
import ccxt.async_support as ccxtasync


#i = 0
#async def func():
#    for e in ccxtasync.exchanges:
#        exchange = getattr(ccxtasync,e)()
#        try:
#            has = exchange.has
#        except :
#            print(e)
#        finally:
#            await exchange.close()


#asyncio.get_event_loop().run_until_complete(func())

    #print(str(getattr(ccxt,e)().enableRateLimit))
#    i+=1

#print(i)

#ExchangeNotAvailable('zb {"result":false,"message":"\\u670d\\u52a1\\u7aef\\u5fd9\\u788c"}',)


#e = ccxt.theocean()
#print(e.name)
e = ccxt.zb()
e.verbose = True
markets = e.fetch_markets()
print(e.has)
for market in markets:
    print(e.fetch_ticker(market["symbol"]))
print("done")
input()

'''

import ccxt
import time

e = ccxt.luno()
e.enableRateLimit = True


for i in range(100):
    t = time.time()
    print(e.fetchTicker("BTC/EUR"))
    print("elapsed:" + str(time.time()-t))

input()

'''