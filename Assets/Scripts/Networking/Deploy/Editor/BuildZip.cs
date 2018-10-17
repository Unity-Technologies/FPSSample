using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ConnectedGames.Build
{
    class BuildZip
    {
        string m_SrcPath;
        string m_DstPath;
        Process m_Process;
        int m_Progress;
        Stopwatch m_Timer = new Stopwatch();
        string m_Platform;
        
        // NOTE: Zipping doesn't use label, this is only here in case this zip is supposed to be uploaded
        // then the label will be used in the upload call
        string m_Label;

        public long ElapsedTime
        {
            get { return m_Timer.ElapsedMilliseconds / 1000; }
        }

        public int Progress
        {
            get { return m_Progress; }
        }

        public string FileName
        {
            get { return m_DstPath; }
        }

        public string Platform
        {
            get { return m_Platform; }
        }

        public string Label
        {
            get { return m_Label; }
        }

        public bool IsDone
        {
            get 
            { 
                UnityEngine.Debug.Assert(m_Process != null);
                return m_Process.HasExited;    
            }
        }

        public BuildZip(string srcPath, string dstPath, string platform, string label)
        {
            m_SrcPath = srcPath;
            m_DstPath = dstPath;
            m_Platform = platform;
            m_Label = label;
        }

        public bool StartZip()
        {
            if (File.Exists(m_DstPath))
            {
                File.Delete(m_DstPath);
            }
            var arguments = new StringBuilder();
            arguments.Append("a -bsp1 -bb3 -bt -mx1 \"");  //a -y -r -bsp1 -bse1 -bso1 {0} {1} -mx9
            arguments.Append(m_DstPath);
            arguments.Append("\" ");
            arguments.Append("\"" + m_SrcPath + "\" ");

            return Run7Zip("", arguments.ToString());
        }

        // TODO: On mac this process seems to hang for a long time sometimes
        // When that happens and there is a domain reload it loses the "upload 
        // after compress" state and it never happens (also progress bar is never cleared)
        bool Run7Zip(string workingDir, string arguments)
        {
            Regex REX_SevenZipStatus = new Regex(@"(?<p>[0-9]+)%");

            //Debug.Log("Args: " + arguments);

            var processName = "7za";
            if (Application.platform == RuntimePlatform.WindowsEditor)
                processName = "7z.exe";
            m_Process = new Process();
            m_Process.StartInfo.FileName = (UnityEditor.EditorApplication.applicationContentsPath + "/Tools/" + processName);
            m_Process.StartInfo.Arguments = arguments;
            m_Process.StartInfo.UseShellExecute = false;
            m_Process.StartInfo.WorkingDirectory = workingDir;
            m_Process.StartInfo.CreateNoWindow = true;
            m_Process.StartInfo.RedirectStandardOutput = true;
            m_Process.OutputDataReceived += (sender, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    Match m = REX_SevenZipStatus.Match(e.Data ?? "");
                    if (m != null && m.Success)
                    {
                        m_Progress = int.Parse(m.Groups["p"].Value);                  
                    }
                }
            };
            m_Process.Exited += (obj, e) =>
            {
                UnityEngine.Debug.Log("Compression done in " + ElapsedTime);
                m_Timer.Stop();
            };

            m_Timer.Start();
            bool ret = m_Process.Start();
            if(ret)
                m_Process.BeginOutputReadLine();
            return ret;
        }
    }
}
