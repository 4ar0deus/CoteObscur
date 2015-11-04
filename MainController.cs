using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;







/*
struct Operators:IDisposable {
    bool isMobileConnected, isStatConnected, isObserverConnected, isError;
    Socket mobileController;                                                        //    socket for mobile controller
    Socket   statController;                                                        // socket for statistic controller
    Socket         observer;                                                        // socket for main controller daemon 
    Socket     _errorSocket;                                                        // null socket
    public void add(string key, Socket _socket) {                                   // add new socket by reserved key
        switch ( key ) {
            case   "mobile":    if ( !isMobileConnected ) {
                                     mobileController = _socket;
                                       isMobileConnected = true;
                }
                                                          break;
            case     "stat":      if ( !isStatConnected ) {
                                       statController = _socket;
                                         isStatConnected = true;
                }
                                                          break;
            case "observer":  if ( !isObserverConnected ) {
                                             observer = _socket;
                                     isObserverConnected = true;
                }
                                                          break;
          }
    }
    public Socket get(string key) {                                                 // get socket by key
        switch ( key ) {
            case   "mobile": return mobileController;
            case     "stat": return   statController;
            case "observer": return         observer;
            default:         return     _errorSocket;
        }
    }
    public void reuse(string key) {                                                 // disconnect socket for second using
        switch ( key ) {
            case   "mobile": mobileController.Disconnect(true); break;
            case     "stat":   statController.Disconnect(true); break;
            case "observer":         observer.Disconnect(true); break;
        }
    }
    public void clear() {                                                           // clear all struct
        if (mobileController != null)
                  mobileController.Close();
          if (statController != null)
                    statController.Close();
                 if (observer !=null)
                          observer.Close();
        isObserverConnected = false;
            isStatConnected = false;
          isMobileConnected = false;
    }
    public int  update() {                                                          // checked offline object
        int clientDisconnectCount = 0;
        if ( !mobileController.Connected ) {
                     isMobileConnected = false;
                       ++clientDisconnectCount;
        }
        if ( !statController.Connected ) {
                       isStatConnected = false;
                       ++clientDisconnectCount;
        }
        if ( !observer.Connected ) {
                   isObserverConnected = false;
                       ++clientDisconnectCount;
        }
                  return clientDisconnectCount;
    }
    public void Dispose() {                                                         // dispose memory resource, clear struct
        clear();
    }
}

class RMLauncher {
      static TcpListener _mainListener;                                             // main TcpListener class
      static Operators  _mainOperators;                                             // operators
      Socket _bufferSocket;                                             // buffer for any connection
    byte[] socketHandler;
     int handlerSize = 1023;

    public void killAll() {
          _mainListener.Stop();
        _mainOperators.clear();
    }

    public RMLauncher(int port) {
               Debug.Log("Prepare start on:" + IPAddress.Any + ":" + port);
        try {
                      _mainListener = new TcpListener(IPAddress.Any, port);                                                          
                                                     _mainListener.Start();
       }
        catch ( SocketException exep ) {
                            Debug.Log("Don't start server on this param;");
                          Debug.Log(StackTraceUtility.ExtractStackTrace());
                                                Debug.Log(exep.StackTrace);
                                                        Application.Quit();
        }
       Debug.Log("Server mounted and wait client:");                                 // complete intialization
              socketHandler = new byte[handlerSize];
    }
#region AcceptReceiverTools
    string handler, cpHandler;
    void hs_asyncReceive(IAsyncResult iar) {
       // try {
             RMLauncher rm = (RMLauncher) iar.AsyncState;
            Socket _temp = rm._bufferSocket;
            int recSize = _temp.EndReceive(iar); //_temp.EndReceive(iar);
            cpHandler = handler;
            handler = Encoding.UTF8.GetString(socketHandler, 0, recSize);
            Debug.Log("Прочитано:" + recSize+ " Handler:"+handler);
     
       // }
    //    catch ( SocketException exep ) {
     //            Debug.Log("Receive error on any situation");
     //       Debug.Log(StackTraceUtility.ExtractStackTrace());

     //   }
} 
    void acceptReceiver(Socket _bufferSocket) {
        _bufferSocket.BeginReceive(socketHandler, 0, handlerSize, 0, new AsyncCallback(hs_asyncReceive), this);
    }
#endregion AcceptReceiverTools
    void hs_asyncAccept(IAsyncResult iar) {
        _bufferSocket = _mainListener.EndAcceptSocket(iar);
                        Debug.Log("Client wait accepting");
                    //         acceptReceiver(_bufferSocket);
        //Debug.Log("Прочитан хандлер: " + handler + " " + handler.Length);
                             //  if (handler != cpHandler ) {
               // _mainOperators.add(handler, _bufferSocket);
                 //  Debug.Log("Client accepter:" + handler);
   //     }               
    }
    public void startAccepter() {
        _mainListener.BeginAcceptSocket(new AsyncCallback(hs_asyncAccept), _mainListener);
        if ( _bufferSocket != null ) {
            acceptReceiver(_bufferSocket);
            Debug.Log("Прочитан хандлер: " + handler + " " + handler.Length);
        }
    }
  
}

class MainController: MonoBehaviour {
    RMLauncher _launcher;
    void Start() {
        _launcher = new RMLauncher(27015);
        _launcher.startAccepter();
        
    }
    void Update() {
      
    }
    void OnApplicationQuit() {
        _launcher.killAll();
    }
}



class LauncherServer {                                                                                           // main class for local server
    static TcpListener _mainListener;                                                                                   // server object
    static Socket[] _clientSocket;                                                                                       // client socket
    static Socket _statSocket;
    bool isClientConnected = false;                                                                              // if true - begin receive data
    byte[] recMessage = new byte[1024];                                                                          // receive byte buffer
    string outBuffer = "";                                                                                       // string buffer for return value
    int clientCount = 0;

    public LauncherServer(int port) {                                                                            // start server
        Debug.Log("Server started on:" + IPAddress.Any + ":" + port);
        _clientSocket = new Socket[2];
        _mainListener = new TcpListener(IPAddress.Any, port);                                                    // creating TcpListener
        _mainListener.Start();                                                                                   // start listening
        Debug.Log("Server mounted and wait client.");
    }
    ~LauncherServer() {
        _mainListener.Stop();
        _clientSocket[0].Close();
        _clientSocket[1].Close();
    }
    public void startAccepter() {
        _mainListener.BeginAcceptSocket(new AsyncCallback(asyncAccept), _mainListener);
    }

    public void receive() {                                                                                      // if client connected, begin receive
        if ( isClientConnected )                                                                                 // run this method on update func
            _clientSocket[clientCount].BeginReceive(recMessage, 0, 1024, 0, new AsyncCallback(asyncRec), _clientSocket[clientCount]);      // start async begin func 
    }
    void asyncAccept(IAsyncResult iar) {
        if ( clientCount == 0 ) {
            _clientSocket[clientCount] = _mainListener.EndAcceptSocket(iar);
            isClientConnected = true;
            Debug.Log("Client count:" + _clientSocket[0].RemoteEndPoint);
            startAccepter();
        }
        if ( clientCount == 1 ) {
            ++clientCount;
            _clientSocket[clientCount] = _mainListener.EndAcceptSocket(iar);
            Debug.Log("Client count:" + _clientSocket[1].RemoteEndPoint);
        }


        /*
        if ( clientCount <= 2 ) {
            _clientSocket[clientCount] = _mainListener.EndAcceptSocket(iar);
            Debug.Log("Client count:" + _clientSocket[clientCount].RemoteEndPoint.AddressFamily);
            ++clientCount;
           _mainListener.BeginAcceptSocket(new AsyncCallback(asyncAccept), _mainListener);
        }
        else {
            _mainListener.EndAcceptSocket(iar);
        }
        ////////

    }
    void asyncRec(IAsyncResult iar) {
        try {
            Socket client = (Socket)iar.AsyncState;
            int recSize = client.EndReceive(iar);
            outBuffer = Encoding.UTF8.GetString(recMessage, 0, recSize);                                              // encoding byte array to string 
            if ( recSize == 0 ) {
                _mainListener.BeginAcceptSocket(new AsyncCallback(asyncAccept), _mainListener);

            }
        }
        catch ( SocketException exp ) {
            Debug.Log("Receive error, wait recconection");
            _mainListener.BeginAcceptSocket(new AsyncCallback(asyncAccept), _mainListener);                          // start async accept func
        }
    }
    public void sendMessage(string _msg) {
        _clientSocket[1].Send(Encoding.UTF8.GetBytes(_msg));
    }

    public void watchDog() {

    }

    public string getMessage() {
        string pvrBuf = outBuffer;
        outBuffer = "";

        return pvrBuf;
    }
}


struct gameStatus {
    public bool isStarted;
    public bool isStopped;
    public bool isReceiveStat;
}


public class MainController : MonoBehaviour {
    public GameObject saveObject;
    static bool isInit = false;
    static bool isReceiveStarted = false;
    static LauncherServer ls;                                                                                              // declare server variable
    static CommandLet cl;
    string bufferString = " ";
    void Start() {
        if ( !isInit ) {
            DontDestroyOnLoad(this);
            ls = new LauncherServer(27015);                                                                             // start serve
            cl = new CommandLet();
            isInit = true;
        }
    }


    bool isAccepter = false;

    void Update() {
        if ( !isAccepter ) {
            ls.startAccepter();
            isAccepter = true;
        }

        if ( !isReceiveStarted ) {
            ls.receive();
            isReceiveStarted = false;
        }// start receiver

        bufferString = ls.getMessage();
        Debug.Log(bufferString);
        if ( bufferString.Length > 0 ) {
            if ( cl.parseCommand(bufferString) == true )
                if ( bufferString == "MC_STAT" ) {
                    ls.sendMessage(bufferString);
                }
        }
        bufferString = null;
    }

}

class CommandLet {
    gameStatus gs = new gameStatus();
    void StartGame() {
        if ( !gs.isStarted ) {
            // Debug.Log("Game starting...");
            Application.LoadLevel("Game");

            gs.isStarted = true;
            gs.isStopped = false;
        }
    }

    void StopGame() {
        if ( !gs.isStopped && gs.isStarted ) {
            // Debug.Log("Game exit...");
            Application.LoadLevel("base");
            gs.isStarted = false;
            gs.isStopped = true;

        }
    }

    void ReceiveStats() {
        if ( !gs.isReceiveStat ) {

            gs.isReceiveStat = true;
        }
    }

    public bool parseCommand(string _msg) {
        switch ( _msg ) {
            case "MC_START":
                Debug.Log("Parse command");
                StartGame();
                return true;
            case "MC_STOP":
                StopGame();
                return true;
            case "MC_STAT":
                return true;
        }
        return false;
    }

}





*/
