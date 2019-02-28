using System;
using System.Threading;
using System.Net;
using NUnit.Framework;

using System.Net.Sockets;

using UnityEngine;
using SQP;
using System.Text;
using Unity.Networking.Transport;

namespace TransportTests
{
    public class SQPTests
    {
        byte[] m_Buffer = new byte[1472];

        DataStreamReader reader;
        DataStreamWriter writer;
        DataStreamReader.Context context;

        [SetUp]
        public void Setup()
        {
            reader = new DataStreamReader();
            writer = new DataStreamWriter(m_Buffer.Length, Unity.Collections.Allocator.Temp);
        }

        [TearDown]
        public void Teardown()
        {
            writer.Dispose();
        }

        [Test]
        public void SQP_SerializeChallangeRequest_NoError()
        {
            var snd = new ChallangeRequest();
            snd.ToStream(ref writer);

            reader = new DataStreamReader(writer, 0, writer.Length);
            context = default(DataStreamReader.Context);
            var rcv = new ChallangeRequest();
            rcv.FromStream(reader, ref context);

            Assert.AreEqual((byte)SQPMessageType.ChallangeRequest, rcv.Header.Type);
        }

        [Test]
        public void SQP_SerializeChallangeResponse_NoError()
        {
            var id = (uint)1337;
            var snd = new ChallangeResponse();

            snd.Header.ChallangeId = id;

            snd.ToStream(ref writer);

            var rcv = new ChallangeResponse();

            reader = new DataStreamReader(writer, 0, writer.Length);
            context = default(DataStreamReader.Context);
            rcv.FromStream(reader, ref context);

            Assert.AreEqual((byte)SQPMessageType.ChallangeResponse, rcv.Header.Type);
            Assert.AreEqual(id, (uint)rcv.Header.ChallangeId);
        }

        [Test]
        public void SQP_SerializeQueryRequest_NoError()
        {
            var id = (uint)1337;
            var chunk = (byte)31;

            var snd = new QueryRequest();

            snd.Header.ChallangeId = id;
            snd.RequestedChunks = chunk;

            snd.ToStream(ref writer);

            var rcv = new QueryRequest();
            reader = new DataStreamReader(writer, 0, writer.Length);
            context = default(DataStreamReader.Context);
            rcv.FromStream(reader, ref context);

            Assert.AreEqual((byte)SQPMessageType.QueryRequest, rcv.Header.Type);
            Assert.AreEqual(id, (uint)rcv.Header.ChallangeId);
            Assert.AreEqual(chunk, rcv.RequestedChunks);
        }

        [Test]
        public void SQP_SerializeQueryResponseHeader_NoError()
        {
            var id = (uint)1337;
            var version = (ushort)12345;
            var packet = (byte)3;
            var last = (byte)9;

            var snd = new QueryResponseHeader();

            snd.Header.ChallangeId = id;
            snd.Version = version;
            snd.CurrentPacket = packet;
            snd.LastPacket = last;

            snd.ToStream(ref writer);

            var rcv = new QueryResponseHeader();
            var reader = new DataStreamReader(writer, 0, writer.Length);
            context = default(DataStreamReader.Context);
            rcv.FromStream(reader, ref context);

            Assert.AreEqual((byte)SQPMessageType.QueryResponse, rcv.Header.Type);
            Assert.AreEqual(id, (uint)rcv.Header.ChallangeId);
            Assert.AreEqual(version, rcv.Version);
            Assert.AreEqual(packet, rcv.CurrentPacket);
            Assert.AreEqual(last, rcv.LastPacket);
        }

        [Test]
        public void ByteOutputStream_WriteAndReadString_Works()
        {
            var encoding = new UTF8Encoding();
            var sendShort = "banana";
            var sendLong = "thisisalongstringrepeatedoverthisisalongstringrepeatedoverthisisalongstringrepeatedoverandoverthisisalongstringrepeatedoverandoverthisisalongstringrepeatedoverandoverthisisalongstringrepeatedoverandoverthisisalongstringrepeatedoverandoverthisisalongstringrepeatedoverandover";
            var sendUTF = "ᚠᛇᚻ᛫ᛒᛦᚦ᛫ᚠᚱᚩᚠᚢᚱ᛫ᚠᛁᚱᚪ᛫ᚷᛖᚻᚹᛦᛚᚳᚢᛗᚠᛇᚻ᛫ᛒᛦᚦ᛫ᚠᚱᚩᚠᚢᚱ᛫ᚠᛁᚱᚪ᛫ᚷᛖᚻᚹᛦᛚᚳᚢᛗᚠᛇᚻ᛫ᛒᛦᚦ᛫ᚠᚱᚩᚠᚢᚱ᛫ᚠᛁᚱᚪ᛫ᚷᛖᚻᚹᛦᛚᚳᚢᛗᚠᛇᚻ᛫ᛒᛦᚦ᛫ᚠᚱᚩᚠᚢᚱ᛫ᚠᛁᚱᚪ᛫ᚷᛖᚻᚹᛦᛚᚳᚢᛗᚠᛇᚻ᛫ᛒᛦᚦ᛫ᚠᚱᚩᚠᚢᚱ᛫ᚠᛁᚱᚪ᛫ᚷᛖᚻᚹᛦᛚᚳᚢᛗᚠᛇᚻ᛫ᛒᛦᚦ᛫ᚠᚱᚩᚠᚢᚱ᛫ᚠᛁᚱᚪ᛫ᚷᛖᚻᚹᛦᛚᚳᚢᛗᚠᛇᚻ᛫ᛒᛦᚦ᛫ᚠᚱᚩᚠᚢᚱ᛫ᚠᛁᚱᚪ᛫ᚷᛖᚻᚹᛦᛚᚳᚢᛗᚠᛇᚻ᛫ᛒᛦᚦ᛫ᚠᚱᚩᚠᚢᚱ᛫ᚠᛁᚱᚪ᛫ᚷᛖᚻᚹᛦᛚᚳᚢᛗᚠᛇᚻ᛫ᛒᛦᚦ᛫ᚠᚱᚩᚠᚢᚱ᛫ᚠᛁᚱᚪ᛫ᚷᛖᚻᚹᛦᛚᚳᚢᛗᚠᛇᚻ᛫ᛒᛦᚦ᛫ᚠᚱᚩᚠᚢᚱ᛫ᚠᛁᚱᚪ᛫ᚷᛖᚻᚹᛦᛚᚳᚢᛗ";

            writer.WriteString(sendShort, encoding);
            writer.WriteString(sendLong, encoding);
            writer.WriteString(sendUTF, encoding);

            reader = new DataStreamReader(writer, 0, writer.Length);
            context = default(DataStreamReader.Context);

            var recvShort = reader.ReadString(ref context, encoding);
            var recvLong = reader.ReadString(ref context, encoding);
            var recvUTF = reader.ReadString(ref context, encoding);

            Assert.AreEqual(sendShort, recvShort);

            sendLong = sendLong.Substring(0, recvLong.Length);
            Assert.AreEqual(sendLong, recvLong);

            sendUTF = sendUTF.Substring(0, recvUTF.Length);
            Assert.AreEqual(sendUTF, recvUTF);
        }

        [Test]
        public void SQP_SerializeServerInfo_NoError()
        {
            var current = (ushort)34;
            var max = (ushort)35;
            var port = (ushort)35001;
            var build = "2018.3";

            var header = new QueryResponseHeader();

            header.Header.ChallangeId = 1337;
            header.Version = 12345;
            header.CurrentPacket = 12;
            header.LastPacket = 13;

            var snd = new SQP.ServerInfo();
            snd.QueryHeader = header;
            snd.ServerInfoData.CurrentPlayers = current;
            snd.ServerInfoData.MaxPlayers = max;
            snd.ServerInfoData.ServerName = "Server";
            snd.ServerInfoData.GameType = "GameType";
            snd.ServerInfoData.BuildId = "2018.3";
            snd.ServerInfoData.Map = "Level0";
            snd.ServerInfoData.Port = port;

            snd.ToStream(ref writer);

            var rcv = new SQP.ServerInfo();
            reader = new DataStreamReader(writer, 0, writer.Length);
            context = default(DataStreamReader.Context);
            rcv.FromStream(reader, ref context);

            Assert.AreEqual((byte)SQPMessageType.QueryResponse, rcv.QueryHeader.Header.Type);
            Assert.AreEqual((uint)header.Header.ChallangeId, (uint)rcv.QueryHeader.Header.ChallangeId);
            Assert.AreEqual(header.Version, rcv.QueryHeader.Version);
            Assert.AreEqual(header.CurrentPacket, rcv.QueryHeader.CurrentPacket);
            Assert.AreEqual(header.LastPacket, rcv.QueryHeader.LastPacket);

            Assert.AreEqual(current, (ushort)rcv.ServerInfoData.CurrentPlayers);
            Assert.AreEqual(max, (ushort)rcv.ServerInfoData.MaxPlayers);
            Assert.AreEqual(port, (ushort)rcv.ServerInfoData.Port);

            Assert.AreEqual(build, rcv.ServerInfoData.BuildId);
        }

        [Test]
        public void SQPClientServer_ServerInfoQuery_ServerInfoReceived()
        {
            var port = 13337;
            var server = new SQPServer(port);
            var endpoint = new IPEndPoint(IPAddress.Loopback, port);
            var client = new SQPClient();

            var sid = server.ServerInfoData;
            sid.ServerName = "Banana Boy Adventures";
            sid.BuildId = "2018-1";
            sid.CurrentPlayers = 1;
            sid.MaxPlayers = 20;
            sid.Port = 1337;
            sid.GameType = "Capture the egg.";
            sid.Map = "Great escape to the see";

            server.ServerInfoData = sid;

            var handle = client.GetSQPQuery(endpoint);
            client.StartInfoQuery(handle);

            var iterations = 0;
            while (handle.m_State != SQPClient.SQPClientState.Idle && iterations++ < 1000)
            {
                server.Update();
                client.Update();
            }
            Assert.Less(iterations, 1000);

            Assert.AreEqual(handle.m_State, SQPClient.SQPClientState.Idle);
            Assert.AreEqual(handle.validResult, true);
            var sidRecieved = handle.m_ServerInfo.ServerInfoData;
            Assert.AreEqual(sidRecieved.BuildId, sid.BuildId);
            Assert.AreEqual(sidRecieved.CurrentPlayers, sid.CurrentPlayers);
            Assert.AreEqual(sidRecieved.GameType, sid.GameType);
            Assert.AreEqual(sidRecieved.Map, sid.Map);
            Assert.AreEqual(sidRecieved.MaxPlayers, sid.MaxPlayers);
            Assert.AreEqual(sidRecieved.Port, sid.Port);
            Assert.AreEqual(sidRecieved.ServerName, sid.ServerName);
        }

        //[Test]
        public void SQPServer_ServerInfoQueryTest_ShouldWork()
        {
            var port = 10001;
            var server = new SQPServer(port);

            var sid = server.ServerInfoData;
            sid.ServerName = "Banana Boy Adventures";
            sid.BuildId = "2018-1";
            sid.CurrentPlayers = 1;
            sid.MaxPlayers = 20;
            sid.Port = 1337;
            sid.GameType = "Capture the egg.";
            sid.Map = "Great escape to the see";
            server.ServerInfoData = sid;

            var start = NetworkUtils.stopwatch.ElapsedMilliseconds;
            while(true)
            {
                server.Update();
                /*
                if (server.IsDone)
                {
                    Debug.Log("ServerDone");
                    return;
                }
                */

                if (NetworkUtils.stopwatch.ElapsedMilliseconds - start > 1000)
                    Debug.Log("Listening");
                
            }
        }
    }
}
