using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Security.Cryptography;
using System;

public static class LogFrequency {
    public  static Dictionary<string, LogFreq> logFreqs = new Dictionary<string, LogFreq>() ;
    public static Byte[] buffer ;
    
    public class LogFreq {
        public int total;
        public int incr;
        public int maxPrint;
        public int totalPrint;
        public int nextPrint;
        public string message;
        public int total2;
        public int total3;
        public int total4;
    }

    static public void Incr(string logName, int incr2=0, int incr3=0, int incr4=0) {
        if (!logFreqs.ContainsKey(logName)) return;
        var logFreq = logFreqs[logName];
        logFreq.total++;
        logFreq.total2 += incr2;
        logFreq.total3 += incr3;
        logFreq.total4 += incr4;
        
        if (logFreq.total == logFreq.nextPrint &&(logFreq.maxPrint<0 ||  logFreq.totalPrint < logFreq.maxPrint)) {
            logFreq.totalPrint++;
            Print(logName);
            logFreq.nextPrint += logFreq.incr;
        }
    }

    static public void IncrPrintByteA(string logName, byte[] data, int length, int incr2=0, int incr3=0, int incr4=0
        ) {
        if (!logFreqs.ContainsKey(logName)) return;
        var logFreq = logFreqs[logName];
        logFreq.total++;
        logFreq.total2 += incr2;
        logFreq.total3 += incr3;
        logFreq.total4 += incr4;
        
        if (logFreq.total == logFreq.nextPrint &&(logFreq.maxPrint<0 ||  logFreq.totalPrint < logFreq.maxPrint)) {
            logFreq.totalPrint++;
            Print(logName, data, length);
            logFreq.nextPrint += logFreq.incr;
        }
    }

    static public void  Print(string logname, byte[] data, int length) {
        int displayLength = Mathf.Min(length, 100);
        buffer = new byte[displayLength]; 
        Array.Copy(data,buffer,displayLength);
        string hex = BitConverter.ToString(buffer);
        GameDebug.Log(logname + length + " : " +hex);
    }
    
    static public void Print(string logName) {
        if (!logFreqs.ContainsKey(logName)) return;
        var logFreq = logFreqs[logName];
        GameDebug.Log(logFreq.message + " t:"+Time.time+ " : "+ logFreq.total
                      + " t2: "+ logFreq.total2
                      + " t3: "+ logFreq.total3
                      + " t4: "+ logFreq.total4
                      + " timestamp: "+ DateTime.Now.ToString()
        );
    }
    
    static public void AddLogFreq(string logName, int incr, string message, int maxPrint = -1) {
        logFreqs[logName] = new LogFreq() {
            incr = incr,
            message = message,
            nextPrint = 1,
            maxPrint = maxPrint

        };
    }
}
