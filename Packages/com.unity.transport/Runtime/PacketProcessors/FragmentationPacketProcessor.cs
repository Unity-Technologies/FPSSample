using System;
using Unity.Collections;

namespace Unity.Networking.Transport.PacketProcessors
{
    // Note should be packed, but not worried about real size at this time
    public struct FragmentedPacket
    {
        public int ID; // some sort of identifier to indicate which original packet it belongs to

        public int
            SequenceNum; //+ve indicates pos, -ve indicates spill over size (otherwise assumed to be fixed size e.g. 1K ?)

        public int SequenceCnt; // total num fragments
        public NativeSlice<byte> packetData;
    }

    public struct RecombineSlot
    {
        public bool[] available;

        public NativeArray<byte> packetData;

        // public byte[] packetData;
        public int defragID;
        public int sequenceCnt;
        public int lastLen;
    }

    public class Fragmenter
    {
        // Parameters for tuning - note no reliabilty is used on fragmented packets, so significant OOO or dropped packets will result in poor final results

        public const int MAX_IN_FLIGHT_PACKETS = 64; // Maximum number of packets we can have in flight
        public const int MAX_INITIAL_PACKET_SIZE = 65536; // Size of largest packet we can fragment

        public const int
            FRAG_SIZE =
                1200 - (3 * sizeof(int)); // Size of packet, not currently defined as 1200 including FragmentedPacketHeader

        // Fragment Packet definition
        RecombineSlot[] defragBuffers;

        public NativeQueue<FragmentedPacket> fragmentedOutgoing;

        public Fragmenter()
        {
            fragmentedOutgoing = new NativeQueue<FragmentedPacket>(Allocator.Persistent);

            defragBuffers = new RecombineSlot[MAX_IN_FLIGHT_PACKETS];
            // initialize
            for (int i = 0; i < MAX_IN_FLIGHT_PACKETS; i++)
            {
                //defragBuffers[i].packetData = new byte[MAX_INITIAL_PACKET_SIZE];  // Probably fine to upfront initialise this storage and keep it forever
                defragBuffers[i].packetData = new NativeArray<byte>(MAX_INITIAL_PACKET_SIZE, Allocator.Persistent);
                defragBuffers[i].available =
                    new bool[(MAX_INITIAL_PACKET_SIZE + (FRAG_SIZE - 1)) /
                             FRAG_SIZE]; // Again assumes we have a fixed MAX packet size
                defragBuffers[i].defragID =
                    (i == 0)
                        ? 1
                        : 0; // 1 is an invalid id for the first slot, 0 is invalid for the others (modulo arithmetic)
            }
        }

        public void Dispose()
        {
            if (fragmentedOutgoing.IsCreated)
                fragmentedOutgoing.Dispose();

            for (int i = 0; i < MAX_IN_FLIGHT_PACKETS; i++)
            {
                if (defragBuffers[i].packetData.IsCreated)
                    defragBuffers[i].packetData.Dispose();
            }
        }

        public void FragmentPacket(NativeSlice<byte> packet, int sequence)
        {
            int packetSize = (int) packet.Length; // should assert against too big for int16

            int numFragments =
                (int) ((packetSize + (FRAG_SIZE - 1)) / FRAG_SIZE); // should assert against too many fragments here
            int cFragment = 0;
            int skipAmount = 0;

            while (packetSize != 0)
            {
                int currentFragSize = Math.Min(packetSize, FRAG_SIZE);

                FragmentedPacket tFrag = new FragmentedPacket();

                tFrag.ID = sequence;
                tFrag.SequenceCnt = numFragments;
                tFrag.SequenceNum = ((cFragment + 1) == numFragments) ? (int) (-currentFragSize) : cFragment++;

                tFrag.packetData = packet.Slice(skipAmount, currentFragSize);

                fragmentedOutgoing.Enqueue(tFrag);

                skipAmount += currentFragSize;
                packetSize -= currentFragSize;
            }

            // globalPacketID++;
        }

        static bool IsDone(ref RecombineSlot slot)
        {
            bool retVal = true;
            for (int i = 0; i < slot.sequenceCnt; i++)
                //foreach (bool b in avail)
            {
                retVal &= slot.available[i];
            }

            return retVal;
        }

        static bool IsZero(ref RecombineSlot slot)
        {
            bool retVal = false;
            for (int i = 0; i < slot.sequenceCnt; i++)
                //foreach (bool b in avail)
            {
                retVal |= slot.available[i];
            }

            return !retVal;
        }

        //public bool DefragmentPacket(NativeSlice<byte> fragment, NativeSlice<byte> packet)
        public bool DefragmentPacket(FragmentedPacket incoming, out NativeSlice<byte> packet)
        {
            var fragID = incoming.ID % MAX_IN_FLIGHT_PACKETS;
            packet = default(NativeSlice<byte>);

            // insert packet data into recombiner packet --- should check and discard BAD packets
            if ((incoming.SequenceCnt > defragBuffers[fragID].available.Length) ||
                (incoming.SequenceNum >= incoming.SequenceCnt) ||
                ((incoming.SequenceNum < 0) && ((-incoming.SequenceNum) != incoming.packetData.Length)))
            {
                // Bad incoming packet, discard - dump warning to console
                //Debug.Log("Dropping bad incoming packet");

                return false;
            }

            if (defragBuffers[fragID].defragID != incoming.ID)
            {
                // Check for dropped packet - to keep unit tests sane
                if (!IsDone(ref defragBuffers[fragID]) && !IsZero(ref defragBuffers[fragID]))
                {
                    // Dropped at least one message - dump warning to console, set flag to stop assert on wrong number of packets
                    UnityEngine.Debug.Log("Dropping partial buffer due to too many inflight packets");
                    //hasDroppedPackets = true;
                }

                // new packet to decode
                System.Array.Clear(defragBuffers[fragID].available, 0, defragBuffers[fragID].available.Length);
                defragBuffers[fragID].defragID = incoming.ID;
            }

            int length, position, idx;

            if (incoming.SequenceNum < 0)
            {
                // final part of original packet
                length = -incoming.SequenceNum;
                idx = incoming.SequenceCnt - 1;

                defragBuffers[fragID].lastLen = length;
            }
            else
            {
                length = FRAG_SIZE;
                idx = incoming.SequenceNum;
            }

            position = idx * FRAG_SIZE;
            defragBuffers[fragID].available[idx] = true;

            defragBuffers[fragID].sequenceCnt = incoming.SequenceCnt;

            defragBuffers[fragID].packetData.Slice(position, length).CopyFrom(incoming.packetData.Slice());
            //Array.Copy(incoming.packetData, 0, defragBuffers[fragID].packetData, position, length);

            if (IsDone(ref defragBuffers[fragID]))
            {
                // Enqueue packet to incoming packet stream - NOTE we must take a value copy here!
                //incomingPacketQueue.Enqueue((byte[])defragBuffers[fragID].packetData.Clone());
                //defragBuffers[fragID].packetData
                int len = ((defragBuffers[fragID].sequenceCnt - 1) * FRAG_SIZE) + defragBuffers[fragID].lastLen;

                packet = new NativeSlice<byte>(defragBuffers[fragID].packetData, 0, len);

                return true;
            }

            return false;
        }
    }
}