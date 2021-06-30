using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GDNTransport :  INetworkTransport {
    public static bool setupComplete = false;
    
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
        
        GDNNetworkDriver.overrideIsServer = isServer;
        GDNNetworkDriver.overrideIsServerValue = true;

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
    /// ip can client consumer name or server in property
    /// port can be port in
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

        if (ev == GDNNetworkDriver.DriverTransportEvent.Type.empty)
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
}
