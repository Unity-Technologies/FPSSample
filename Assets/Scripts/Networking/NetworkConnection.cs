using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NetworkCompression;
using UnityEngine.Profiling;

public class NetworkConnectionCounters
{
    public int bytesIn;                         // The number of user bytes received on this connection
    public int bytesOut;                        // The number of user bytes sent on this connection

    public int headerBitsIn;                    // The number of header bytes received on this connection

    public int packagesIn;                      // The number of packages received on this connection (including package fragments)
    public int packagesOut;                     // The number of packages sent on this connection (including package fragments)

    public int packagesStaleIn;                 // The number of state packages we received
    public int packagesDuplicateIn;             // The number of duplicate packages we received
    public int packagesOutOfOrderIn;            // The number of packages we received out of order

    public int packagesLostIn;                  // The number of incoming packages that was lost (i.e. holes in the package sequence)
    public int packagesLostOut;                 // The number of outgoing packages that wasn't acked (either due to choke or network)

    public int fragmentedPackagesIn;            // The number of incoming packages that was fragmented
    public int fragmentedPackagesOut;           // The number of outgoing packages that was fragmented

    public int fragmentedPackagesLostIn;        // The number of incoming fragmented packages we couldn't reassemble
    public int fragmentedPackagesLostOut;       // The number of outgoing fragmented packages that wasn't acked

    public int chokedPackagesOut;               // The number of packages we dropped due to choke

    public int eventsIn;                        // The total number of events received
    public int eventsOut;                       // The total number of events sent

    public int eventsLostOut;                   // The number of events that was lost

    public int reliableEventsOut;               // The number of reliable events sent
    public int reliableEventResendOut;          // The number of reliable events we had to resend

    public Aggregator avgBytesIn = new Aggregator();
    public Aggregator avgBytesOut = new Aggregator();
    public Aggregator avgPackagesIn = new Aggregator();
    public Aggregator avgPackagesOut = new Aggregator();
    public Aggregator avgPackageSize = new Aggregator();

    public void UpdateAverages()
    {
        avgBytesIn.Update(bytesIn);
        avgBytesOut.Update(bytesOut);
        avgPackagesIn.Update(packagesIn);
        avgPackagesOut.Update(packagesOut);
    }
}


public class PackageInfo
{
    public long sentTime;
    public bool fragmented;
    public NetworkMessage content;

    public List<NetworkEvent> events = new List<NetworkEvent>(10);

    public virtual void Reset()
    {
        sentTime = 0;
        fragmented = false;
        content = 0;
        foreach (var eventInfo in events)
            eventInfo.Release();
        events.Clear();
    }
}

public class NetworkConnection<TCounters, TPackageInfo>
    where TCounters : NetworkConnectionCounters, new()
    where TPackageInfo : PackageInfo, new()
{
    public int connectionId;
    public INetworkTransport transport;

    public TCounters counters = new TCounters();

    public int rtt;                                 // Round trip time (ping + time lost due to read/send frequencies)

    public int inSequence;                          // The highest sequence of packages we have received
    public ushort inSequenceAckMask;                // The mask describing which of the last packages we have received relative to inSequence
    public long inSequenceTime;                     // The time the last package was received

    public int outSequence = 1;                     // The sequence of the next outgoing package
    public int outSequenceAck;                      // The highest sequence of packages that have been acked
    public ushort outSequenceAckMask;               // The mask describing which of the last packaged have been acked related to outSequence

    public NetworkConnection(int connectionId, INetworkTransport transport)
    {
        this.connectionId = connectionId;
        this.transport = transport;

        chokedTimeToNextPackage = 0;
    }

    public BinaryWriter debugSendStreamWriter;

    /// <summary>
    /// Called when the connection released (e.g. when the connection was disconnected)
    /// unlike Reset, which can be called multiple times on the connection in order to reset any
    /// state cached on the connection
    /// </summary>
    public virtual void Shutdown()
    {
        if (debugSendStreamWriter != null)
        {
            debugSendStreamWriter.Close();
            debugSendStreamWriter.Dispose();
            debugSendStreamWriter = null;
        }
    }

    /// <summary>
    /// Resets all cached connection state including reliable data pending acknowledgments
    /// </summary>
    public virtual void Reset()
    {
    }

    protected bool CanSendPackage(ref BitOutputStream output)
    {
        if (!outstandingPackages.Available(outSequence)) // running out here means we hit 64 packs without any acks from client...
        {
            // We have too many outstanding packages. We need the other end to send something to us, so we know he 
            // is alive. This happens for example when we break the client in the debugger while the server is still 
            // sending messages but potentially it could also happen in extreme cases of congestion or package loss. 
            // We will try to send empty packages with low frequency to see if we can get the connection up and running again

            if(Game.frameTime >= chokedTimeToNextPackage)
            {
                chokedTimeToNextPackage = Game.frameTime + NetworkConfig.netChokeSendInterval.FloatValue;

                // Treat the last package as lost
                int chokedSequence;
                var info = outstandingPackages.TryGetByIndex(outSequence % outstandingPackages.Capacity, out chokedSequence);
                GameDebug.Assert(info != null);

                NotifyDelivered(chokedSequence, info, false);

                counters.chokedPackagesOut++;

                info.Reset();
                outstandingPackages.Remove(chokedSequence);

                // Send empty package
                TPackageInfo emptyPackage;
                BeginSendPackage(ref output, out emptyPackage);
                CompleteSendPackage(emptyPackage, ref output);
            }
            return false;
        }
        return true;
    }

    // Returns the 'wide' packageSequenceNumber (i.e. 32 bit reconstructed from the 16bits sent over wire)
    protected int ProcessPackageHeader(byte[] packageData, int packageSize, out NetworkMessage content, out byte[] assembledData, out int assembledSize, out int headerSize)
    {
        counters.packagesIn++;
        assembledData = packageData;
        assembledSize = packageSize;
        headerSize = 0;
        var input = new BitInputStream(packageData);

        int headerStartInBits = input.GetBitPosition();

        content = (NetworkMessage)input.ReadBits(8);

        // TODO: Possible improvement is to ack on individual fragments not just entire message
        if ((content & NetworkMessage.FRAGMENT) != 0)
        {
            // Package fragment
            var fragmentPackageSequence = Sequence.FromUInt16((ushort)input.ReadBits(16), inSequence);
            var numFragments = (int)input.ReadBits(8);
            var fragmentIndex = (int)input.ReadBits(8);
            var fragmentSize = (int)input.ReadBits(16);

            FragmentReassemblyInfo assembly;
            if (!m_FragmentReassembly.TryGetValue(fragmentPackageSequence, out assembly))
            {
                // If we run out of room in the reassembly buffer we will not be able to reassemble this package
                if (!m_FragmentReassembly.Available(fragmentPackageSequence))
                    counters.fragmentedPackagesLostIn++;

                GameDebug.Assert(numFragments <= NetworkConfig.maxFragments);

                assembly = m_FragmentReassembly.Acquire(fragmentPackageSequence);
                assembly.numFragments = numFragments;
                assembly.receivedMask = 0;
                assembly.receivedCount = 0;
            }

            GameDebug.Assert(assembly.numFragments == numFragments);
            GameDebug.Assert(fragmentIndex < assembly.numFragments);
            counters.headerBitsIn += input.GetBitPosition() - headerStartInBits;

            if ((assembly.receivedMask & (1U << fragmentIndex)) != 0)
            {
                // Duplicate package fragment
                counters.packagesDuplicateIn++;
                return 0;
            }

            assembly.receivedMask |= 1U << fragmentIndex;
            assembly.receivedCount++;

            input.ReadBytes(assembly.data, fragmentIndex * NetworkConfig.packageFragmentSize, fragmentSize);

            if (assembly.receivedCount < assembly.numFragments)
            {
                return 0;   // Not fully assembled
            }

            // Continue processing package as we have now reassembled the package
            assembledData = assembly.data;
            assembledSize = fragmentIndex * NetworkConfig.packageFragmentSize + fragmentSize;
            input.Initialize(assembledData);
            headerStartInBits = 0;
            content = (NetworkMessage)input.ReadBits(8);
        }

        var inSequenceNew = Sequence.FromUInt16((ushort)input.ReadBits(16), inSequence);
        var outSequenceAckNew = Sequence.FromUInt16((ushort)input.ReadBits(16), outSequenceAck);
        var outSequenceAckMaskNew = (ushort)input.ReadBits(16);

        if (inSequenceNew > inSequence)
        {
            // If we have a hole in the package sequence that will fall off the ack mask that 
            // means the package (inSequenceNew-15 and before) will be considered lost (either it will never come or we will 
            // reject it as being stale if we get it at a later point in time)
            var distance = inSequenceNew - inSequence;
            for (var i = 0; i < Math.Min(distance, 15); ++i)    // TODO : Fix this contant
            {
                if ((inSequenceAckMask & 1 << (15 - i)) == 0)
                    counters.packagesLostIn++;
            }

            // If there is a really big hole then those packages are considered lost as well

            // Update the incoming ack mask.
            if (distance > 15)
            {
                counters.packagesLostIn += distance - 15;
                inSequenceAckMask = 1; // all is lost except current package
            }
            else
            {
                inSequenceAckMask <<= distance;
                inSequenceAckMask |= 1;
            }

            inSequence = inSequenceNew;
            inSequenceTime = NetworkUtils.stopwatch.ElapsedMilliseconds;
        }
        else if (inSequenceNew < inSequence)
        {
            // Package is out of order 

            // Check if the package is stale
            // NOTE : We rely on the fact that we will reject packages that we cannot ack due to the size
            // of the ack mask, so we don't have to worry about resending messages as long as we do that
            // after the original package has fallen off the ack mask.
            var distance = inSequence - inSequenceNew;
            if (distance > 15) // TODO : Fix this constant
            {
                counters.packagesStaleIn++;
                return 0;
            }

            // Check if the package is a duplicate
            var ackBit = 1 << distance;
            if ((ackBit & inSequenceAckMask) != 0)
            {
                // Duplicate package
                counters.packagesDuplicateIn++;
                return 0;
            }

            // Accept the package out of order
            counters.packagesOutOfOrderIn++;
            inSequenceAckMask |= (ushort)ackBit;
        }
        else
        {
            // Duplicate package
            counters.packagesDuplicateIn++;
            return 0;
        }

        if (inSequenceNew % 3 == 0)
        {
            var timeOnServer = (ushort)input.ReadBits(8);
            TPackageInfo info;
            if (outstandingPackages.TryGetValue(outSequenceAckNew, out info))
            {
                var now = NetworkUtils.stopwatch.ElapsedMilliseconds;
                rtt = (int)(now - info.sentTime - timeOnServer);
            }
        }

        // If the ack sequence is not higher we have nothing new to do
        if (outSequenceAckNew <= outSequenceAck)
        {
            headerSize = input.Align();
            return inSequenceNew;
        }

        // Find the sequence numbers that we have to consider lost
        var seqsBeforeThisAlreadyNotifedAsLost = outSequenceAck - 15;
        var seqsBeforeThisAreLost = outSequenceAckNew - 15;
        for (int sequence = seqsBeforeThisAlreadyNotifedAsLost; sequence <= seqsBeforeThisAreLost; ++sequence)
        {
            // Handle conditions before first 15 packets
            if (sequence < 0)
                continue;

            // If seqence covered by old ack mask, we may already have received it (and notified)
            int bitnum = outSequenceAck - sequence;
            var ackBit = bitnum >= 0 ? 1 << bitnum : 0;
            var notNotified = (ackBit & outSequenceAckMask) == 0;

            if (outstandingPackages.Exists(sequence) && notNotified)
            {
                var info = outstandingPackages[sequence];
                NotifyDelivered(sequence, info, false);

                counters.packagesLostOut++;
                if (info.fragmented)
                    counters.fragmentedPackagesLostOut++;

                info.Reset();
                outstandingPackages.Remove(sequence);
            }
        }

        outSequenceAck = outSequenceAckNew;
        outSequenceAckMask = outSequenceAckMaskNew;

        // Ack packages if they haven't been acked already
        for (var sequence = Math.Max(outSequenceAck - 15, 0); sequence <= outSequenceAck; ++sequence)
        {
            var ackBit = 1 << outSequenceAck - sequence;
            if (outstandingPackages.Exists(sequence) && (ackBit & outSequenceAckMask) != 0)
            {
                var info = outstandingPackages[sequence];
                NotifyDelivered(sequence, info, true);

                info.Reset();
                outstandingPackages.Remove(sequence);
            }
        }

        counters.headerBitsIn += input.GetBitPosition() - headerStartInBits;

        headerSize = input.Align();
        return inSequenceNew;
    }

    protected void BeginSendPackage(ref BitOutputStream output, out TPackageInfo info)
    {
        GameDebug.Assert(outstandingPackages.Available(outSequence), "NetworkConnection.BeginSendPackage : package info not available for sequence : {0}", outSequence);

        output.WriteBits(0, 8);                                 // Package content flags (will set later as we add messages)
        output.WriteBits(Sequence.ToUInt16(outSequence), 16);
        output.WriteBits(Sequence.ToUInt16(inSequence), 16);
        output.WriteBits(inSequenceAckMask, 16);

        // Send rtt info every 3th package. We calculate the RTT as the time from sending the package
        // and receiving the ack for the package minus the time the package spent on the server

        // TODO should this be sent from client to server?

        if (outSequence % 3 == 0)
        {
            var now = NetworkUtils.stopwatch.ElapsedMilliseconds;
            // TOULF Is 255 enough? 
            var timeOnServer = (byte)Math.Min(now - inSequenceTime, 255);
            output.WriteBits(timeOnServer, 8);
        }

        info = outstandingPackages.Acquire(outSequence);
    }

    protected void AddMessageContentFlag(NetworkMessage message)
    {
        m_PackageBuffer[0] |= (byte)message;
    }

    protected int CompleteSendPackage(TPackageInfo info, ref BitOutputStream output)
    {
        Profiler.BeginSample("NetworkConnection.CompleteSendPackage()");

        info.sentTime = NetworkUtils.stopwatch.ElapsedMilliseconds;
        info.content = (NetworkMessage)m_PackageBuffer[0];
        int packageSize = output.Flush();

        GameDebug.Assert(packageSize < NetworkConfig.maxPackageSize,"packageSize < NetworkConfig.maxPackageSize");

        if (debugSendStreamWriter != null)
        {
            debugSendStreamWriter.Write(m_PackageBuffer, 0, packageSize);
            debugSendStreamWriter.Write((UInt32)0xedededed);
        }

        if (packageSize > NetworkConfig.packageFragmentSize)
        {
            // Package is too big and needs to be sent as fragments
            var numFragments = packageSize / NetworkConfig.packageFragmentSize;
            //GameDebug.Log("FRAGMENTING: " + connectionId + ": " + packageSize + " (" + numFragments + ")");
            var lastFragmentSize = packageSize % NetworkConfig.packageFragmentSize;
            if (lastFragmentSize != 0)
                ++numFragments;
            else
                lastFragmentSize = NetworkConfig.packageFragmentSize;

            for (var i = 0; i < numFragments; ++i)
            {
                var fragmentSize = i < numFragments - 1 ? NetworkConfig.packageFragmentSize : lastFragmentSize;

                var fragmentOutput = new BitOutputStream(m_FragmentBuffer);
                fragmentOutput.WriteBits((uint)NetworkMessage.FRAGMENT, 8); // Package fragment identifier
                fragmentOutput.WriteBits(Sequence.ToUInt16(outSequence), 16);
                fragmentOutput.WriteBits((uint)numFragments, 8);
                fragmentOutput.WriteBits((uint)i, 8);
                fragmentOutput.WriteBits((uint)fragmentSize, 16);
                fragmentOutput.WriteBytes(m_PackageBuffer, i * NetworkConfig.packageFragmentSize, fragmentSize);
                int fragmentPackageSize = fragmentOutput.Flush();

                transport.SendData(connectionId, m_FragmentBuffer, fragmentPackageSize);
                counters.packagesOut++;
                counters.bytesOut += fragmentPackageSize;
            }
            counters.fragmentedPackagesOut++;
        }
        else
        {
            transport.SendData(connectionId, m_PackageBuffer, packageSize);
            counters.packagesOut++;
            counters.bytesOut += packageSize;
        }

        ++outSequence;

        Profiler.EndSample();

        return packageSize;
    }

    protected virtual void NotifyDelivered(int sequence, TPackageInfo info, bool madeIt)
    {
        if (madeIt)
        {
            // Release received reliable events
            foreach (var eventInfo in info.events)
            {
                if (!ackedEventTypes.Contains(eventInfo.type))
                    ackedEventTypes.Add(eventInfo.type);
                eventInfo.Release();
            }
        }
        else
        {
            foreach (var eventInfo in info.events)
            {
                counters.eventsLostOut++;
                if (eventInfo.reliable)
                {
                    // Re-add dropped reliable events to outgoing events
                    counters.reliableEventResendOut++;
                    GameDebug.Log("Resending lost reliable event: " + ((GameNetworkEvents.EventType)eventInfo.type.typeId) + ":" +eventInfo.sequence);
                    eventsOut.Add(eventInfo);
                }
                else
                    eventInfo.Release();
            }
        }
        info.events.Clear();
    }

    // Events handling

    public void QueueEvent(NetworkEvent info)
    {
        eventsOut.Add(info);
        info.AddRef();
    }

    public void ReadEvents<TInputStream>(ref TInputStream input, INetworkCallbacks networkConsumer) where TInputStream : NetworkCompression.IInputStream
    {
        //input.SetStatsType(NetworkCompressionReader.Type.Event);
        var numEvents = NetworkEvent.ReadEvents(eventTypesIn, connectionId, ref input, networkConsumer);
        counters.eventsIn += numEvents;
    }

    public void WriteEvents<TOutputStream>(TPackageInfo info, ref TOutputStream output) where TOutputStream : NetworkCompression.IOutputStream
    {
        if (eventsOut.Count == 0)
            return;

        foreach (var eventInfo in eventsOut)
        {
            counters.eventsOut++;
            if (eventInfo.reliable)
                counters.reliableEventsOut++;
        }

        AddMessageContentFlag(NetworkMessage.Events);

        GameDebug.Assert(info.events.Count == 0);
        NetworkEvent.WriteEvents(eventsOut, ackedEventTypes, ref output);
        info.events.AddRange(eventsOut);
        eventsOut.Clear();
    }

    double chokedTimeToNextPackage;
    public SequenceBuffer<TPackageInfo> outstandingPackages = new SequenceBuffer<TPackageInfo>(64, () => new TPackageInfo());

    class FragmentReassemblyInfo
    {
        public int numFragments;
        public uint receivedMask;
        public uint receivedCount;
        public byte[] data = new byte[1024 * 64];
    }

    SequenceBuffer<FragmentReassemblyInfo> m_FragmentReassembly = new SequenceBuffer<FragmentReassemblyInfo>(8, () => new FragmentReassemblyInfo());

    byte[] m_FragmentBuffer = new byte[2048];
    public byte[] m_PackageBuffer = new byte[1024 * 64];    //TODO: fix this

    // Events
    Dictionary<ushort, NetworkEventType> eventTypesIn = new Dictionary<ushort, NetworkEventType>();
    List<NetworkEventType> ackedEventTypes = new List<NetworkEventType>();
    public List<NetworkEvent> eventsOut = new List<NetworkEvent>(); // TODO : Should be private (content calc issue)
}
