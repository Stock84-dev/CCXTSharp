import struct
#import time
import threading
#import datetime
import platform
import socket


class Pipe:

    def __init__(self, pipeNameIn, pipeNameOut):
        self.__platform = platform.system()
        self.__lock = threading.Lock()

        if self.__platform == 'Linux':
            pipeNameIn = '/tmp/CoreFxPipe_' + pipeNameIn
            pipeNameOut = '/tmp/CoreFxPipe_' + pipeNameOut
            self.__socketIn = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
            self.__socketOut = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
            try:
                self.__socketIn.connect(pipeNameIn)
                self.__socketOut.connect(pipeNameOut)
            except socket.error as msg:
                print(msg)
        else:
            pipeNameIn = r'\\.\pipe\\' + pipeNameIn
            pipeNameOut = r'\\.\pipe\\' + pipeNameOut
            self.__pipeIn = open(pipeNameIn, 'r+b', 0)
            self.__pipeOut = open(pipeNameOut, 'r+b', 0)



    def Read(self):
        text = '' 
        amount_expected = 0
        # for some reason c# pipe writes nothing when closes and causes exception in struct
        try:
            if not (self.__platform == 'Linux'):
                #print(threading.current_thread())
                self.__lock.acquire()
                #print("read started: " + str(time.time()))
                amount_expected = struct.unpack('I', self.__pipeIn.read(4))[0]       # Read str length
                text = self.__pipeIn.read(amount_expected)                              # Read str
                self.__pipeIn.seek(0)                                  # Important!!!
                #print("read ended: " + str(time.time()))
                self.__lock.release()
                #self.start = datetime.datetime.now()
                #print(text)
            else:
                self.__lock.acquire()
                amount_expected = struct.unpack('I', self.__socketIn.recv(4))[0]
                message = self.__socketIn.recv(amount_expected)
                self.__lock.release()
                text = message.decode()
        except struct.error:
                return 'exit'
        finally:
            if self.__lock.locked():    
                self.__lock.release()
        return text
        
    def Write(self, text):
        if text is None:
            return
        if not (self.__platform == 'Linux'):
            #print("write started: " + str(time.time()))
            #print str(datetime.datetime.now()-self.start)
            self.__pipeOut.write(struct.pack('I', len(text)) + bytes(text, 'utf-8'))  # Write str length and str
            self.__pipeOut.seek(0)
            #print("write ended: " + str(time.time()))
            #print(threading.current_thread())
            #print(text)
        else:
            self.__socketOut.sendall(struct.pack('I', len(text)) + text.encode('utf-8'))

    def Close(self):
        if not (self.__platform == 'Linux'):
            self.__pipeIn.close()
            self.__pipeOut.close()
        else:
            self.__socketIn.close()
            self.__socketOut.close()
       
