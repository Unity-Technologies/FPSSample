using System;
using System.Collections;
using System.Collections.Generic;
using Macrometa;
using UnityEngine;

public class GDNTransport :  INetworkTransport {
    public static bool setupComplete = false;
    // must be set before websockets are opened.
    public static bool isSocketPingOn = false; //off by default for compatibility
    public static bool isStatsOn= false; //off by default for compatibility
    public static bool isPlayStatsClientOn;
    public static bool sendDummyTraffic= false; //off by default for compatibility
    public static bool isMonitor = false;  // is monitor not game
    public static bool connectionStarted = false;
    public static string localId;
    public static string appType;
    
    public GDNNetworkDriver gdnNetworkDriver;
   
    
    private GDNNetworkDriver.GDNConnection[] m_IdToConnection;
    
    private static GDNTransport instance=null;
    
    public static GDNTransport Instance {
        get {
            if (instance==null) {
                instance = new GDNTransport();
            }
            return instance;
        }
    }
    
    
    /// <summary>
    /// Setup m_IdToConnection = new NativeArray<NetworkConnection>(maxConnections, Allocator.Persistent);
    /// startup connection to Macrometa
    /// </summary>
    /// <param name="port"></param>
    /// <param name="maxConnections"></param>
    public void Connect(bool isServer, int port = 0, int maxConnections = 16 )
    {
        connectionStarted = true;
        GDNNetworkDriver.overrideIsServer = true;
        GDNNetworkDriver.overrideIsServerValue = isServer;
        GDNNetworkDriver.isPlayStatsClientOn = isPlayStatsClientOn;
        GDNNetworkDriver.isMonitor = isMonitor;
        GDNStreamDriver.isSocketPingOn = isSocketPingOn;
        GDNStreamDriver.isStatsOn = isStatsOn;
        GDNStreamDriver.sendDummyTraffic = sendDummyTraffic;
        GDNStreamDriver.localId = localId;
        GDNStreamDriver.appType = appType;
        gdnNetworkDriver= new GameObject().AddComponent<GDNNetworkDriver>();

        MonoBehaviour.DontDestroyOnLoad( gdnNetworkDriver.gameObject);

        m_IdToConnection = new GDNNetworkDriver.GDNConnection[maxConnections];
        //Does GDNNetworkDriver need maxConnections connection to refuse later connections
        LogFrequency.AddLogFreq("OnData",1, "onData: ", 2 );
        LogFrequency.AddLogFreq("SendData",1, "oSendData: ", 2 );
        LogFrequency.AddLogFreq("SendDataB",1, "oSendDataB: ", 2 );
        GameDebug.Log(" GDNTransport.Connect ++++++++++++++++++++++++++++++++++++++++++");
    }

    public void UpdateGameRecord(string gameMode, string mapName, int maxPlayers,
        int currPlayers, string status, long statusChangeTime) {
        if (gdnNetworkDriver != null) {
            gdnNetworkDriver.UpdateGameRecord(gameMode, mapName, maxPlayers,
                currPlayers, status, statusChangeTime);
        }
    }
    
    
    
    /// <summary>
    ///  get latency on consumer1
    /// </summary>
    /// <returns>returns -3 if consumer1 does not exist</returns>
    public int GetLatency() {
        if (gdnNetworkDriver != null && gdnNetworkDriver.gdnStreamDriver.consumer1 != null) {
            if (gdnNetworkDriver.gdnStreamDriver.consumer1.IsOpen == false) {
                return -1;
            }
            if (gdnNetworkDriver.gdnStreamDriver.consumer1.StartPingThread == false) {
                return -2;
            }
            return gdnNetworkDriver.gdnStreamDriver.consumer1.Latency;
        }
        else {
            return -3;
        }
        
    }
    
    /// <summary>
    /// ip and port are currently ignored
    /// make a connection object with internalID
    /// assign to m_IdToConnection
    /// return connection.InternalId;
    /// </summary>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    public int Connect(string ip, int port) {
        // if server setup is not complete delay this till it is complete
        if (!gdnNetworkDriver.gdnStreamDriver.setupComplete) {
            gdnNetworkDriver.gdnStreamDriver.sendConnect = false; //set that send connect has not been done yet
        }
        else {
            gdnNetworkDriver.gdnStreamDriver.Connect();
        }

        return 0;
    }
    
    /// <summary>
    /// send disconnect message type
    /// disable due new error previously unseen occuring in game
    /// when this change was made.
    /// problem was multiple connections happening
    /// may have been urelated.
    /// </summary>
    /// <param name="connectionId"></param>
   
    public void Disconnect(int connectionId) {
        gdnNetworkDriver.gdnStreamDriver.ProducerSend(connectionId, Macrometa.VirtualMsgType.Disconnect, new byte[0]); 
    }

    public bool NextEvent(ref TransportEvent e) {

        if (gdnNetworkDriver == null || !gdnNetworkDriver.gdnStreamDriver.setupComplete) {
            return false;
        }
        var driverTransportEvent = gdnNetworkDriver.gdnStreamDriver.PopEventQueue();
        var ev = driverTransportEvent.type;

        if (ev == GDNNetworkDriver.DriverTransportEvent.Type.Empty)
            return false;
        e.data = new byte[8192];
        switch (ev) {
            case GDNNetworkDriver.DriverTransportEvent.Type.Data:
                e.type = TransportEvent.Type.Data; 
                Array.Copy(driverTransportEvent.data, e.data, driverTransportEvent.dataSize);
                e.dataSize = driverTransportEvent.dataSize;
                e.connectionId = driverTransportEvent.connectionId;
                LogFrequency.IncrPrintByteA("OnData",e.data,e.dataSize);
                break;
            case GDNNetworkDriver.DriverTransportEvent.Type.Connect:
                e.type = TransportEvent.Type.Connect;
                e.connectionId = driverTransportEvent.connectionId;
                break;
            case GDNNetworkDriver.DriverTransportEvent.Type.Disconnect:
                e.type = TransportEvent.Type.Disconnect;
                e.connectionId = driverTransportEvent.connectionId;
                break;
            default:
                return false;
        }
        return true;
    }

    public void SendData(int connectionId, byte[] data, int sendSize) {
        byte[] sendData = new byte[sendSize];
        Array.Copy(data, sendData, sendSize);
        gdnNetworkDriver.gdnStreamDriver.ProducerSend(connectionId, Macrometa.VirtualMsgType.Data, sendData);
        LogFrequency.IncrPrintByteA("SendData",sendData,sendSize);
    }

    public string GetConnectionDescription(int connectionId) {
        return ""; // Same return as in FPSSample
    }
    
    public void Shutdown() {
        // not implement with GDN
    }

    public void Update()
    {
        // not needed with GDN
    }
    
}
