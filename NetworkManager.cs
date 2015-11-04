//************************************
//This unit provides an interface to the capabilities of the network (client-server) interaction;
//This unit implements a client survey operations;
//Unit implements write operations to the client thread;
//This unit using async method for stream operation;
//This code was designed in IVSystems 2015
//Author: 4ar0deus@mail.ru
//************************************
using UnityEngine;
using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
namespace NetworkManager {
//******************************************
//Debug log was provide capabilities for 
//reference information message to user
//If have need this functionality 
//use descent from this class
//******************************************
    class DebugLog {                       // debug system, for Unity3d using Debug.Log
        void m_write(string message) {     // for any framework realization using method declarated in System
            Debug.Log(message);
        }
        public void m_dmesg(string message) {
            m_write(message);              //In the future, this method will be changed to hide the non-priority messages
        }
        public void m_dmesg(string message, int id) {
            m_write(message);
        }
    }
//Provide capabilities DebugSystem for this class
    class RemoteManager : DebugLog {
        public RemoteManager(string keyWord, int bufSize) {    // serice class for client function
            tempBufferSize = bufSize;                          // size of message buffer
            mainClient = null;
            identityKeyword = keyWord;                         // unique keyword for identification object in network
            _stream = null; 
            message = null;                                    // This var allow to work with the data obtained from other class
            recSize = -5;                                      // for identification (client disconnected event)
            status = false;                                    // client status                                                            
        }                                                      // false - offline, true - init object
        //***************************************
        //if client was disconnected, need to dispose 
        //init resource
        //***************************************
        public void dispose() {
            mainClient = null;
            _stream = null;
            status = false;
            streamBuf = null;
            message = null;
        }
        //***************************************
        //If the key is obtained by the network coincides with a key
        //customer, you connect customers
        //***************************************
        public void bindClient(TcpClient _client) {
            mainClient = _client;
            _stream = mainClient.GetStream();
            status = true;
            streamBuf = new byte[tempBufferSize];
            m_dmesg("Client connected: " + identityKeyword);
            recSize = -5;
        }
        //***************************************
        // Start receive data as Async method
        //***************************************
        public void startRec() {
            if ( status )               // if client online
                try {
                    _stream.BeginRead(streamBuf, 0, tempBufferSize, new AsyncCallback(asyncRec), mainClient);
                }
                // if using Visual Studio 2015 as native c# application, this exeption was called
                catch ( System.IO.IOException ex ) {
                    m_dmesg(ex.ToString());
                }
        }
        void asyncRec(IAsyncResult iar) {
            try {
                message = null;
                TcpClient _temp = (TcpClient)iar.AsyncState;
                NetworkStream reader = _temp.GetStream();
                recSize = -1;
                recSize = reader.EndRead(iar);
                message = Encoding.UTF8.GetString(streamBuf);
            }
            catch ( InvalidOperationException ex ) {
                m_dmesg("Invalid get stream for null client");
                m_dmesg(ex.ToString());
            }

        }
        //******************************************
        // Async send method
        //******************************************
        public void startSend(string msg) {
            if ( status ) {
                try {
                    byte[] ioBuf = Encoding.UTF8.GetBytes(msg);
                    _stream.BeginWrite(ioBuf, 0, msg.Length, new AsyncCallback(asyncSend), mainClient);
                }
                catch ( System.IO.IOException ex ) {
                    m_dmesg(ex.ToString());
                }
            }
        }
        void asyncSend(IAsyncResult iar) {
            TcpClient _temp = (TcpClient)iar.AsyncState;
            NetworkStream reader = _temp.GetStream();
            reader.EndWrite(iar);
        }

        //**********************************************
        // Propetries and getters for access to private members of the class   
        //**********************************************
        public string getIdentityKey {
            get { return identityKeyword; }
        }
        public string getMessage {
            get {
                string bufferMessage = message;
                streamBuf = null;
                streamBuf = new byte[tempBufferSize];
                message = null;
                return bufferMessage;
            }
        }
        public int recSize;
        public  bool status;
        int tempBufferSize;
        byte[] streamBuf;
        public string message;
        TcpClient mainClient;
        string identityKeyword;
        NetworkStream _stream;
    }
    #region serverDataUnit
    //******************************************
    // Control field for init server
    //******************************************
    class ControlField : DebugLog {
        public ControlField(string localHostName) {
            portAddr = 27015;                               // default port
            serverState = false;
            //***********************************
            //ipAddr -> a useless piece of code showing where the server is started, 
            // in rare cases shows the real ip
            //***********************************
            ipAddr = Dns.GetHostByName(localHostName).AddressList[0].ToString();
            mainListener = null;
        }
        public int portAddr;
        public TcpListener mainListener;
        public string ipAddr;
        public bool serverState;                        // server status

    }
    #endregion serverDataUnit
    class ServerController : DebugLog {                 // main class for NetworkManager
        //******************************************
        //the number of customers are able to simultaneously connect to the server
        //******************************************
        int permittedClientNum;                 
        ControlField serverResource;
        RemoteManager[] clientList;                         
        int clientCount = -1;
        public ServerController(int _permittedClientNum, string hostname) {
            serverResource = new ControlField(hostname);
            if ( _permittedClientNum < 0 || _permittedClientNum > 64 ) {
                clientList = new RemoteManager[3];
                permittedClientNum = 3;
            }
            else {
                clientList = new RemoteManager[_permittedClientNum];
                permittedClientNum = _permittedClientNum;
            }
            m_dmesg("Created slots for " + _permittedClientNum + " clients");
        }
        public void addClient(string identityKey, int bufferSize) {     // Reserve a slot for a customer
            if ( clientCount <= permittedClientNum ) {
                ++clientCount;
                clientList[clientCount] = new RemoteManager(identityKey, bufferSize);
                m_dmesg("Reserve client slots [ id:" + clientCount + " identityKey:" + identityKey + "]");
            }
        }
        public void killAll() {                                        // destroy server, calling from ApplicationQuit
            serverResource.mainListener.Stop();
            serverResource.mainListener = null;
            serverResource = null;
            foreach ( RemoteManager _cl in clientList ) {
                _cl.dispose();
            }
        }
        //******************************
        // the method is not called directly, use the adapter clientHandler
        //******************************
        public void setMessageFromClient(int id, string msg) {
            if ( clientList[id].status ) {
                clientList[id].startSend(msg);
            }
        }
        //******************************
        // the method is not called directly, use the adapter clientHandler
        //******************************
        public string getMessageFromClient(int clientID) {
            clientList[clientID].startRec();
            //********************************************
            //if recSize equal by -1, this means about disconnected client
            if ( clientList[clientID].recSize == -1 ) {
                clientList[clientID].dispose();
                m_dmesg("Client " + clientList[clientID].getIdentityKey + " was disconnected, recconect: " + clientID + ", his status is:" + clientList[clientID].status);
                m_WaitClientAccepting(clientID);        // reconnection
            }
            else {
                return clientList[clientID].message;
            }
            return null;            // some situation mostly return null
        }
        public void m_startServer() {   // mounted server 
            try {
                serverResource.mainListener = new TcpListener(serverResource.portAddr);
                serverResource.mainListener.Start();
                serverResource.serverState = true;
                m_dmesg("Server mounted on " + serverResource.ipAddr + ":" + serverResource.portAddr);
            }
            catch ( SocketException ex ) {
                m_dmesg("Server not mounted on " + serverResource.ipAddr + ":" + serverResource.portAddr);
                m_dmesg(ex.StackTrace);
            }
        }
        public void m_WaitGeneralAccepting() {
            if ( serverResource.serverState ) {
                for ( int i = 0; i <= clientCount; ++i ) {
                    m_dmesg("[mwga] Server wait connection on " + i + " slots");
                    serverResource.mainListener.BeginAcceptTcpClient(new AsyncCallback(m_doGeneralClientAccepting), serverResource.mainListener);
                }
            }
        }
        //*****************************
        //parsing 0-char symbol from receive message
        //****************************
        string parseCode(string str) {
            string buff = "";
            for ( int i = 0; i < str.Length; ++i ) {
                if ( ( int ) str[i] != 0 ) buff += str[i];
            }
            return buff;
        }
        //****************************************
        // Search all client determine of clientCount
        //***************************************
        private void m_doGeneralClientAccepting(IAsyncResult iar) {
            TcpClient _tempClient = serverResource.mainListener.EndAcceptTcpClient(iar);
            byte[] iobuffer = new byte[64];
            m_dmesg("Unknown device waits for a connection...");
            NetworkStream _tempStream = _tempClient.GetStream();
            _tempStream.Read(iobuffer, 0, 64);
            string IdentityKey = parseCode(Encoding.UTF8.GetString(iobuffer));
            for ( int i = 0; i <= clientCount; ++i ) {
                if ( !clientList[i].status ) {
                    if ( clientList[i].getIdentityKey.Equals(IdentityKey) ) {
                        m_dmesg("[mdgca] Recognized connected device: " + IdentityKey);
                        clientList[i].bindClient(_tempClient);
                        return;
                    }
                }
            }
            m_dmesg("Device not recognized...");
            _tempClient.Close();
        }
        private void callBackConnection() {
            if ( serverResource.serverState ) {
                m_dmesg("Server wait connection");
                serverResource.mainListener.BeginAcceptTcpClient(new AsyncCallback(m_doGeneralClientAccepting), serverResource.mainListener);
            }
        }
        TemporaryID tmp = new TemporaryID();
        public void m_WaitClientAccepting(int clientID) {
            if ( serverResource.serverState && ((clientID <= clientCount) && (clientID >= 0)) ) {
                serverResource.mainListener.BeginAcceptTcpClient(new AsyncCallback(m_doClientAccepting), serverResource.mainListener);
                tmp.bufSet(clientID);
                m_dmesg("[mwca] Server wait connection");
            }
        }
        private void m_doClientAccepting(IAsyncResult iar) {
            int clientID = tmp.getBuf();
            m_dmesg("Server wait connection object for " + clientID + " slots");
            TcpClient _tempClient = serverResource.mainListener.EndAcceptTcpClient(iar);
            byte[] ioBuffer = new byte[clientList[clientID].getIdentityKey.Length];
            m_dmesg("Unknow device waits for a connection to " + clientID + " slots");
            NetworkStream _tempStream = _tempClient.GetStream();
            _tempStream.Read(ioBuffer, 0, clientList[clientID].getIdentityKey.Length);
            if ( Encoding.UTF8.GetString(ioBuffer).Equals(clientList[clientID].getIdentityKey) ) {
                m_dmesg("[mdca] Recognizes the device: " + clientList[clientID].getIdentityKey);
                clientList[clientID].bindClient(_tempClient);
            }
            else {
                m_dmesg("Connection refused, the device is not recognized");
            }
        }
        class TemporaryID {
            public static int id = 0;
            public void bufSet(int _id) {
                id = _id;
            }
            public int getBuf() {
                return id;
            }
        }
    }
}