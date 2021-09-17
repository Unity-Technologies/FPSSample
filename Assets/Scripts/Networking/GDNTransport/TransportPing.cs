using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;

using System.Linq;

using System.Text.RegularExpressions;
using System.Threading;
using BestHTTP;


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
        /// reurn list of ping Ids where count > pinLimit
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
                //Debug.Log("pings id: "+ id + "  destinationID: "+ destinationID );
            }
        }
        
        
        public static TransportPing Remove(int id) {
            //GameDebug.Log("Ping Remove count: "+ pings.Count);
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
            //GameDebug.Log("Ping Removed OK: "+ id + " : " + result.elapsedTime);
            stopWatchPool.Return(result.stopwatch);
            result.stopwatch = null;
            return result;
        }

        /// <summary>
        /// used whne shutting down all streams 
        /// </summary>
        /// <returns></returns>
        public static void  Clear() {
            foreach (var ping in pings) {
                stopWatchPool.Return(ping.Value.stopwatch);
            }
            pings.Clear();
        }

        /// <summary>
        /// return elapsed time on first ping or 0 if no pings
        /// </summary>
        /// <returns></returns>
        public static  float PingTime() {
            if (pings.Count == 0) {
                return 0;
            }
            var ping = pings.First().Value;
            return ping.stopwatch.ElapsedMilliseconds;
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
        public static int latencyGroupSize = 1; //1 seconds 

        static System.IO.StreamWriter logFile = null;

        public StreamStats outStreamStats;
        public StreamStats inStreamStats;

        public string appType;
        public string localNodeId; // node Id
        public string remoteNodeId; // node Id
        
        public string localHost;  //datacenter ip
        public string localCity;
        public string localCountrycode;
        public string remoteHost;  //datacenter ip
        public string remoteCity;
        public string remoteCountrycode;
        
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
/*
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
*/
            latencyGroupSize = aLatencyGroupSize;
           // GameDebug.Log("Stats logging initialized. Logging to " + logfilePath + "/" + name);
        }

        /// <summary>
        /// extras information Region for including in logs
        /// 
        /// </summary>
        public void InitStatsFromRegion(Region region) {
            localHost = region.host;
            localCity = region.locationInfo.city;
            localCountrycode = region.locationInfo.countrycode;
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

        public static string csvHeader = "dateTime, localNodeId, " +
                                         "localHost,localCity,localCountrycode," +
                                         "remoteNodeId,remoteHost,remoteCity,remoteCountrycode," +
                                         " localId ,remoteId ," +
                                         "connectionId , streamOutName, streamOutType, streamOutData  ," +
                                         " streamInName , streamInType, streamInData," +
                                         "rttAverage  , totalPingAverage  ,extraAverage ," +
                                         "streamOutLocalPingAverage ,streamOutRemotePingAverage ," +
                                         "streamInLocalPingAverage ,streamInRemotePingAverage  , streamOutMessages," +
                                         " streamOutBytes, streamInMessages , streamInBytes , latencyGroupSize";

        public string ToCSVLine() {
            return "" + dateTime + "," + localNodeId + "," + localHost + "," + localCity + "," + localCountrycode+ "," 
                   + remoteNodeId + "," + remoteHost + "," + remoteCity + "," + remoteCountrycode + "," 
                   + localId + "," + remoteId + "," +
                   connectionId + "," + streamOutName + "," + streamOutType + "," + streamOutData + "," +
                   streamInName + "," + streamInType + "," + streamInData + "," +
                   rttAverage + "," + totalPingAverage + "," + extraAverage + "," +
                   streamOutLocalPingAverage + "," + streamOutRemotePingAverage + "," +
                   streamInLocalPingAverage + "," + streamInRemotePingAverage + "," + streamOutMessages
                   + "," + streamOutBytes + "," + streamInMessages + "," + streamInBytes + "," + latencyGroupSize;
        }

        public NetworkStatsData CurrentNetWorkStats() {
            return new NetworkStatsData() {
                dateTime =(long)(dateTime.
                    Subtract(new DateTime(1970, 1, 1))).TotalSeconds,
                appType = appType,
                localNodeId = localNodeId,
                localHost = localHost,  //datacenter ip
                localCity = localCity,
                localCountrycode = localCountrycode,
                remoteHost = remoteHost,  //datacenter ip
                remoteCity = remoteCity, 
                remoteCountrycode = remoteCountrycode,
                localId = localId,
                remoteId = remoteId,
                connectionId = connectionId,
                streamOutName = streamOutName,
                streamInName = streamInName,
                rttAverage =  (int)rttAverage,
                streamOutLocalPingAverage = (int) streamOutLocalPingAverage,
                streamOutRemotePingAverage =  (int)streamOutRemotePingAverage,
                streamInLocalPingAverage = (int)streamInLocalPingAverage,
                streamInRemotePingAverage = (int)streamInRemotePingAverage,
                streamOutMessages = (int) streamOutMessages.val,
                streamInMessages = (int) streamInMessages.val,
                streamOutBytes = (int) streamOutBytes.val,
                streamInBytes = (int) streamInBytes.val,
                secondsInGroup = latencyGroupSize
            };
        }
        public class NetworkStatsData {
            public string version = "0.2";
            public string appType = "FPS";
            public int rttAverage; // in milliseconds 
            public int streamOutMessages; //number of messages sent in time period
            public int streamInMessages;  //number of messages recieved in time period
            public int streamOutBytes;    //number of bytes sent in time period
            public int streamInBytes;     //number of bytes recieved in time period
            public int secondsInGroup;   //length of time period in seconds
            public int streamOutLocalPingAverage; // in milliseconds 2 decimalplaces
            public int streamOutRemotePingAverage; // in milliseconds 2 decimalplaces
            public int streamInLocalPingAverage; // in milliseconds 2 decimalplaces
            public int streamInRemotePingAverage; // in milliseconds 2 decimalplaces
            public long dateTime;  // unix time stamp  UTC +0
            public string localNodeId; // not full domain name missing macrometa.io
            public string localHost;  //datacenter ip
            public string localCity;  //datacenter  city name
            public string localCountrycode; // datacenter country 2 characters
            public string remoteHost;  //datacenter ip for client end of connection
            public string remoteCity;
            public string remoteCountrycode; 
            public string localId;     //servername
            public string remoteId;     // client name 
            public int connectionId;   // used to identify clients by int
            public string sessionID;   // unique ID for each session of latency monitoring
            public string streamOutName; //stream server sends on
            public string streamInName;   //server stream receives on
            public int fps;
            
            
        }


    
        
        public NetworkStatsData AddRtt(float rtt, float outLocalPing, float inLocalPing,
            float outRemotePing,float inRemotePing, string remoteId, string host, string city, string countrycode) {
            NetworkStatsData result= null;
            rttTotal += rtt;
            streamOutLocalPingTotal += outLocalPing;
            streamInLocalPingTotal += inLocalPing;
            streamOutRemotePingTotal += outRemotePing;
            streamInRemotePingTotal += inRemotePing;
            this.remoteId = remoteId;
            remoteHost = host;
            remoteCity = city;
            remoteCountrycode = countrycode;
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
                   " " + dateTime.ToString() + " remoteId:" + remoteId;
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