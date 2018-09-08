import struct
#import time
import threading
#import datetime

class Pipe:

    def __init__(self, pipeNameIn, pipeNameOut):
        self.__pipeIn = open(r'\\.\pipe\\' + pipeNameIn, 'r+b', 0)
        self.__pipeOut = open(r'\\.\pipe\\' + pipeNameOut, 'r+b', 0)
        self.lock = threading.Lock()
           

    def Read(self):
        #print(threading.current_thread())
        self.lock.acquire()
        #print("read started: " + str(time.time()))
        n = struct.unpack('I', self.__pipeIn.read(4))[0]       # Read str length
        text = self.__pipeIn.read(n)                              # Read str
        self.__pipeIn.seek(0)                                  # Important!!!
        #print("read ended: " + str(time.time()))
        self.lock.release()
        #self.start = datetime.datetime.now()
        #print(text)
        return text
        
    def Write(self, text):
        if text is not None:
            #print("write started: " + str(time.time()))
            #print str(datetime.datetime.now()-self.start)
            self.__pipeOut.write(struct.pack('I', len(text)) + bytes(text, 'utf-8'))  # Write str length and str
            self.__pipeOut.seek(0)
            #print("write ended: " + str(time.time()))
        #print(threading.current_thread())
        #print(text)
       