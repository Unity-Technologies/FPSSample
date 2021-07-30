using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using BestHTTP;
using Debug = UnityEngine.Debug;

namespace Macrometa {
    
    public class TransportPings {
        /// <summary>
        /// All pings access should be main thread only.
        /// </summary>
        public static Dictionary<int, TransportPing> pings = new Dictionary<int, TransportPing>();
        /// <summary>
        /// store time (unity Time.time) to sens first ping for an id
        /// </summary>
        public static Dictionary<int, float> firstPingTimes = new Dictionary<int, float>();
        
        public static SimplePool<Stopwatch> stopWatchPool =
            new SimplePool<Stopwatch>(()=>Stopwatch.StartNew());
        
        private static int _counter;

        // using thread safe ID generation means TryAdd, TryGetVaule, TryRemove, TryUpdate
        // are easier use
        public static int GetNewId() {
            return Interlocked.Increment(ref _counter);
        }
        public static int Add( int destinationId, float sendTime, float receiveTime) {
            var id = GetNewId();

            var stopwatch = stopWatchPool.Get();
            stopwatch.Reset();
            stopwatch.Start();
            var transportPing = new TransportPing() {
                id = id,
                destinationId = destinationId,
                stopwatch = stopwatch,
            };
            pings.Add(id, transportPing);
            return id;
        }

        /// <summary>
        /// count all pings by desitnationId
        /// if count > pinLimit 
        /// </summary>
        public static List<int> HeartbeatCheck(int pingLimit) {
            var counts = new Dictionary<int, int>();
            var result = new List<int>();
            foreach( var transportPing in pings.Values) {
                if (counts.ContainsKey(transportPing.destinationId)) {
                    counts[transportPing.destinationId]++;
                }
                else {
                    counts[transportPing.destinationId] = 1;
                }
            }
            foreach (var kvp in counts) {
                if (kvp.Value > pingLimit) {
                    result.Add(kvp.Key);
                }
            }
            return result;
        }

        public static void RemoveDestinationId(int destinationID) {
            firstPingTimes.Remove(destinationID);
            var ids = new List<int>();
            foreach (var kvp in pings) {
                if (kvp.Value.destinationId == destinationID) {
                    ids.Add(kvp.Key);
                }
            }
            foreach (int id in ids) {
                pings.Remove(id);
                Debug.Log("pings id: "+ id + "  destinationID: "+ destinationID );
            }
        }
        
        
        public static TransportPing Remove(int id) {
            TransportPing result;
            if (pings.ContainsKey(id)) {
                result = pings[id];
                pings.Remove(id);
            } else {
                GameDebug.LogError("Could not find ping: " + id);
                return new TransportPing {id = -1, elapsedTime = -1, stopwatch = null};
            }

            if (result.stopwatch == null) {
                GameDebug.LogError("missing stopwatch ping: " + id);
            }
            result.elapsedTime = result.stopwatch.ElapsedMilliseconds;
            stopWatchPool.Return(result.stopwatch);
            result.stopwatch = null;
            return result;
        }
    }
    
    
    /// <summary>
    /// Track GDN Return Trip time message
    /// </summary>
    public struct TransportPing {
        public int id;
        public int destinationId;
        public Stopwatch stopwatch;
        public long elapsedTime;
        
        public override string ToString() {
            return destinationId + " : " +elapsedTime.ToString();
        }
    }

    

    public class PingStatsGroup {
        public static int latencyGroupSize = 10; //10 seconds 

        static System.IO.StreamWriter logFile = null;

        public StreamStats outStreamStats;
        public StreamStats inStreamStats;

        public string localNodeId; // node Id
        public string remoteNodeId; // node Id
        public string localId; // localLocation Id 
        public string remoteId; // remoteLocation Ide.g. Grant Tokyo

        public int connectionId;
        public string streamOutName;
        public StreamType streamOutType;
        public DataType streamOutData;
        public string streamInName;
        public StreamType streamInType;
        public DataType streamInData;

        public int latencyCurrentCount = 0;

        public float streamOutLocalPingAverage;
        public float streamOutRemotePingAverage;
        public float streamInLocalPingAverage;
        public float streamInRemotePingAverage;

        public TotalStat streamOutMessages = new TotalStat();
        public TotalStat streamOutBytes = new TotalStat();
        public TotalStat streamInMessages = new TotalStat();
        public TotalStat streamInBytes = new TotalStat();

        public float streamOutLocalPingTotal;
        public float streamOutRemotePingTotal;
        public float streamInLocalPingTotal;
        public float streamInRemotePingTotal;

        public float rttTotal = 0;
        public float rttAverage;
        public float totalPingAverage; // half of the total of the  4 ping stream averages
        public float extraAverage; // rttAverage - totalPingAverage
        public DateTime dateTime;


        public static void Init(string logfilePath, string logBaseName, int aLatencyGroupSize) {

            // Try creating logName; attempt a number of suffixes
            string name = "";
            for (var i = 0; i < 10; i++) {
                name = logBaseName + (i == 0 ? "" : "_" + i) + ".csv";
                try {
                    logFile = System.IO.File.CreateText(logfilePath + "/" + name);
                    logFile.AutoFlush = true;
                    Log(csvHeader);
                    break;
                }
                catch {
                    name = "<none>";
                }
            }

            latencyGroupSize = aLatencyGroupSize;
            GameDebug.Log("Stats logging initialized. Logging to " + logfilePath + "/" + name);
        }

        /// <summary>
        /// extras information GDNData for including in logs
        /// 
        /// </summary>
        public void InitStatsFromGDNDate(GDNData gdnData) {
            localNodeId = NodeFromGDNData(gdnData);
        }

        /// <summary>
        /// extract node from GDNData.federationURL
        /// </summary>
        /// <param name="gdnData"></param>
        /// <returns></returns>
        public static string NodeFromGDNData(GDNData gdnData) {
            Match match = Regex.Match(gdnData.federationURL, @"^(.*)\.macrometa\.io$",
                RegexOptions.IgnoreCase);

            if (match.Success) {
                return match.Groups[1].Value;
            }
            else {
                return "";
            }
        }


        static void Log(string message) {
            if (logFile != null)
                logFile.WriteLine(message + "\n");
        }

        public void SetStreamStats(StreamStats streamStats, bool isInStream) {
            if (isInStream) {
                inStreamStats = streamStats;
                streamInName = streamStats.streamName;
                streamInType = streamStats.streamType;
                streamInData = streamStats.dataType;
            }
            else {
                outStreamStats = streamStats;
                streamOutName = streamStats.streamName;
                streamOutType = streamStats.streamType;
                streamOutData = streamStats.dataType;
            }
        }

        public static string csvHeader = "dateTime, localNodeId,remoteNodeId, localId ,remoteId ," +
                                         "connectionId , streamOutName, streamOutType, streamOutData  ," +
                                         " streamInName , streamInType, streamInData," +
                                         "rttAverage  , totalPingAverage  ,extraAverage ," +
                                         "streamOutLocalPingAverage ,streamOutRemotePingAverage ," +
                                         "streamInLocalPingAverage ,streamInRemotePingAverage  , streamOutMessages," +
                                         " streamOutBytes, streamInMessages , streamInBytes , latencyGroupSize";

        public string ToCSVLine() {
            return "" + dateTime + "," + localNodeId + "," + remoteNodeId + "," + localId + "," + remoteId + "," +
                   connectionId + "," + streamOutName + "," + streamOutType + "," + streamOutData + "," +
                   streamInName + "," + streamInType + "," + streamInData + "," +
                   rttAverage + "," + totalPingAverage + "," + extraAverage + "," +
                   streamOutLocalPingAverage + "," + streamOutRemotePingAverage + "," +
                   streamInLocalPingAverage + "," + streamInRemotePingAverage + "," + streamOutMessages
                   + "," + streamOutBytes + "," + streamInMessages + "," + streamInBytes + "," + latencyGroupSize;
        }

        public NetworkStatsData CurrentNetWorkStats() {
            return new NetworkStatsData() {
                dateTime = dateTime,
                localNodeId = localNodeId,
                localId = localId,
                remoteId = remoteId,
                connectionId = connectionId,
                streamOutName = streamOutName,
                streamInName = streamInName,
                rttAverage = rttAverage,
                streamOutLocalPingAverage = streamOutLocalPingAverage,
                streamOutRemotePingAverage = streamOutRemotePingAverage,
                streamInLocalPingAverage = streamInLocalPingAverage,
                streamInRemotePingAverage = streamInRemotePingAverage,
                streamOutMessages = (int) streamOutMessages.total,
                streamInMessages = (int) streamInMessages.total,
                streamOutBytes = (int) streamOutBytes.total,
                streamInBytes = (int) streamInBytes.total,
                secondsInGroup = latencyGroupSize
            };
        }
        public class NetworkStatsData {
            public float rttAverage;
            public int streamOutMessages; //number of messages sent in time period
            public int streamInMessages;  //number of messages recieved in time period
            public int streamOutBytes;    //number of bytes sent in time period
            public int streamInBytes;     //number of bytes recieved in time period
            public int secondsInGroup; 
            public float streamOutLocalPingAverage;
            public float streamOutRemotePingAverage;
            public float streamInLocalPingAverage;
            public float streamInRemotePingAverage;
            public DateTime dateTime;  // string but unix time stamp would be better
            public string localNodeId; // not full domain name missing macrometa.io
            public string localId;     
            public string remoteId;
            public int connectionId;   // used to identify clients by int
            public string streamOutName; 
            public string streamInName;
            

               // number of seconds in time period
        }


    
        
        public NetworkStatsData AddRtt(float rtt, float outLocalPing, float inLocalPing,
            float outRemotePing,float inRemotePing, string aNodeId) {
            NetworkStatsData result= null;
            rttTotal += rtt;
            streamOutLocalPingTotal += outLocalPing;
            streamInLocalPingTotal += inLocalPing;
            streamOutRemotePingTotal += outRemotePing;
            streamInRemotePingTotal += inRemotePing;
            remoteNodeId = aNodeId;
            latencyCurrentCount++;
            if (latencyCurrentCount == latencyGroupSize) {
                result =GenerateStatsNow();
                GameDebug.Log(SimpleStats());
                GameDebug.Log(SimpleStats2());

            }
            return result;
        }
        
        public NetworkStatsData GenerateStatsNow() {
            dateTime = DateTime.Now;
            streamOutBytes.total = outStreamStats.bytesSent;
            streamOutMessages.total = outStreamStats.messageCount;
            streamInBytes.total = inStreamStats.bytesSent;
            streamInMessages.total = inStreamStats.messageCount;
            
            streamOutBytes.SetCurr();
            streamOutMessages.SetCurr();
            streamInBytes.SetCurr(); 
            streamInMessages.SetCurr(); 
            
            streamOutBytes.SetVal();
            streamOutMessages.SetVal();
            streamInBytes.SetVal(); 
            streamInMessages.SetVal(); 
            
            
            //calculate averages
            rttAverage = rttTotal / (float)latencyCurrentCount;
            streamOutLocalPingAverage = streamOutLocalPingTotal / (float)latencyCurrentCount;
            streamInLocalPingAverage = streamInLocalPingTotal / (float)latencyCurrentCount;
            streamOutRemotePingAverage = streamOutRemotePingTotal / (float)latencyCurrentCount;
            streamInRemotePingAverage = streamInRemotePingTotal / (float)latencyCurrentCount;
            totalPingAverage = (streamOutLocalPingAverage + streamInLocalPingAverage +
                               streamOutRemotePingAverage +  streamOutRemotePingAverage)/2.0f;
            extraAverage = rttAverage - totalPingAverage;

            
            //reset totals to zero
            rttTotal = 0;
            streamOutLocalPingTotal = 0;
            streamInLocalPingTotal = 0;
            streamOutRemotePingTotal = 0;
            streamInRemotePingTotal = 0;
            latencyCurrentCount = 0;
            
            streamOutBytes.SetPrev();
            streamOutMessages.SetPrev();
            streamInBytes.SetPrev(); 
            streamInMessages.SetPrev(); 
            
            
            Log(ToCSVLine());
            return CurrentNetWorkStats();
        }

        public class TotalStat {
            public float total;
            public float curr;
            public float prev;
            public float val;

            public void SetCurr() {
                curr = total;
            }

            public void SetVal() {
                val = curr - prev;
            }

            public void SetPrev() {
                prev = curr;
            }

            public override string ToString() {
               return val.ToString();
            }
        }
        
        public string SimpleStats() {
            return "id: "+ connectionId + " rtt: " + rttAverage + " ping: " + totalPingAverage / 2.0f + " remain: " + extraAverage;
        }

        public string SimpleStats2() {
            return " Stream out: " + streamOutBytes + " Stream In: " + streamInBytes +
                   " Stream msgs: " + streamOutMessages + " Stream msgs: " + streamInMessages +
                   " " + dateTime.ToString();
        }
        
    }
    
    public enum StreamType {
        Shared,
        Exclusive
    }

    public enum DataType {
        JSON,
        Byte
    }

    public class StreamStats {
        public string streamName;
        public StreamType streamType;
        public DataType dataType;
        public int messageCount;
        public int bytesSent;

        /// <summary>
        /// increment count od data passing through a stream
        /// This is NOT thread safe but only thread from the streams web socket should be writing here
        /// a PingStatsGroup state group only reads and it does not matter if reads before after or during increment
        /// multitple  PingStatsGroup can read StreamStats
        /// </summary>
        /// <param name="dataAmount"> number of bytes passingthrough stream</param>
        /// <param name="messageCountIncr"> number of messages passing through stream</param>
        public void IncrementCounts(int dataAmount, int messageCountIncr = 1) {
            bytesSent += dataAmount;
            messageCount += messageCountIncr;
        }

    }
}