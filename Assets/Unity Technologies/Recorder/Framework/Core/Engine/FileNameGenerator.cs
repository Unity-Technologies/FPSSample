using System;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;

namespace UnityEngine.Recorder
{
    /// <summary>
    /// What is this: Utility class that generates file names and supports in-place tag replacement.
    /// Motivation  : Most recorders output to files and having a comon implementation that supports
    ///               most often used file name generation is quite convenient.
    /// Notes       : 
    ///     - Since most recorders will use this, RecorderSettings actually has one by default. 
    ///     - Does not take path into consideration and output does not include it.
    /// </summary>    
    [Serializable]
    public struct FileNameGenerator
    {
        public static string[] tagLabels { get; private set; }
        public static string[] tags { get; private set; }
        static string m_projectName;

        public enum ETags
        {
            Time,
            Date,
            Project,
            Product,
            Scene,
            Resolution,
            Frame,
            Extension
        }

        [SerializeField]
        string m_Pattern;

        string m_FramePattern;
        string m_FramePatternDst;

        public string pattern {
            get { return m_Pattern;}
            set { m_Pattern = value;  }
        }

        static FileNameGenerator()
        {
            tags = new[]
            {
                "<ts>",  
                "<dt>",
                "<prj>",
                "<prd>",
                "<scn>",
                "<res>",
                "<00000>",
                "<ext>"
            };

            tagLabels = new[]
            {
                "<ts>  - Time",  
                "<dt>  - Date",
                "<prj> - Project name",
                "<prd> - Product name (editor only)",
                "<scn> - Scene name",
                "<res> - Resolution",
                "<000> - Frame number",
                "<ext> - Default extension"
            };
        }

        public static string AddTag(string pattern, ETags t)
        {
            if (!string.IsNullOrEmpty(pattern))
            {
                switch (t)
                {
                    case ETags.Frame:
                    case ETags.Extension:
                    {
                        pattern += ".";
                        break;
                    }
                    default:
                    {
                        pattern += "-";
                        break;
                    }
                }
            }

            pattern += tags[(int)t];

            return pattern;
        }

        public string BuildFileName( RecordingSession session, int frame, int width, int height, string ext )
        {
            if (string.IsNullOrEmpty(m_projectName))
            {
#if UNITY_EDITOR
                var parts = Application.dataPath.Split('/');
                m_projectName = parts[parts.Length - 2];                  
#else
                m_projectName = "N/A";
#endif
            }

            var regEx = new Regex("(<0*>)");
            var match = regEx.Match(pattern);
            if (match.Success)
            {
                m_FramePattern = match.Value;
                m_FramePatternDst = m_FramePattern.Substring(1,m_FramePattern.Length-2 );
            }
            else
            {
                m_FramePattern = "<0>";
                m_FramePatternDst = "0";
            }             

            var fileName  = pattern.Replace(tags[(int)ETags.Extension], ext)
                .Replace(tags[(int)ETags.Resolution], string.Format("{0}x{1}", width, height))
                .Replace(m_FramePattern, frame.ToString(m_FramePatternDst))
                .Replace(tags[(int)ETags.Scene], SceneManager.GetActiveScene().name)
                .Replace(tags[(int)ETags.Project], m_projectName)
#if UNITY_EDITOR
                .Replace(tags[(int)ETags.Product], UnityEditor.PlayerSettings.productName )
#else
                .Replace(tags[(int)ETags.Product], "(prd-NA)")
#endif
                .Replace(tags[(int)ETags.Time], string.Format( "{0}h{1}m",session.m_SessionStartTS.ToString("HH"),session.m_SessionStartTS.ToString("mm") ))
                .Replace(tags[(int)ETags.Date], session.m_SessionStartTS.ToShortDateString().Replace('/','-'))
                ;
            
            return fileName;
        }

    }
}
