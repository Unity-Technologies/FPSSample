using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.Profiling;

public class UNETTransport : INetworkTransport
{
    public int hostId { get { return m_HostId; } }

    public bool Init(int port = 0, int maxConnections = 16)
    {
        var config = new UnityEngine.Networking.GlobalConfig();
        config.ThreadAwakeTimeout = 1;
        UnityEngine.Networking.NetworkTransport.Init(config);

        m_ReadBuffer = new byte[NetworkConfig.maxPackageSize + 1024];

        m_ConnectionConfig = new ConnectionConfig();
        m_ConnectionConfig.SendDelay = 0;

        m_ChannelUnreliable = m_ConnectionConfig.AddChannel(QosType.Unreliable);
        m_Topology = new HostTopology(m_ConnectionConfig, maxConnections);

        if (UnityEngine.Debug.isDebugBuild && m_isNetworkSimuationActive)
            m_HostId = NetworkTransport.AddHostWithSimulator(m_Topology, 1, 300, port);
        else
            m_HostId = NetworkTransport.AddHost(m_Topology, port);

        if (m_HostId != -1 && port != 0)
        {
            GameDebug.Log("Listening on " + string.Join(", ", NetworkUtils.GetLocalInterfaceAddresses()) + " on port " + port);
        }

        return m_HostId != -1;
    }

    public void Shutdown()
    {
        if (m_HostId != -1)
        {
            NetworkTransport.RemoveHost(m_HostId);
            m_HostId = -1;
        }
    }

    public int Connect(string address, int port)
    {
        IPAddress[] ipAddresses;
        try
        {
            ipAddresses = Dns.GetHostAddresses(address);
        }
        catch (System.Exception e)
        {
            GameDebug.Log("Unable to resolve " + address + ". " + e.Message);
            return 0;
        }

        if (ipAddresses.Length < 1)
        {
            GameDebug.Log("Unable to resolve " + address + ". Host not found");
            return 0;
        }

        // TODO (petera) do we want to do round-robin?
        var ip = ipAddresses[0].ToString();

        byte error;
        if (UnityEngine.Debug.isDebugBuild && m_isNetworkSimuationActive)
        {
            var simulationConfig = new ConnectionSimulatorConfig(48, 50, 48, 50, 10);
            return NetworkTransport.ConnectWithSimulator(m_HostId, ip, port, 0, out error, simulationConfig);
        }
        else
            return NetworkTransport.Connect(m_HostId, ip, port, 0, out error);
    }

    public void Disconnect(int connectionId)
    {
        byte error;
        NetworkTransport.Disconnect(m_HostId, connectionId, out error);
    }

    public int Update()
    {
        return 0;
    }

    public bool NextEvent(ref TransportEvent res)
    {
        GameDebug.Assert(m_HostId > -1, "Trying to update transport with no host id");

        Profiler.BeginSample("UNETTransform.ReadData()");

        int connectionId;
        int channelId;
        int receivedSize;
        byte error;

        var ne = NetworkTransport.ReceiveFromHost(m_HostId, out connectionId, out channelId, m_ReadBuffer, m_ReadBuffer.Length, out receivedSize, out error);

        switch (ne)
        {
            default:
            case UnityEngine.Networking.NetworkEventType.Nothing:
                Profiler.EndSample();
                return false;
            case UnityEngine.Networking.NetworkEventType.ConnectEvent:
            {
                string address;
                int port;
                NetworkID network;
                NodeID dstNode;
                NetworkTransport.GetConnectionInfo(m_HostId, connectionId, out address, out port, out network, out dstNode, out error);
                GameDebug.Log("Incoming connection: " + connectionId + " (from " + address + ":" + port + ")");

                res.type = TransportEvent.Type.Connect;
                res.connectionId = connectionId;
                break;
            }
            case UnityEngine.Networking.NetworkEventType.DisconnectEvent:
                res.type = TransportEvent.Type.Disconnect;
                res.connectionId = connectionId;
                break;
            case UnityEngine.Networking.NetworkEventType.DataEvent:
                res.type = TransportEvent.Type.Data;
                res.data = m_ReadBuffer;
                res.dataSize = receivedSize;
                res.connectionId = connectionId;
                break;
        }

        Profiler.EndSample();

        return true;
    }

    public void SendData(int connectionId, byte[] data, int sendSize)
    {
        Profiler.BeginSample("UNETTransform.SendData()");

        byte error;
        if(!NetworkTransport.Send(m_HostId, connectionId, m_ChannelUnreliable, data, sendSize, out error))
            GameDebug.Log("Error while sending data to connection : " + connectionId + "(error : " + (NetworkError)error + ")");

        Profiler.EndSample();
    }

    public string GetConnectionDescription(int connectionId)
    {
        string address;
        int port;
        NetworkID network;
        NodeID dstNode;
        byte error;
        NetworkTransport.GetConnectionInfo(m_HostId, connectionId, out address, out port, out network, out dstNode, out error);
        return "UNET: " + address + ":" + port;
    }

    byte[] m_ReadBuffer;

    bool m_isNetworkSimuationActive = false;

    ConnectionConfig m_ConnectionConfig;
    HostTopology m_Topology;

    int m_HostId = -1;
    int m_ChannelUnreliable;
}
