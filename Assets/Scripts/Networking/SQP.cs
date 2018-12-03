using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// An implementation of the ServerInfo part of the Server Query Protocol
/// </summary>
namespace SQP
{
    [Flags]
    public enum SQPChunkType
    {
        ServerInfo = 1,
        ServerRules = 2,
        PlayerInfo = 4,
        TeamInfo = 8
    }

    public enum SQPMessageType
    {
        ChallangeRequest = 0,
        ChallangeResponse = 0,
        QueryRequest = 1,
        QueryResponse = 1
    }

    public interface ISQPMessage
    {
        void ToStream(ref ByteOutputStream writer);
        void FromStream(ref ByteInputStream reader);
    }

    public struct SQPHeader : ISQPMessage
    {
        public byte Type { get; internal set; }
        public uint ChallangeId;

        public void ToStream(ref ByteOutputStream writer)
        {
            writer.WriteUInt8(Type);
            writer.WriteUInt32_NBO(ChallangeId);
        }

        public void FromStream(ref ByteInputStream reader)
        {
            Type = reader.ReadUInt8();
            ChallangeId = reader.ReadUInt32_NBO();
        }
    }

    public struct ChallangeRequest : ISQPMessage
    {
        public SQPHeader Header;

        public void ToStream(ref ByteOutputStream writer)
        {
            Header.Type = (byte)SQPMessageType.ChallangeRequest;
            Header.ToStream(ref writer);
        }

        public void FromStream(ref ByteInputStream reader)
        {
            Header.FromStream(ref reader);
        }
    }

    public struct ChallangeResponse
    {
        public SQPHeader Header;

        public void ToStream(ref ByteOutputStream writer)
        {
            Header.Type = (byte)SQPMessageType.ChallangeResponse;
            Header.ToStream(ref writer);
        }

        public void FromStream(ref ByteInputStream reader)
        {
            Header.FromStream(ref reader);
        }
    }

    public struct QueryRequest
    {
        public SQPHeader Header;
        public ushort Version;

        public byte RequestedChunks;

        public void ToStream(ref ByteOutputStream writer)
        {
            Header.Type = (byte)SQPMessageType.QueryRequest;

            Header.ToStream(ref writer);
            writer.WriteUInt16_NBO(Version);
            writer.WriteUInt8(RequestedChunks);
        }

        public void FromStream(ref ByteInputStream reader)
        {
            Header.FromStream(ref reader);
            Version = reader.ReadUInt16_NBO();
            RequestedChunks = reader.ReadUInt8();
        }
    }

    public struct QueryResponseHeader
    {
        public SQPHeader Header;
        public ushort Version;
        public byte CurrentPacket;
        public byte LastPacket;
        public ushort Length;

        public void ToStream(ref ByteOutputStream writer)
        {
            Header.Type = (byte)SQPMessageType.QueryResponse;
            Header.ToStream(ref writer);
            writer.WriteUInt16_NBO(Version);
            writer.WriteUInt8(CurrentPacket);
            writer.WriteUInt8(LastPacket);
            writer.WriteUInt16_NBO(Length);
        }

        public void FromStream(ref ByteInputStream reader)
        {
            Header.FromStream(ref reader);
            Version = reader.ReadUInt16_NBO();
            CurrentPacket = reader.ReadUInt8();
            LastPacket = reader.ReadUInt8();
            Length = reader.ReadUInt16_NBO();
        }
    }

    public class ServerInfo
    {
        public QueryResponseHeader QueryHeader;
        public uint ChunkLen;
        public Data ServerInfoData;

        public ServerInfo()
        {
            ServerInfoData = new Data();
        }

        public class Data
        {
            public ushort CurrentPlayers;
            public ushort MaxPlayers;

            public string ServerName = "";
            public string GameType = "";
            public string BuildId = "";
            public string Map = "";
            public ushort Port;

            public void ToStream(ref ByteOutputStream writer)
            {
                writer.WriteUInt16_NBO(CurrentPlayers);
                writer.WriteUInt16_NBO(MaxPlayers);

                writer.WriteString(ServerName, encoding);
                writer.WriteString(GameType, encoding);
                writer.WriteString(BuildId, encoding);
                writer.WriteString(Map, encoding);

                writer.WriteUInt16_NBO(Port);
            }

            public void FromStream(ref ByteInputStream reader)
            {
                CurrentPlayers = reader.ReadUInt16_NBO();
                MaxPlayers = reader.ReadUInt16_NBO();

                ServerName = reader.ReadString(encoding);
                GameType = reader.ReadString(encoding);
                BuildId = reader.ReadString(encoding);
                Map = reader.ReadString(encoding);

                Port = reader.ReadUInt16_NBO();
            }
        }

        public void ToStream(ref ByteOutputStream writer)
        {
            QueryHeader.ToStream(ref writer);

            var start = (ushort)writer.GetBytePosition();

            writer.WriteUInt32_NBO(0); // ChunkLen

            var chunkStart = (uint)writer.GetBytePosition();
            ServerInfoData.ToStream(ref writer);
            ChunkLen = (uint)writer.GetBytePosition() - chunkStart;
            QueryHeader.Length = (ushort)(writer.GetBytePosition() - start);

            const int LengthSize = 2;
            const int LengthOffset = 9;
            const int ChunkLenSize = 4;
            const int ChunkLenOffset = 11;

            var length = (ushort)System.Net.IPAddress.HostToNetworkOrder((short)QueryHeader.Length);
            var chunkLen = (uint)System.Net.IPAddress.HostToNetworkOrder((int)ChunkLen);

            writer.WriteBytesOffset(new byte[] { (byte)length, (byte)(length >> 8) }, 0, LengthOffset, LengthSize);
            writer.WriteBytesOffset(new byte[] { (byte)chunkLen, (byte)(chunkLen >> 8), (byte)(chunkLen >> 16), (byte)(chunkLen >> 24) }, 0, ChunkLenOffset, ChunkLenSize);
        }

        public void FromStream(ref ByteInputStream reader)
        {
            QueryHeader.FromStream(ref reader);
            ChunkLen = reader.ReadUInt32_NBO();

            ServerInfoData.FromStream(ref reader);
        }
        static private Encoding encoding = new UTF8Encoding();
    }

    public static class UdpExtensions
    {
        public static SocketError SetupAndBind(this Socket socket, int port = 0)
        {
            SocketError error = SocketError.Success;
            socket.Blocking = false;

            var ep = new IPEndPoint(IPAddress.Any, port);
            try
            {
                socket.Bind(ep);
            }
            catch (SocketException e)
            {
                error = e.SocketErrorCode;
                throw e;
            }
            return error;
        }
    }

    public class SQPClient
    {
        Socket m_Socket;

        byte[] m_Buffer = new byte[1472];

        System.Net.EndPoint endpoint = new System.Net.IPEndPoint(0, 0);

        public enum SQPClientState
        {
            Idle,
            WaitingForChallange,
            WaitingForResponse,
        }
        public class SQPQuery
        {
            public SQPQuery()
            {
                m_ServerInfo = new ServerInfo();
                m_State = SQPClientState.Idle;
                m_Server = null;
            }
            public void Init(IPEndPoint server)
            {
                GameDebug.Assert(m_State == SQPClientState.Idle);
                GameDebug.Assert(m_Server == null);
                m_Server = server;
                validResult = false;
            }
            public IPEndPoint m_Server;
            public bool validResult;
            public SQPClientState m_State;
            public uint ChallangeId;
            public long RTT;
            public long StartTime;
            public ServerInfo m_ServerInfo;
        }

        List<SQPQuery> m_Queries = new List<SQPQuery>();

        public SQPClient()
        {
            m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            m_Socket.SetupAndBind(0);
        }

        public SQPQuery GetSQPQuery(IPEndPoint server)
        {
            SQPQuery q = null;
            foreach (var pending in m_Queries)
            {
                if (pending.m_State == SQPClientState.Idle && pending.m_Server == null)
                {
                    q = pending;
                    break;
                }
            }
            if (q == null)
            {
                q = new SQPQuery();
                m_Queries.Add(q);
            }

            q.Init(server);

            return q;
        }

        public void ReleaseSQPQuery(SQPQuery q)
        {
            q.m_Server = null;
        }

        public void StartInfoQuery(SQPQuery q)
        {
            GameDebug.Assert(q.m_State == SQPClientState.Idle);

            q.StartTime = NetworkUtils.stopwatch.ElapsedMilliseconds;

            var writer = new ByteOutputStream(m_Buffer);
            var req = new ChallangeRequest();
            req.ToStream(ref writer);

            m_Socket.SendTo(m_Buffer, writer.GetBytePosition(), SocketFlags.None, q.m_Server);
            q.m_State = SQPClientState.WaitingForChallange;
        }

        void SendServerInfoQuery(SQPQuery q)
        {
            q.StartTime = NetworkUtils.stopwatch.ElapsedMilliseconds;
            var req = new QueryRequest();
            req.Header.ChallangeId = q.ChallangeId;
            req.RequestedChunks = (byte)SQPChunkType.ServerInfo;

            var writer = new ByteOutputStream(m_Buffer);
            req.ToStream(ref writer);

            q.m_State = SQPClientState.WaitingForResponse;
            m_Socket.SendTo(m_Buffer, writer.GetBytePosition(), SocketFlags.None, q.m_Server);
        }

        public void Update()
        {
            if (m_Socket.Poll(0, SelectMode.SelectRead))
            {
                int read = m_Socket.ReceiveFrom(m_Buffer, m_Buffer.Length, SocketFlags.None, ref endpoint);
                if (read > 0)
                {
                    var reader = new ByteInputStream(m_Buffer);
                    var header = new SQPHeader();
                    header.FromStream(ref reader);

                    foreach (var q in m_Queries)
                    {
                        if (q.m_Server == null || !endpoint.Equals(q.m_Server))
                            continue;

                        switch (q.m_State)
                        {
                            case SQPClientState.Idle:
                                // Just ignore if we get extra data
                                break;

                            case SQPClientState.WaitingForChallange:
                                if ((SQPMessageType)header.Type == SQPMessageType.ChallangeResponse)
                                {
                                    q.ChallangeId = header.ChallangeId;
                                    q.RTT = NetworkUtils.stopwatch.ElapsedMilliseconds - q.StartTime;
                                    // We restart timer so we can get an RTT that is an average between two measurements
                                    q.StartTime = NetworkUtils.stopwatch.ElapsedMilliseconds;
                                    SendServerInfoQuery(q);
                                }
                                break;

                            case SQPClientState.WaitingForResponse:
                                if ((SQPMessageType)header.Type == SQPMessageType.QueryResponse)
                                {
                                    reader.Reset();
                                    q.m_ServerInfo.FromStream(ref reader);

                                    // We report the average of two measurements
                                    q.RTT = (q.RTT + (NetworkUtils.stopwatch.ElapsedMilliseconds - q.StartTime)) / 2;

                                    /*
                                    GameDebug.Log(string.Format("ServerName: {0}, BuildId: {1}, Current Players: {2}, Max Players: {3}, GameType: {4}, Map: {5}, Port: {6}",
                                        m_ServerInfo.ServerInfoData.ServerName,
                                        m_ServerInfo.ServerInfoData.BuildId,
                                        (ushort)m_ServerInfo.ServerInfoData.CurrentPlayers,
                                        (ushort)m_ServerInfo.ServerInfoData.MaxPlayers,
                                        m_ServerInfo.ServerInfoData.GameType,
                                        m_ServerInfo.ServerInfoData.Map,
                                        (ushort)m_ServerInfo.ServerInfoData.Port));
                                        */

                                    q.validResult = true;
                                    q.m_State = SQPClientState.Idle;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }
            }

            foreach (var q in m_Queries)
            {
                // Timeout if stuck in any state but idle for too long
                if (q.m_State != SQPClientState.Idle)
                {
                    var now = NetworkUtils.stopwatch.ElapsedMilliseconds;
                    if (now - q.StartTime > 3000)
                    {
                        q.m_State = SQPClientState.Idle;
                    }
                }
            }
        }
    }

    public class SQPServer
    {
        Socket m_Socket;
        System.Random m_Random;

        SQP.ServerInfo m_ServerInfo = new ServerInfo();

        public SQP.ServerInfo.Data ServerInfoData
        {
            get { return m_ServerInfo.ServerInfoData; }
            set { m_ServerInfo.ServerInfoData = value; }
        }

        byte[] m_Buffer = new byte[1472];

        System.Net.EndPoint endpoint = new System.Net.IPEndPoint(0, 0);
        Dictionary<EndPoint, uint> m_OutstandingTokens = new Dictionary<EndPoint, uint>();

        public SQPServer(int port)
        {
            m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            m_Socket.SetupAndBind(port);
            m_Random = new System.Random();
        }

        public void Update()
        {
            if (m_Socket.Poll(0, SelectMode.SelectRead))
            {
                int read = m_Socket.ReceiveFrom(m_Buffer, m_Buffer.Length, SocketFlags.None, ref endpoint);
                if (read > 0)
                {
                    var reader = new ByteInputStream(m_Buffer);
                    var header = new SQPHeader();
                    header.FromStream(ref reader);

                    SQPMessageType type = (SQPMessageType)header.Type;

                    switch (type)
                    {
                        case SQPMessageType.ChallangeRequest:
                            {
                                if (!m_OutstandingTokens.ContainsKey(endpoint))
                                {
                                    uint token = GetNextToken();
                                    //Debug.Log("token generated: " + token);

                                    var writer = new ByteOutputStream(m_Buffer);
                                    var rsp = new ChallangeResponse();
                                    rsp.Header.ChallangeId = token;
                                    rsp.ToStream(ref writer);

                                    m_Socket.SendTo(m_Buffer, writer.GetBytePosition(), SocketFlags.None, endpoint);

                                    m_OutstandingTokens.Add(endpoint, token);
                                }

                            }
                            break;
                        case SQPMessageType.QueryRequest:
                            {
                                uint token;
                                if (!m_OutstandingTokens.TryGetValue(endpoint, out token))
                                {
                                    //Debug.Log("Failed to find token!");
                                    return;
                                }
                                m_OutstandingTokens.Remove(endpoint);

                                reader.Reset();
                                var req = new QueryRequest();
                                req.FromStream(ref reader);

                                if ((SQPChunkType)req.RequestedChunks == SQPChunkType.ServerInfo)
                                {
                                    var rsp = m_ServerInfo;
                                    var writer = new ByteOutputStream(m_Buffer);
                                    rsp.QueryHeader.Header.ChallangeId = token;

                                    rsp.ToStream(ref writer);
                                    m_Socket.SendTo(m_Buffer, writer.GetBytePosition(), SocketFlags.None, endpoint);
                                }
                            }
                            break;
                        default:
                            break;
                    }

                }
            }
        }

        uint GetNextToken()
        {
            uint thirtyBits = (uint)m_Random.Next(1 << 30);
            uint twoBits = (uint)m_Random.Next(1 << 2);
            return (thirtyBits << 2) | twoBits;
        }
    }
}