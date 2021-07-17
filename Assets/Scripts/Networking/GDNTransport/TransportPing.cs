using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace Macrometa {


    public class TransportPings {
        public static Dictionary<int, TransportPing> pings = new Dictionary<int, TransportPing>();
        public static int nextId = 0;
        
        

        public static int Add(int id, string name, float sendTime, float receiveTime) {
            if (id <0) {
                id = nextId;
                nextId++;
            }

            TransportPing transportPing;
            if (pings.ContainsKey(id)) {
                transportPing = pings[id];
                if (sendTime != 0) {
                    transportPing.times[name] = new Vector2(sendTime, 0);
                }
                else {
                    transportPing.times[name] = new Vector2(transportPing.times[name].x,receiveTime);
                }
            }
            else {
                transportPing = new TransportPing() {
                    id = id,
                    times = new Dictionary<string, Vector2>() {
                        {name, new Vector2(sendTime,0)}
                    }
                };
            }
            pings[id]=transportPing;
            return id;
        }

        public static TransportPing Remove(int id) {
            var result = pings[id];
            pings.Remove(id);
            return result;
        }
    }
    
    
    /// <summary>
    /// Track GDN Return Trip time message
    /// 
    /// </summary>
    public struct TransportPing {
        public int id;
        public Dictionary<string, Vector2> times; // time name => (sendTime,receiveTime)


        public override string ToString() {

            StringBuilder result = new StringBuilder();
            foreach (var kvp in times) {
                result.Append("( "+kvp.Key + " : " + ( kvp.Value.y -  kvp.Value.x) + " ) ");
            }
            return result.ToString();
        }
    }
}