using System;
using System.Collections;
using System.Collections.Generic;
using Macrometa;
using UnityEngine;

public class GDNTransport :  INetworkTransport {
    public static bool setupComplete = false;
    public static bool isPingOn; // must be set before websockets are opened.
    
    private GDNNetworkDriver gdnNetworkDriver;
    private GDNNetworkDriver.GDNConnection[] m_IdToConnection;
    
    /// <summary>
    /// Setup m_IdToConnection = new NativeArray<NetworkConnection>(maxConnections, Allocator.Persistent);
    /// startup connection to Macrometa
    /// </summary>
    /// <param name="port"></param>
    /// <param name="maxConnections"></param>
    public GDNTransport(bool isServer, int port = 0, int maxConnections = 16 )
    {
        
        GDNNetworkDriver.overrideIsServer = true;
        GDNNetworkDriver.overrideIsServerValue = isServer;
        GDNNetworkDriver.isPingOn = !isServer;

        gdnNetworkDriver= new GameObject().AddComponent<GDNNetworkDriver>();

        MonoBehaviour.DontDestroyOnLoad(gdnNetworkDriver.gameObject);
        gdnNetworkDriver.port = port;

        m_IdToConnection = new GDNNetworkDriver.GDNConnection[maxConnections];
        //Does GDNNetworkDriver need maxConnections connection to refuse later connections
        LogFrequency.AddLogFreq("OnData",1, "onData: ", 2 );
        LogFrequency.AddLogFreq("SendData",1, "oSendData: ", 2 );
        LogFrequency.AddLogFreq("SendDataB",1, "oSendDataB: ", 2 );

    }
    
    /// <summary>
    ///  get latency on consumer1
    /// </summary>
    /// <returns>returns -3 if consumer1 does not exist</returns>
    public int GetLatency() {
        if (gdnNetworkDriver != null && gdnNetworkDriver.consumer1 != null) {
            if (gdnNetworkDriver.consumer1.IsOpen == false) {
                return -1;
            }
            if (gdnNetworkDriver.consumer1.StartPingThread == false) {
                return -2;
            }
            return gdnNetworkDriver.consumer1.Latency;
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
        if (!gdnNetworkDriver.setupComplete) {
            gdnNetworkDriver.sendConnect = false; //set that send connect has not been done yet
        }
        else {
            gdnNetworkDriver.Connect();
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
        //gdnNetworkDriver.ProducerSend(connectionId, Macrometa.VirtualMsgType.Disconnect, new byte[0]); 
    }

    public bool NextEvent(ref TransportEvent e) {

        if (gdnNetworkDriver == null || !gdnNetworkDriver.setupComplete) {
            return false;
        }
        var driverTransportEvent = gdnNetworkDriver.PopEventQueue();
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
        gdnNetworkDriver.ProducerSend(connectionId, Macrometa.VirtualMsgType.Data, sendData);
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
    
    
    public void ServerReceivePing(TransportEvent te) {
       
    }
    public void ClientReceivePing(TransportEvent te) {
        
    }
    
    public void ServerSendPing() {
        
    }
    public void ClientSendPing() {
        
    }
}
