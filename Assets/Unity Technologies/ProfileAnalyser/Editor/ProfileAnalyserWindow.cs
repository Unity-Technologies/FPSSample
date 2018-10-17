using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Reflection;
using UnityEditor.IMGUI.Controls;
using System.Text.RegularExpressions;
using System.Threading;

namespace ProfileAnalyser
{
    enum ThreadRange
    {
        Median,
        UpperQuartile,
        Max
    };

    enum ActiveTab
    {
        Summary,
        Compare,
    };

    public enum Mode
    {
        All,
        Time,
        Count,
        Custom,
    };

    enum ThreadActivity
    {
        None,
        Analyse,
        AnalyseDone,
        Compare,
        CompareDone,
    };

    public class ProfileAnalyserWindow : EditorWindow
    {
        static public Color Color256(int r, int g, int b, int a)
        {
            return new Color((float)r / 255.0f, (float)g / 255.0f, (float)b / 255.0f, (float)a / 255.0f);
        }

        private ProgressBarDisplay m_progressBar;
        private ProfileAnalyser m_profileAnalyser;

        private ProfilerWindowInterface m_profilerWindowInterface;
        private string m_lastProfilerSelectedMarker;

        private int m_firstFrameindex = 0;
        private int m_lastFrameindex = 0;

        private string[] m_depthStrings;
        private int[] m_depthValues;
        private int m_depthFilter = -1;
        private List<string> m_threadUINames = new List<string>();
        private List<string> m_threadFilters = new List<string>();
        private string m_threadFilter = "1:Main Thread";    // Default if it exists
        private string m_nameFilter = "";
        private string m_nameExclude = "";
        private string[] m_modeStrings;
        private int[] m_modeValues;
        private Mode m_modeFilter = Mode.All;

        private int m_profilerFirstFrameIndex = 0;
        private int m_profilerLastFrameIndex = 0;
        private int m_grabFirstFrameindex = 0;
        private int m_grabLastFrameindex = 0;

        private ActiveTab m_nextActiveTab = ActiveTab.Summary;
        private ActiveTab m_activeTab = ActiveTab.Summary;
        private bool m_otherTabDirty = false;

        private bool m_showFrameSummary = true;
        private bool m_showThreadSummary = false;
        private bool m_showMarkerSummary = true;

        private Material m_material;
        private int[] m_columnWidth = new int[4];

        public Color m_colorWhite = new Color(1.0f, 1.0f, 1.0f);
        public Color m_colorBarBackground = new Color(0.5f, 0.5f, 0.5f);
        public Color m_colorBoxAndWhiskerBackground = new Color(0.4f, 0.4f, 0.4f);
        public Color m_colorBar = new Color(0.95f, 0.95f, 0.95f);
        public Color m_colorStandardLine = new Color(1.0f, 1.0f, 1.0f);

        public Color m_colorLeft = Color256(111, 163, 216, 255);//Color256(0, 212, 254, 255);
        public Color m_colorRight = Color256(238, 134, 84, 255);//Color256(255, 45, 0, 255);
        public Color m_colorBoth = Color256(175, 150, 150, 255);//Color256(127, 127, 127, 255);

        public Color[] m_colorLeftBars = {
            Color256(59, 104, 144, 255),
            Color256(68, 118, 163, 255),
            Color256(75, 130, 179, 255),
            Color256(82, 140, 193, 255),
            Color256(88, 150, 206, 255),
            Color256(111, 163, 216, 255),
            Color256(143, 180, 222, 255),
            Color256(166, 193, 227, 255),
            Color256(187, 207, 233, 255),
            Color256(206, 219, 238, 255),
        };
        public Color[] m_colorRightBars = {
            Color256(161, 83, 30, 255),
            Color256(182, 94, 35, 255),
            Color256(200, 104, 40, 255),
            Color256(215, 113, 43, 255),
            Color256(230, 121, 47, 255),
            Color256(238, 134, 84, 255),
            Color256(241, 161, 128, 255),
            Color256(243, 179, 156, 255),
            Color256(245, 196, 180, 255),
            Color256(247, 212, 201, 255),
        };

        public Color[] m_colorBars = {
            Color256(112,112,112,255),
            Color256(120,120,120,255),
            Color256(128,128,128,255),
            Color256(136,136,136,255),
            Color256(144,144,144,255),
            Color256(152,152,152,255),
            Color256(160,160,160,255),
            Color256(168,168,168,255),
            Color256(176,176,176,255),
            Color256(184,184,184,255),
        }; 

        /*
         * // From http://ksrowell.com/blog-visualizing-data/2012/02/02/optimal-colors-for-graphs/
        public Color[] m_colorBars = {
            Color256(114,147,203,255),
            Color256(255,151,76,255),
            Color256(132,186,91,255),
            Color256(211,94,96,255),
            Color256(128,133,133,255),
            Color256(144,103,167,255),
            Color256(171,104,87,255),
            Color256(204,194,16,255),
            new Color(0.95f, 0.95f, 0.95f),
            new Color(0.0f, 0.0f, 0.0f),
        };
        */

        string m_profilePath;
        ProfileData m_profileData;
        ProfileAnalysis m_analysis;
        private int m_selectedMarker = 0;
        private string m_selectedMarkerName;

        string m_leftPath;
        string m_rightPath;
        ProfileData m_leftData;
        ProfileData m_rightData;
        ProfileAnalysis m_leftAnalysis;
        ProfileAnalysis m_rightAnalysis;
        List<MarkerPairing> m_pairings = new List<MarkerPairing>();
        int m_selectedPairing = 0;

        [SerializeField] TreeViewState m_profileTreeViewState;
        [SerializeField] MultiColumnHeaderState m_profileMulticolumnHeaderState;
        ProfileTable m_profileTable;

        [SerializeField] TreeViewState m_comparisonTreeViewState;
        [SerializeField] MultiColumnHeaderState m_comparisonMulticolumnHeaderState;
        ComparisonTable m_comparisonTable;

        static int m_windowWidth = 1170;
        static int m_windowHeight = 840;
        static int m_widthRHS = 270;
        static int m_widthColumn0 = 100;
        static int m_widthColumn1 = 50;
        static int m_widthColumn2 = 50;
        static int m_widthColumn3 = 50;

        GUIContent m_guiNameFilter = new GUIContent("Name Filter : ", "Only show markers containg all of the strings");
        GUIContent m_guiNameExclude = new GUIContent("Exclude names : ", "Excludes all markers containing any of the strings");
        GUIContent m_guiModeFilter = new GUIContent("Mode : ");

        ThreadRange m_threadRange = ThreadRange.UpperQuartile;
        string[] m_threadRanges = { "Median frame time", "Upper quartile of frame time", "Max frame time" };

        GUIStyle m_glStyle;

        bool m_async = true;
        Thread m_backgroundThread;
        ThreadActivity m_threadActivity;
        int m_threadPhase;
        int m_threadPhases;
        int m_threadProgress;

        [MenuItem("Window/Profile Analyser")]
        private static void Init()
        {
            var window = GetWindow<ProfileAnalyserWindow>("Profile Analyser");
            window.minSize = new Vector2(800, 480);
            window.position.size.Set(m_windowWidth, m_windowHeight);
            window.Show();
        }

        public bool CheckAndSetupMaterial()
        {
            if (m_material==null)
                m_material = new Material(Shader.Find("Unlit/ProfileAnalyserShader"));

            if (m_material == null)
                return false;

            return true;
        }

        private void OnEnable()
        {
            m_profilerWindowInterface = new ProfilerWindowInterface();

            m_progressBar = new ProgressBarDisplay();
            m_profileAnalyser = new ProfileAnalyser(m_progressBar);

            CheckAndSetupMaterial();

            m_modeStrings = Enum.GetNames(typeof(Mode));
            m_modeValues = (int[])Enum.GetValues(typeof(Mode));
            m_threadActivity = ThreadActivity.None;
            m_threadProgress = 0;
            m_threadPhase = 0;
        }

        private void OpenProfilerOrUseExisting()
        {
            m_profilerWindowInterface.OpenProfilerOrUseExisting();
            m_profilerWindowInterface.GetFrameRangeFromProfiler(out m_profilerFirstFrameIndex, out m_profilerLastFrameIndex);
            m_grabFirstFrameindex = m_profilerFirstFrameIndex;
            m_grabLastFrameindex = m_profilerLastFrameIndex;
        }

        private void OnGUI()
        {
            if (m_glStyle == null)
            {
                m_glStyle = new GUIStyle(GUI.skin.box);
                m_glStyle.padding = new RectOffset(0, 0, 0, 0);
                m_glStyle.margin = new RectOffset(0, 0, 0, 0);
            }

            Draw();
        }

		private void Update()
		{
            // Check if profiler is open
            if (m_profilerWindowInterface.IsReady())
            {
                // Check if the selected marker in the profiler has changed
                var selectedMarker = m_profilerWindowInterface.GetProfilerWindowMarkerName();
                if (selectedMarker != null && selectedMarker != m_lastProfilerSelectedMarker)
                {
                    m_lastProfilerSelectedMarker = selectedMarker;
                    SelectMarker(selectedMarker);
                }

                // Check if a new profile has been recorded (or loaded) by checking the frame index range.
                int first;
                int last;
                m_profilerWindowInterface.GetFrameRangeFromProfiler(out first, out last);
                if (first != m_profilerFirstFrameIndex || last != m_profilerLastFrameIndex)
                {
                    // Store the updated range and alter the grab range
                    m_profilerFirstFrameIndex = first;
                    m_profilerLastFrameIndex = last;
                    m_grabFirstFrameindex = m_profilerFirstFrameIndex;
                    m_grabLastFrameindex = m_profilerLastFrameIndex;
                }
            }
            else
            {
                if (m_profilerWindowInterface.IsProfilerWindowOpen())
                {
                    m_profilerWindowInterface.OpenProfilerOrUseExisting();
                }
            }


            // Deferred to here so drawing isn't messed up by chaning tab half way through a function rendering the old tab
            if (m_nextActiveTab != m_activeTab)
            {
                m_activeTab = m_nextActiveTab;
                //Debug.Log(string.Format("Setting tab to {0} and marker to {1}",m_activeTab.ToString(), m_selectedMarkerName));
                SelectMarker(m_selectedMarkerName);

                if (m_otherTabDirty)
                {
                    UpdateActiveTab(false);  // Make sure any depth/thread updates are applied when switching tabs, but don't dirty the other tab
                    m_otherTabDirty = false;
                }
            }

            // Force repaint for the progress bar
            if (IsAnalysisRunning())
            {
                int progress = m_profileAnalyser.GetProgress();
                int ignorePhases = 2;   // First 2 phases are negligable time
                if (m_threadPhases > ignorePhases)
                {
                    if (m_threadPhase < ignorePhases)
                        progress = 0;
                    else
                        progress = ((100 * (m_threadPhase-ignorePhases)) + progress) / (m_threadPhases-ignorePhases);
                }
                   
                
                if (m_threadProgress != progress)
                {
                    m_threadProgress = progress;
                    Repaint();
                }
            }

            switch (m_threadActivity)
            {
                case ThreadActivity.AnalyseDone:
                    // Create table when analysis complete
                    if (m_analysis != null)
                    {
                        CreateProfileTable();
                        Repaint();
                    }
                    m_threadActivity = ThreadActivity.None;
                    break;

                case ThreadActivity.CompareDone:
                    if (m_leftAnalysis != null && m_rightAnalysis != null)
                    {
                        CreateComparisonTable();
                        Repaint();
                    }
                    m_threadActivity = ThreadActivity.None;
                    break;
            }
        }

        private void UpdateLeftOrRightProfileIfEmpty()
        {
            if (string.IsNullOrEmpty(m_leftPath))
            {
                m_leftData = m_profileData;
                m_leftPath = m_profilePath;
                Compare();
            }
            else if (string.IsNullOrEmpty(m_rightPath))
            {
                m_rightData = m_profileData;
                m_rightPath = m_profilePath;
                Compare();
            }
        }

        private void Load()
        {
            string path = EditorUtility.OpenFilePanel("Load profile analyser data file", "", "pdata");
            if (path.Length != 0)
            {
                ProfileData newData;
                if (ProfileData.Load(path, out newData))
                {
                    m_profileData = newData;
                    m_profilePath = path;
                    m_firstFrameindex = m_profileData.OffsetToDisplayFrame(0);
                    m_lastFrameindex = m_profileData.OffsetToDisplayFrame(m_profileData.GetFrameCount() - 1);
                    Analyse();
                    //UpdateLeftOrRightProfileIfEmpty();
                }
            }
        }

        private void Save()
        {
            string path = EditorUtility.SaveFilePanel("Save profile analyser data file", "", "capture.pdata", "pdata");
            if (path.Length != 0)
            {
                m_profilePath = path;
                ProfileData.Save(path, m_profileData);

                // Update left/right data is we are effectively overwriting it.
                bool updateComparison = false;
                if (m_leftPath == m_profilePath)
                {
                    m_leftData = m_profileData;
                    m_leftAnalysis = m_analysis;
                    updateComparison = true;
                }
                if (m_rightPath == m_profilePath)
                {
                    m_rightData = m_profileData;
                    m_rightAnalysis = m_analysis;
                    updateComparison = true;
                }

                if (updateComparison)
                    Compare();
            }
        }

        void GeneratePairings()
        {
            if (m_leftAnalysis == null)
                return;
            if (m_rightAnalysis == null)
                return;
            List<MarkerData> leftMarkers = m_leftAnalysis.GetMarkers();
            if (leftMarkers == null)
                return;
            List<MarkerData> rightMarkers = m_rightAnalysis.GetMarkers();
            if (rightMarkers == null)
                return;

            Dictionary<string, MarkerPairing> markerPairs = new Dictionary<string, MarkerPairing>();
            for (int index = 0; index < leftMarkers.Count; index++)
            {
                MarkerData marker = leftMarkers[index];

                MarkerPairing pair = new MarkerPairing
                {
                    name = marker.name,
                    leftIndex = index,
                    rightIndex = -1
                };
                markerPairs[marker.name] = pair;
            }
            for (int index = 0; index < rightMarkers.Count; index++)
            {
                MarkerData marker = rightMarkers[index];

                if (markerPairs.ContainsKey(marker.name))
                {
                    MarkerPairing pair = markerPairs[marker.name];
                    pair.rightIndex = index;
                    markerPairs[marker.name] = pair;
                }
                else
                {
                    MarkerPairing pair = new MarkerPairing
                    {
                        name = marker.name,
                        leftIndex = -1,
                        rightIndex = index
                    };
                    markerPairs[marker.name] = pair;
                }
            }

            m_pairings = new List<MarkerPairing>();
            foreach (MarkerPairing pair in markerPairs.Values)
                m_pairings.Add(pair);       
        }

        private void BeginAsyncAction(ThreadActivity activity)
        {
            if (IsAnalysisRunning())
                return;

            m_threadActivity = activity;
            m_threadProgress = 0;
            m_threadPhase = 0;
            if (activity == ThreadActivity.Compare)
            {
                m_threadPhases = 4;
            }
            else
            {
                m_threadPhases = 1;
            }

            m_backgroundThread = new Thread(BackgroundThread);
            m_backgroundThread.Start();
        }

        private void CreateComparisonTable()
        {
            GetThreadNames(m_leftData, m_rightData, out m_threadUINames, out m_threadFilters);

            if (m_comparisonTreeViewState == null)
                m_comparisonTreeViewState = new TreeViewState();

            //if (m_comparisonMulticolumnHeaderState==null)
            m_comparisonMulticolumnHeaderState = ComparisonTable.CreateDefaultMultiColumnHeaderState(700);

            var multiColumnHeader = new MultiColumnHeader(m_comparisonMulticolumnHeaderState);
            multiColumnHeader.SetSorting((int)ComparisonTable.MyColumns.AbsDiff, false);
            multiColumnHeader.ResizeToFit();
            m_comparisonTable = new ComparisonTable(m_comparisonTreeViewState, multiColumnHeader, m_leftAnalysis, m_rightAnalysis, m_pairings, this);

            if (string.IsNullOrEmpty(m_selectedMarkerName))
                SelectPairing(0);
            else
                SelectPairingByName(m_selectedMarkerName);
        }

        private bool CompareSync()
        {
            if (m_leftData == null)
                return false;
            if (m_rightData == null)
                return false;
            
            // First scan just the frames
            int startLeft = m_leftData.OffsetToDisplayFrame(0);
            int endLeft = m_leftData.OffsetToDisplayFrame(m_leftData.GetFrameCount()-1);
            m_threadPhase = 0;
            m_leftAnalysis = m_profileAnalyser.Analyse(m_leftData, startLeft, endLeft, "None", m_depthFilter, m_nameFilter, m_nameExclude);

            int startRight = m_rightData.OffsetToDisplayFrame(0);
            int endRight = m_rightData.OffsetToDisplayFrame(m_rightData.GetFrameCount()-1);
            m_threadPhase = 1;
            m_rightAnalysis = m_profileAnalyser.Analyse(m_rightData, startRight, endRight, "None", m_depthFilter, m_nameFilter, m_nameExclude);

            if (m_leftAnalysis == null)
                return false;
            if (m_rightAnalysis == null)
                return false;

            // Calculate the max frame time of the two scans 
            float timeScaleMax = Math.Max(m_leftAnalysis.GetFrameSummary().msMax, m_rightAnalysis.GetFrameSummary().msMax);

            // Now process the markers and setup buckets using the overall max frame time
            m_threadPhase = 2;
            m_leftAnalysis = m_profileAnalyser.Analyse(m_leftData, startLeft, endLeft, m_threadFilter, m_depthFilter, m_nameFilter, m_nameExclude, timeScaleMax);

            m_threadPhase = 3;
            m_rightAnalysis = m_profileAnalyser.Analyse(m_rightData, startRight, endRight, m_threadFilter, m_depthFilter, m_nameFilter, m_nameExclude, timeScaleMax);

            GeneratePairings();

            var leftMarkers = m_leftAnalysis.GetMarkers();
            var rightMarkers = m_rightAnalysis.GetMarkers();
            foreach (var pairing in m_pairings)
            {
                float min = float.MaxValue;
                float max = 0.0f;
                MarkerData leftMarker = null;
                MarkerData rightMarker = null;
                if (pairing.leftIndex >= 0)
                {
                    leftMarker = leftMarkers[pairing.leftIndex];
                    max = Math.Max(max, leftMarker.msMax);
                    min = Math.Min(min, leftMarker.msMin);
                }
                if (pairing.rightIndex >= 0)
                {
                    rightMarker = rightMarkers[pairing.rightIndex];
                    max = Math.Max(max, rightMarker.msMax);
                    min = Math.Min(min, rightMarker.msMin);
                }

                if (leftMarker!=null)
                {
                    leftMarker.ComputeBuckets(min, max);
                }
                if (rightMarker!=null)
                {
                    rightMarker.ComputeBuckets(min, max);
                }
            }

            return true;
        }

        private void Compare()
        {
            if (m_async)
            {
                //m_comparisonTable = null;
                //m_leftAnalysis = null;
                //m_rightAnalysis = null;
                BeginAsyncAction(ThreadActivity.Compare);
            }
            else
            {
                CompareSync();
            }
        }

        private List<MarkerPairing> GetPairings()
        {
            return m_pairings;
        }

        private void GetFrameRangeFromProfiler()
        {
            m_profilerWindowInterface.GetFrameRangeFromProfiler(out m_grabFirstFrameindex, out m_grabLastFrameindex);
            m_profileAnalyser.QuickScan();
        }

        private int GetUnsavedIndex(string path)
        {
            if (path == null)
                return 0;
            
            Regex unsavedRegExp = new Regex(@"^Unsaved[\s*]([\d]*)", RegexOptions.IgnoreCase);
            Match match = unsavedRegExp.Match(path);
            if (match.Length <= 0)
                return 0;

            return Int32.Parse(match.Groups[1].Value);
        }

        private void GrabFromProfiler(int firstFrame, int lastFrame)
        {
            m_progressBar.InitProgressBar("Grabbing Frames", "Please wait...", lastFrame -firstFrame);

            m_profileData = m_profileAnalyser.GrabFromProfiler(firstFrame, lastFrame);
            m_firstFrameindex = m_profileData.OffsetToDisplayFrame(0);
            m_lastFrameindex = m_profileData.OffsetToDisplayFrame(m_profileData.GetFrameCount() - 1);

            int lastIndex = 0;
            lastIndex = Math.Max(lastIndex, GetUnsavedIndex(m_leftPath));
            lastIndex = Math.Max(lastIndex, GetUnsavedIndex(m_rightPath));
            m_profilePath = string.Format("Unsaved {0}",lastIndex+1);
            SelectTab(ActiveTab.Summary);

            m_progressBar.ClearProgressBar();

            //UpdateLeftOrRightProfileIfEmpty();
        }
            
        private void BackgroundThread()
        {
            switch (m_threadActivity)
            {
                case ThreadActivity.Analyse:
                    AnalyseSync();
                    m_threadActivity = ThreadActivity.AnalyseDone;
                    break;

                case ThreadActivity.Compare:
                    CompareSync();
                    m_threadActivity = ThreadActivity.CompareDone;
                    break;

                default:
                    m_threadActivity = ThreadActivity.None;
                    break;
            }
        }

        private void CreateProfileTable()
        {
            if (m_profileTreeViewState == null)
                m_profileTreeViewState = new TreeViewState();

            //if (m_profileMulticolumnHeaderState==null)
            m_profileMulticolumnHeaderState = ProfileTable.CreateDefaultMultiColumnHeaderState(700);

            var multiColumnHeader = new MultiColumnHeader(m_profileMulticolumnHeaderState);
            multiColumnHeader.SetSorting((int)ProfileTable.MyColumns.Median, false);
            multiColumnHeader.ResizeToFit();
            m_profileTable = new ProfileTable(m_profileTreeViewState, multiColumnHeader, m_analysis, this);

            if (string.IsNullOrEmpty(m_selectedMarkerName))
                SelectMarker(0);
            else
                SelectMarkerByName(m_selectedMarkerName);

            GetThreadNames(m_profileData, out m_threadUINames, out m_threadFilters);
        }

        private void AnalyseSync()
        {
            if (m_profileData==null)
                return;
            
            m_analysis = m_profileAnalyser.Analyse(m_profileData, m_firstFrameindex, m_lastFrameindex, m_threadFilter, m_depthFilter, m_nameFilter, m_nameExclude);
        }

        private void Analyse()
        {
            if (m_async)
            {
                //m_profileTable = null;
                //m_analysis = null;
                BeginAsyncAction(ThreadActivity.Analyse);
            }
            else
            {
                AnalyseSync();
            }
        }

        private void GetThreadNames(ProfileData profleData, out List<string> threadUINames, out List<string> threadFilters)
        {
            GetThreadNames(profleData, null, out threadUINames, out threadFilters);
        }

        private string GetFriendlyThreadName(string threadNameWithIndex, bool single)
        {
            var info = threadNameWithIndex.Split(':');
            int threadGroupIndex = int.Parse(info[0]);
            var threadName = info[1];

            if (single) // Single instance of this thread name
                return threadName;
            else
                return string.Format("{0} : {1}", threadName, threadGroupIndex);
        }

        private void GetThreadNames(ProfileData leftData, ProfileData rightData, out List<string> threadUINames, out List<string> threadFilters)
        {
            threadUINames = new List<string>();
            threadFilters = new List<string>();

            List<string> threadNames = leftData.GetThreadNames();
            if (rightData != null)
            {
                foreach (var threadNameWithIndex in rightData.GetThreadNames())
                {
                    if (!threadNames.Contains(threadNameWithIndex))
                    {
                        // TODO: Insert after last thread with same name (or at end)
                        threadNames.Add(threadNameWithIndex);
                    }
                }
            }

            threadUINames.Add("All");
            threadFilters.Add("All");
            for (int index = 0; index < threadNames.Count; index++)
            {
                var threadNameWithIndex = threadNames[index];
                var info = threadNameWithIndex.Split(':');
                int threadGroupIndex = int.Parse(info[0]);
                var threadName = info[1];

                if (threadGroupIndex == 1)
                {
                    if (threadNames.Contains(string.Format("2:{0}",threadName)))
                    {
                        // First thread name of a group with the same name
                        // Add an 'all' selection
                        threadUINames.Add(string.Format("{0} : All", threadName));
                        threadFilters.Add("All:" + threadName);
                        // And add the first item too
                        threadUINames.Add(GetFriendlyThreadName(threadNameWithIndex, false));
                        threadFilters.Add(threadNameWithIndex);
                    }
                    else
                    {
                        // Single instance of this thread name
                        threadUINames.Add(GetFriendlyThreadName(threadNameWithIndex, true));
                        threadFilters.Add(threadNameWithIndex);
                    }
                }
                else
                {
                    threadUINames.Add(GetFriendlyThreadName(threadNameWithIndex, false));
                    threadFilters.Add(threadNameWithIndex);
                }
            }
        }

        private int ClampToRange(int value, int min, int max)
        {
            if (value < min)
                value = min;
            if (value > max)
                value = max;

            return value;
        }

        private void DrawProfilerWindowControls()
        {
            int minFrameindex = 1;
            int maxFrameindex = 1;
            //if (ProfilerDriver.enabled)
            {
                minFrameindex = 1 + ProfilerDriver.firstFrameIndex;
                maxFrameindex = 1 + ProfilerDriver.lastFrameIndex;   
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Profiler Window : ", GUILayout.Width(100));
            if (!m_profilerWindowInterface.IsProfilerWindowOpen())
            {
                if (GUILayout.Button("Open", GUILayout.ExpandWidth(false), GUILayout.Width(50)))
                    m_profilerWindowInterface.OpenProfilerOrUseExisting();
            }
            else
            {
                if (GUILayout.Button("Close", GUILayout.ExpandWidth(false), GUILayout.Width(50)))
                    m_profilerWindowInterface.CloseProfiler();

                if (GUILayout.Button("Grab frames", GUILayout.ExpandWidth(false)))
                {
                    GrabFromProfiler(m_grabFirstFrameindex, m_grabLastFrameindex);
                    Analyse();
                }
                EditorGUILayout.LabelField("Range : ", GUILayout.Width(50));
                string grabFirstFrameString = EditorGUILayout.DelayedTextField(m_grabFirstFrameindex.ToString(), GUILayout.Width(30));
                EditorGUILayout.LabelField(" : ", GUILayout.Width(30));
                var grabLastFrameString = EditorGUILayout.DelayedTextField(m_grabLastFrameindex.ToString(), GUILayout.Width(30));
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false), GUILayout.Width(50)))
                {
                    m_grabFirstFrameindex = minFrameindex;
                    m_grabLastFrameindex = maxFrameindex;
                }
                else
                {
                    Int32.TryParse(grabFirstFrameString, out m_grabFirstFrameindex);
                    m_grabFirstFrameindex = ClampToRange(m_grabFirstFrameindex, minFrameindex, maxFrameindex);
                    Int32.TryParse(grabLastFrameString, out m_grabLastFrameindex);
                    m_grabLastFrameindex = ClampToRange(m_grabLastFrameindex, minFrameindex, maxFrameindex);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLoadSave()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load", GUILayout.ExpandWidth(false), GUILayout.Width(50)))
                Load();
            if (GUILayout.Button("Save", GUILayout.ExpandWidth(false), GUILayout.Width(50)))
                Save();
            if (m_profilePath!=null)
                EditorGUILayout.LabelField(m_profilePath);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDepthFilter()
        {
            int maxDepth = 1;
            if (IsAnalysisValid())
            {
                if (m_activeTab == ActiveTab.Summary)
                {
                    if (m_analysis != null)
                        maxDepth = m_analysis.GetFrameSummary().maxMarkerDepth;
                }
                else
                {
                    if (m_leftAnalysis != null)
                        maxDepth = m_leftAnalysis.GetFrameSummary().maxMarkerDepth;
                    if (m_rightAnalysis != null)
                        maxDepth = Math.Max(m_rightAnalysis.GetFrameSummary().maxMarkerDepth, maxDepth);
                }
            }

            if (m_depthStrings == null || (!IsAnalysisRunning() && (maxDepth != (1 + m_depthStrings.Length))))
            {
                List<string> depthStrings = new List<string>();
                List<int> depthValues = new List<int>();
                depthStrings.Add("All");
                depthValues.Add(-1);
                // Depth 0 is not used
                for (int depth = 1; depth <= maxDepth; depth++)
                {
                    depthStrings.Add(depth.ToString());
                    depthValues.Add(depth);
                }
                m_depthStrings = depthStrings.ToArray();
                m_depthValues = depthValues.ToArray();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Depth Filter : ", "Specific marker callstack depth to analyse"), GUILayout.Width(100));
            bool triggerRefresh = false;
            int lastDepthFilter = m_depthFilter;
            if (m_depthFilter >= m_depthStrings.Length)
                m_depthFilter = -1;
            m_depthFilter = EditorGUILayout.IntPopup(m_depthFilter, m_depthStrings, m_depthValues, GUILayout.Width(30));
            if (m_depthFilter != lastDepthFilter)
                triggerRefresh = true;
            EditorGUILayout.EndHorizontal();

            if (triggerRefresh)
                UpdateActiveTab();
        }

        private void DrawThreadFilter(ProfileData profileData)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(300));
            EditorGUILayout.LabelField("Thread Filter : ", GUILayout.Width(100));
            if (profileData != null)
            {
                if (m_threadFilters.Count > 0)
                {
                    int threadSelected = m_threadFilters.FindIndex(s => s == m_threadFilter);
                    if (threadSelected < 0)
                        threadSelected = 0; // All
                    if (threadSelected >= m_threadFilters.Count)
                        threadSelected = m_threadFilters.Count - 1;
                    if (threadSelected >= m_threadUINames.Count)
                        threadSelected = m_threadUINames.Count - 1;
                    
                    int newThreadSelected = EditorGUILayout.Popup(threadSelected, m_threadUINames.ToArray(), GUILayout.MaxWidth(200));
                    if (newThreadSelected != threadSelected)
                    {
                        threadSelected = newThreadSelected;
                        m_threadFilter = m_threadFilters[threadSelected];
                        UpdateActiveTab();
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void UpdateActiveTab(bool markOtherDirty = true)
        {
            switch (m_activeTab)
            {
                case ActiveTab.Summary:
                    Analyse();
                    break;
                case ActiveTab.Compare:
                    Compare();
                    break;
            }

            if (markOtherDirty)
                m_otherTabDirty = true;
        }

        private void DrawNameFilter()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(600));
            EditorGUILayout.LabelField(m_guiNameFilter, GUILayout.Width(100));
            string lastFilter = m_nameFilter;
            m_nameFilter = EditorGUILayout.DelayedTextField(m_nameFilter, GUILayout.MaxWidth(200));
            if (m_nameFilter != lastFilter)
            {
                UpdateActiveTab();
            }

            EditorGUILayout.LabelField(m_guiNameExclude, GUILayout.Width(100));
            string lastExclude = m_nameExclude;
            m_nameExclude = EditorGUILayout.DelayedTextField(m_nameExclude, GUILayout.MaxWidth(200));
            if (m_nameExclude != lastExclude)
            {
                UpdateActiveTab();
            }
            EditorGUILayout.EndHorizontal();
        }

        public void SetMode(Mode newMode)
        {
            m_modeFilter = newMode;
            switch (m_activeTab)
            {
                case ActiveTab.Summary:
                    m_profileTable.SetMode(m_modeFilter);
                    break;
                case ActiveTab.Compare:
                    m_comparisonTable.SetMode(m_modeFilter);
                    break;
            }
        }

        private void DrawModeFilter()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(m_guiModeFilter, GUILayout.Width(100));
            Mode modeFilter = (Mode)EditorGUILayout.IntPopup((int)m_modeFilter, m_modeStrings, m_modeValues, GUILayout.Width(100));
            if (modeFilter != m_modeFilter)
            {
                SetMode(modeFilter);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMarkerCount()
        {
            if (!IsAnalysisValid())
                return;

            if (m_activeTab == ActiveTab.Summary)
            {
                int filteredCount = 0;
                if (m_profileTable != null)
                {
                    IList<TreeViewItem> rows = m_profileTable.GetRows();
                    filteredCount = rows.Count;
                }
                EditorGUILayout.LabelField(String.Format("{0} of {1} markers", filteredCount, m_analysis.GetFrameSummary().totalMarkers), GUILayout.MaxWidth(150));
            }
            if (m_activeTab == ActiveTab.Compare)
            {
                int filteredCount = 0;
                if (m_comparisonTable != null)
                {
                    IList<TreeViewItem> rows = m_comparisonTable.GetRows();
                    filteredCount = rows.Count;
                }
                int max = Math.Max(m_leftAnalysis.GetFrameSummary().totalMarkers, m_rightAnalysis.GetFrameSummary().totalMarkers);
                EditorGUILayout.LabelField(String.Format("{0} of {1} markers", filteredCount, max), GUILayout.MaxWidth(150));
            }
        }

        private void DrawAnalysisOptions()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            if (IsAnalysisRunning())
            {
                GUI.enabled = false;
            }

            /*
            int firstFrameIndex = m_profileData ? m_profileData.OffsetToDisplayFrame(0) : 0;
            int lastFrameIndex = m_profileData ? m_profileData.OffsetToDisplayFrame(m_profileData.GetFrameCount() - 1) : 0;
            int frameCount = m_profileData ? m_profileData.GetFrameCount() : 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Start Frame : ", GUILayout.MaxWidth(100));
            string firstFrameString = EditorGUILayout.DelayedTextField(m_firstFrameindex.ToString(), GUILayout.Width(30));
            EditorGUILayout.LabelField("", GUILayout.Width(50));
            if (GUILayout.Button("Reset start", GUILayout.MaxWidth(100)))
            {
                m_firstFrameindex = firstFrameIndex;
            }
            else
            {
                Int32.TryParse(firstFrameString, out m_firstFrameindex);
                m_firstFrameindex = ClampToRange(m_firstFrameindex, firstFrameIndex, lastFrameIndex);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("End Frame : ", GUILayout.MaxWidth(100));
            var lastFrameString = EditorGUILayout.DelayedTextField(m_lastFrameindex.ToString(), GUILayout.Width(30));
            EditorGUILayout.LabelField(string.Format(" of {0}", frameCount), GUILayout.Width(50));
            if (GUILayout.Button("Reset End", GUILayout.MaxWidth(100)))
                m_lastFrameindex = lastFrameIndex;
            else
            {
                Int32.TryParse(lastFrameString, out m_lastFrameindex);
                m_lastFrameindex = ClampToRange(m_lastFrameindex, firstFrameIndex, lastFrameIndex);
            }
            EditorGUILayout.EndHorizontal();
            */

            DrawDepthFilter();
            DrawThreadFilter(m_profileData);
            DrawNameFilter();
            //DrawModeFilter();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Analyse", GUILayout.Width(100)))
                Analyse();
            DrawMarkerCount();
            DrawProgress();
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;
            EditorGUILayout.EndVertical();
        }

        float GetTopMarkerTimeRange(ProfileAnalysis analysis, Color[] colors)
        {
            if (analysis == null)
                return 0.0f;
            
            var markers = analysis.GetMarkers();

            int max = colors.Length;
            int at = 0;
            float range = 0;
            foreach (var marker in markers)
            {
                if (m_depthFilter>=0 && marker.minDepth != m_depthFilter)
                {
                    continue;
                }

                range += marker.msAtMedian;

                at++;
                if (at >= max)
                    break;
            }

            return range;
        }

        void DrawTopMarkers(ProfileAnalysis analysis, float width, float height, Color[] colors, float timeRange)
        {
            if (analysis == null)
                return;
            if (width <= 0)
                return;
            
            int max = colors.Length;
            int at = 0;
            float x = 0;
            float y = 0;
            float spacing = 2;
            var frameSummary = analysis.GetFrameSummary();
            if (frameSummary==null)
                return;
            
            var markers = analysis.GetMarkers();
            if (markers==null)
                return;
            
            //var medianFrameIndex = frameSummary.medianFrameIndex;
            if (timeRange<=0.0f)
                timeRange = frameSummary.msMedian;
            var selectedPairingMarkerName = GetSelectedMarkerName();


            if (DrawStart(width, height))
            {
                DrawBar(x, y, width, height, m_colorBarBackground);

                foreach (var marker in markers)
                {
                    if (m_depthFilter >= 0 && marker.minDepth != m_depthFilter)
                    {
                        continue;
                    }

                    float w = (marker != null) ? marker.msAtMedian / timeRange * (width - spacing) : 0.0f;
                    if (x + w > width)
                        w = width - x;
                    if (marker.name == selectedPairingMarkerName)
                    {
                        DrawBar(x + 1, y + 1, w, height - 2, Color.white);
                        DrawBar(x + 2, y + 2, w - 2, height - 4, colors[at]);
                    }
                    else
                    {
                        DrawBar(x + 2, y + 2, w - 2, height - 4, colors[at]);
                    }

                    x += w;

                    at++;
                    if (at >= max)
                        break;
                }
                DrawEnd();
            }


            Rect rect = GUILayoutUtility.GetLastRect();
            at = 0;
            x = 0.0f;
            GUIStyle centreAlignStyle = new GUIStyle(GUI.skin.label);
            centreAlignStyle.alignment = TextAnchor.MiddleCenter;
            centreAlignStyle.normal.textColor = Color.white;
            //centreAlignStyle.hover.textColor = Color.red;
            GUIStyle leftAlignStyle = new GUIStyle(GUI.skin.label);
            leftAlignStyle.alignment = TextAnchor.MiddleLeft;
            leftAlignStyle.normal.textColor = Color.white;
            for (int index = 0; index < markers.Count; index++)
            {
                var marker = markers[index];
                if (m_depthFilter >= 0 && marker.minDepth != m_depthFilter)
                {
                    continue;
                }

                float w = (marker != null) ? marker.msAtMedian / timeRange * (width - spacing) : 0.0f;
                if (x + w > width)
                    w = width - x;

                Rect labelRect = new Rect(rect.x + x, rect.y, w, rect.height);
                GUIStyle style = centreAlignStyle;
                String displayName = "";
                if (w >= 20)
                {
                    displayName = marker.name;
                    Vector2 size = centreAlignStyle.CalcSize(new GUIContent(marker.name));
                    if (size.x > w)
                    {
                        var words = marker.name.Split('.');
                        displayName = words[words.Length - 1];
                        style = leftAlignStyle;
                    }
                }
                float percent = (marker != null) ? marker.msAtMedian / timeRange * 100 : 0.0f;
                string tooltip = string.Format("{0}\n{1:f2}%, {2:f2}ms", marker.name, percent, marker.msAtMedian);
                if (GUI.Button(labelRect, new GUIContent(displayName, tooltip), style))
                {
                    SelectMarker(marker.name);
                }

                x += w;

                at++;
                if (at >= max)
                    break;
            }
        }

        private bool IsAnalysisRunning()
        {
            if (m_threadActivity != ThreadActivity.None)
                return true;

            return false;
        }

        private int GetProgress()
        {
            // We return the value from the update loop so the data doesn't change over the time onGui is called for layout and repaint
            return m_threadProgress;
        }

        private bool IsAnalysisValid()
        {
            switch (m_activeTab)
            {
                case ActiveTab.Summary:
                    if (m_profileData == null)
                        return false;

                    if (m_analysis == null)
                        return false;

                    if (m_analysis.GetFrameSummary().frames.Count <= 0)
                        return false;
                    break;

                case ActiveTab.Compare:
                    if (m_leftData == null)
                        return false;
                    if (m_rightData == null)
                        return false;

                    if (m_leftAnalysis == null)
                        return false;
                    if (m_rightAnalysis == null)
                        return false;

                    if (m_leftAnalysis.GetFrameSummary().frames.Count <= 0)
                        return false;
                    if (m_rightAnalysis.GetFrameSummary().frames.Count <= 0)
                        return false;
                    break;
            }

            //if (IsAnalysisRunning())
            //    return false;

            return true;
        }

        void DrawProgress()
        {
            if (IsAnalysisRunning())
            {
                int progress = GetProgress();

                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(150));
                float x = 0;
                float y = 0;
                float width = 100;
                float height = GUI.skin.label.lineHeight;
                if (DrawStart(width, height, GUI.skin.label))
                {
                    float barLength = (width * progress) / 100;
                    DrawBar(x, y, barLength, height, m_colorWhite);
                      
                    DrawEnd();
                }
                EditorGUILayout.LabelField(string.Format("{0}%", progress), GUILayout.MaxWidth(50));
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("", GUILayout.Width(150));
            }
        }

        private void DrawFilesLoaded()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            DrawTabSelect();
            //DrawProgress();

            if (IsAnalysisRunning())
                GUI.enabled = false;
            
            DrawProfilerWindowControls();

            GUI.enabled = (!IsAnalysisRunning() && (m_activeTab == ActiveTab.Summary)) ? true : false;
            DrawLoadSave();

            GUI.enabled = (!IsAnalysisRunning() && (m_activeTab == ActiveTab.Compare)) ? true : false;
            DrawComparisonLoadSave();

            GUI.enabled = true;
            GUILayout.Space(12);

            EditorGUILayout.EndVertical();
        }

        private void DrawAnalysis()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();

            DrawFilesLoaded();

            if (m_profileData != null)
                DrawAnalysisOptions();
 
            if (IsAnalysisValid())
            {
                EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(80));
                if (m_depthFilter>=0)
                    EditorGUILayout.LabelField(string.Format("Median frame top 10 markers with depth filtered to level {0} only",m_depthFilter));
                else
                    EditorGUILayout.LabelField("Median frame top 10 markers with no depth filter");

                Color[] colorBars = GetBarColors();
                //TODO: Fix me - should this be 3 or 4 * (padding + margin) ?
                float width = (position.width - m_widthRHS) - (8 * GUI.skin.box.padding.horizontal);
                float range = GetTopMarkerTimeRange(m_analysis, colorBars);
                DrawTopMarkers(m_analysis, width, 40 + GUI.skin.box.padding.vertical, colorBars, range);
                EditorGUILayout.EndVertical();

                if (m_profileTable != null)
                {
                    //Debug.Log(last.yMin);
                    Rect r = GUILayoutUtility.GetRect((position.width - m_widthRHS) - (2 * GUI.skin.box.padding.horizontal), position.height - 287, GUI.skin.box, GUILayout.ExpandWidth(true));
                    m_profileTable.OnGUI(r);
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(m_widthRHS));
            GUILayout.Space(4);
            DrawFrameSummary();
            DrawThreadSummary();
            DrawSelected();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawComparisonLoadSave()
        {
            Color oldColor = GUI.backgroundColor;

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = m_colorLeft;
            bool loadLeft = GUILayout.Button("Load Left", GUILayout.ExpandWidth(false), GUILayout.Width(100));
            GUI.backgroundColor = oldColor;
            if (loadLeft)
            {
                string path = EditorUtility.OpenFilePanel("Load Left profile analyser data file", "", "pdata");
                if (path.Length != 0)
                {
                    ProfileData newData;
                    if (ProfileData.Load(path, out newData))
                    {
                        m_leftData = newData;
                        m_leftPath = path;
                        Compare();
                    }
                }
            }
            if (m_leftPath != null)
                EditorGUILayout.LabelField(m_leftPath);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = m_colorRight;
            bool loadRight = GUILayout.Button("Load Right", GUILayout.ExpandWidth(false), GUILayout.Width(100));
            GUI.backgroundColor = oldColor;
            if (loadRight)
            {
                string path = EditorUtility.OpenFilePanel("Load Right profile analyser data file", "", "pdata");
                if (path.Length != 0)
                {
                    ProfileData newData;
                    if (ProfileData.Load(path, out newData))
                    {
                        m_rightData = newData;
                        m_rightPath = path;
                        Compare();
                    }
                }
            }
            if (m_rightPath != null)
                EditorGUILayout.LabelField(m_rightPath);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawComparisonFrameSummary()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(m_widthRHS));

            m_showFrameSummary = BoldFoldout(m_showFrameSummary, "Frame Summary");
            if (m_showFrameSummary)
            {
                if (IsComparisonValid())
                {
                    var leftFrameSummary = m_leftAnalysis.GetFrameSummary();
                    var rightFrameSummary = m_rightAnalysis.GetFrameSummary();

                    SetColumnSizes(m_widthColumn0, m_widthColumn1, m_widthColumn2, m_widthColumn3);
                    Draw4Column("", "left", "right", "diff");
                    Draw3Column("Frames", string.Format("{0}", leftFrameSummary.count),
                        string.Format("{0}", rightFrameSummary.count));
                    DrawColumn(0, "");
                    Draw4Column("", "ms", "ms", "ms");
                    Draw4ColumnDiff("Average", leftFrameSummary.msAverage, rightFrameSummary.msAverage);

                    Draw4ColumnDiff("Min", leftFrameSummary.msMin, rightFrameSummary.msMin);
                    Draw4ColumnDiff("Lower Quartile", leftFrameSummary.msLowerQuartile, rightFrameSummary.msLowerQuartile);
                    Draw4ColumnDiff("Median", leftFrameSummary.msMedian, rightFrameSummary.msMedian);
                    Draw4ColumnDiff("Upper Quartile", leftFrameSummary.msUpperQuartile, rightFrameSummary.msUpperQuartile);
                    Draw4ColumnDiff("Max", leftFrameSummary.msMax, rightFrameSummary.msMax);

                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    GUILayout.Space(style.lineHeight);

                    EditorGUILayout.BeginHorizontal();
                    int leftBucketCount = leftFrameSummary.buckets.Length;
                    int rightBucketCount = rightFrameSummary.buckets.Length;

                    float msFrameMax = Math.Max(leftFrameSummary.msMax, rightFrameSummary.msMax);

                    if (leftBucketCount != rightBucketCount)
                    {
                        Debug.Log("Error left frame summary bucket count doesn't equal right summary");
                    }
                    else
                    {
                        float width = 200;
                        float height = 40;
                        float min = 0;
                        float max = msFrameMax;
                        float spacing = 2;

                        int bucketCount = leftBucketCount;
                        float x = (spacing / 2);
                        float y = 0;
                        float w = ((width + spacing) / bucketCount) - spacing;
                        float h = height;

                        DrawHistogramStart(width);

                        if (DrawStart(width, height))
                        {
                            float bucketWidth = ((max - min) / bucketCount);
                            Rect rect = GUILayoutUtility.GetLastRect();

                            DrawHistogramBackground(width, height, bucketCount, spacing);

                            if (!IsAnalysisRunning())
                            {
                                for (int bucketAt = 0; bucketAt < bucketCount; bucketAt++)
                                {
                                    int leftBarCount = leftFrameSummary.buckets[bucketAt];
                                    int rightBarCount = rightFrameSummary.buckets[bucketAt];
                                    float leftBarHeight = (h * leftBarCount) / leftFrameSummary.count;
                                    float rightBarHeight = (h * rightBarCount) / rightFrameSummary.count;

                                    /*
                                    DrawBar(x, y + (h - leftBarHeight), w / 2, leftBarHeight, m_colorLeft);
                                    DrawBar(x + w / 2, y + (h - rightBarHeight), w / 2, rightBarHeight, m_colorRight);
                                    */

                                    if ((int)rightBarHeight == (int)leftBarHeight)
                                    {
                                        DrawBar(x, y + (h - leftBarHeight), w, leftBarHeight, m_colorBoth);
                                    }
                                    else if (rightBarHeight > leftBarHeight)
                                    {
                                        DrawBar(x, y + (h - rightBarHeight), w, rightBarHeight, m_colorRight);
                                        DrawBar(x, y + (h - leftBarHeight), w, leftBarHeight, m_colorBoth);
                                    }
                                    else
                                    {
                                        DrawBar(x, y + (h - leftBarHeight), w, leftBarHeight, m_colorLeft);
                                        DrawBar(x, y + (h - rightBarHeight), w, rightBarHeight, m_colorBoth);
                                    }

                                    float bucketStart = min + (bucketAt * bucketWidth);
                                    float bucketEnd = bucketStart + bucketWidth;
                                    GUI.Label(new Rect(rect.x + x, rect.y + y, w, h),
                                              new GUIContent("", string.Format("{0:f2}-{1:f2}ms\nLeft: {2} frames\nRight: {3} frames", bucketStart, bucketEnd, leftBarCount, rightBarCount))
                                             );

                                    x += w;
                                    x += spacing;
                                }
                            }

                            DrawEnd();
                        }

                        DrawHistogramEnd(width, min, max, spacing);
                    }
                    DrawBoxAndWhiskerPlot(20, 40, leftFrameSummary.msMin, leftFrameSummary.msLowerQuartile,
                        leftFrameSummary.msMedian, leftFrameSummary.msUpperQuartile, leftFrameSummary.msMax, 0, msFrameMax,
                        m_colorLeftBars[m_colorLeftBars.Length - 1], m_colorLeftBars[0]);
                    DrawBoxAndWhiskerPlot(20, 40, rightFrameSummary.msMin, rightFrameSummary.msLowerQuartile,
                        rightFrameSummary.msMedian, rightFrameSummary.msUpperQuartile, rightFrameSummary.msMax, 0,
                        msFrameMax, m_colorRightBars[m_colorRightBars.Length - 1], m_colorRightBars[0]);
                    EditorGUILayout.EndHorizontal();

                }
                else
                {
                    EditorGUILayout.LabelField("No analysis data selected");
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawComparisonThreadSummary()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(m_widthRHS));

            m_showThreadSummary = BoldFoldout(m_showThreadSummary, "Thread Summary");
            if (m_showThreadSummary)
            {
                if (IsAnalysisValid())
                {
                    var frameSummary = m_leftAnalysis.GetFrameSummary();

                    SetColumnSizes(m_widthColumn0, 0, 0, 0);
                    //SetColumnSizes(m_widthColumn0, m_widthColumn1, m_widthColumn2 + m_widthColumn3, 0);
                    EditorGUILayout.BeginHorizontal();
                    DrawColumn(0, "Range");
                    m_threadRange = (ThreadRange)EditorGUILayout.Popup((int)m_threadRange, m_threadRanges, GUILayout.Width(150));
                    EditorGUILayout.EndHorizontal();

                    //Draw3Column("", "Median", "Thread");
                    Draw2Column("", "Thread");

                    var threadSummary = m_leftAnalysis.GetThreads();
                    foreach (var thread in threadSummary)
                    {
                        bool singleThread = thread.threadsInGroup > 1 ? false : true;
                        string threadName = GetFriendlyThreadName(thread.threadNameWithIndex, singleThread);

                        float xAxisMin = 0.0f;
                        float xAxisMax = frameSummary.msMax;
                        switch (m_threadRange)
                        {
                            case ThreadRange.Median:
                                xAxisMax = frameSummary.msMedian;
                                break;
                            case ThreadRange.UpperQuartile:
                                xAxisMax = frameSummary.msUpperQuartile;
                                break;
                            case ThreadRange.Max:
                                xAxisMax = frameSummary.msMax;
                                break;
                        }
                        EditorGUILayout.BeginHorizontal();

                        float width = 100;
                        float height = GUI.skin.label.lineHeight + GUI.skin.label.padding.vertical;

                        if (DrawStart(width, height, GUI.skin.label))
                        {
                            Rect rect = GUILayoutUtility.GetLastRect();

                            float x = 0;
                            float y = 0;
                            float w = width;
                            float h = height / 2;

                            Rect rectTop = new Rect(rect.x, rect.y, rect.width, h);
                            DrawBoxAndWhiskerPlotHorizontal(rectTop, x, y, w, h, thread.msMin, thread.msLowerQuartile, thread.msMedian, thread.msUpperQuartile, thread.msMax, xAxisMin, xAxisMax, m_colorLeftBars[m_colorLeftBars.Length - 1], m_colorLeftBars[0]);

                            var threadRight = m_rightAnalysis.GetThreadByName(thread.threadNameWithIndex);
                            if (threadRight != null)
                            {
                                y += h;
                                Rect rectBottom = new Rect(rect.x, rect.y + h, rect.width, h);
                                DrawBoxAndWhiskerPlotHorizontal(rectBottom, x, y, w, h, threadRight.msMin, threadRight.msLowerQuartile, threadRight.msMedian, threadRight.msUpperQuartile, threadRight.msMax, xAxisMin, xAxisMax, m_colorRightBars[m_colorRightBars.Length - 1], m_colorRightBars[0]);
                            }

                            DrawEnd();
                        }

                        //DrawColumn(1, thread.msMedian);
                        //DrawColumn(2, threadName);
                        DrawColumn(1, threadName);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No analysis data selected");
                }
            }

            EditorGUILayout.EndVertical();
        }

        private bool IsComparisonValid()
        {
            if (m_leftData == null)
                return false;
            if (m_rightData == null)
                return false;

            if (m_leftAnalysis == null)
                return false;
            if (m_rightAnalysis == null)
                return false;

            if (m_leftAnalysis.GetFrameSummary().frames.Count <= 0)
                return false;
            if (m_rightAnalysis.GetFrameSummary().frames.Count <= 0)
                return false;

            //if (IsAnalysisRunning())
            //    return false;

            return true;
        }

        private void DrawCompareOptions()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            if (IsAnalysisRunning())
            {
                GUI.enabled = false;
            }

            DrawDepthFilter();
            DrawThreadFilter(m_leftData);
            DrawNameFilter();
            //DrawModeFilter();

            EditorGUILayout.BeginHorizontal();
            if (m_leftData != null && m_rightData != null)
            {
                if (GUILayout.Button("Compare", GUILayout.Width(100)))
                    Compare();
            }
            DrawMarkerCount();
            DrawProgress();
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;
            EditorGUILayout.EndVertical();
        }

        private void DrawComparison()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            DrawFilesLoaded();
            DrawCompareOptions();         
            if (m_comparisonTable != null)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(80));
                EditorGUILayout.LabelField("Median frame top 10 markers");
                float width = (position.width - m_widthRHS) - (8 * GUI.skin.box.padding.horizontal);
                float leftRange = GetTopMarkerTimeRange(m_leftAnalysis, m_colorLeftBars);
                float rightRange = GetTopMarkerTimeRange(m_rightAnalysis, m_colorRightBars);
                DrawTopMarkers(m_leftAnalysis, width, 20, m_colorLeftBars, leftRange);
                DrawTopMarkers(m_rightAnalysis, width, 20, m_colorRightBars, rightRange);
                EditorGUILayout.EndVertical();

                Rect r = GUILayoutUtility.GetRect((position.width - m_widthRHS) - (2 * GUI.skin.box.padding.horizontal), position.height - 287, GUI.skin.box, GUILayout.ExpandWidth(true));
                m_comparisonTable.OnGUI(r);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(m_widthRHS));
            GUILayout.Space(4);
            DrawComparisonFrameSummary();
            DrawComparisonThreadSummary();
            DrawComparisonSelected();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private bool BoldFoldout(bool toggle, string text)
        {
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fontStyle = FontStyle.Bold;
            return EditorGUILayout.Foldout(toggle, text, foldoutStyle);

            /*
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel, GUILayout.Width(100));
            return true;
            */
        }

        void DrawComparisonSelected()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(m_widthRHS));

            GUIStyle style = new GUIStyle(GUI.skin.label);

            m_showMarkerSummary = BoldFoldout(m_showMarkerSummary, "Marker Summary");
            if (m_showMarkerSummary)
            {
                if (IsComparisonValid())
                {
                    List<MarkerData> leftMarkers = m_leftAnalysis.GetMarkers();
                    List<MarkerData> rightMarkers = m_rightAnalysis.GetMarkers();
                    int pairingAt = m_selectedPairing;
                    if (leftMarkers != null && rightMarkers != null && pairingAt >= 0 && pairingAt < m_pairings.Count)
                    {
                        var pairing = m_pairings[pairingAt];

                        var leftMarker = (pairing.leftIndex >= 0 && pairing.leftIndex < leftMarkers.Count) ? leftMarkers[pairing.leftIndex] : null;
                        var rightMarker = (pairing.rightIndex >= 0 && pairing.rightIndex < rightMarkers.Count) ? rightMarkers[pairing.rightIndex] : null;

                        EditorGUILayout.LabelField(pairing.name,
                            GUILayout.MaxWidth(m_widthRHS -
                                               (GUI.skin.box.padding.horizontal + GUI.skin.box.margin.horizontal)));
                        DrawComparisonFrameRatio(leftMarker, rightMarker);

                        EditorGUILayout.BeginHorizontal();
                        DrawColumn(0, "First frame");
                        if (leftMarker != null)
                            DrawFrameIndexButton(leftMarker.firstFrameIndex);
                        else
                            DrawColumn(1, "");
                        if (rightMarker != null)
                            DrawFrameIndexButton(rightMarker.firstFrameIndex);
                        else
                            DrawColumn(2, "");
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(style.lineHeight);

                        SetColumnSizes(m_widthColumn0, m_widthColumn1, m_widthColumn2, m_widthColumn3);
                        Draw4Column("", "ms", "ms", "ms");
                        Draw4ColumnDiff("Lower Quartile", leftMarker != null ? leftMarker.msLowerQuartile : 0,
                            rightMarker != null ? rightMarker.msLowerQuartile : 0);
                        Draw4ColumnDiff("Median", leftMarker != null ? leftMarker.msMedian : 0,
                            rightMarker != null ? rightMarker.msMedian : 0);
                        Draw4ColumnDiff("Upper Quartile", leftMarker != null ? leftMarker.msUpperQuartile : 0,
                            rightMarker != null ? rightMarker.msUpperQuartile : 0);
                        Draw4ColumnDiff("Individual Min", leftMarker != null ? leftMarker.msMinIndividual : 0,
                            rightMarker != null ? rightMarker.msMinIndividual : 0);
                        Draw4ColumnDiff("Individual Max", leftMarker != null ? leftMarker.msMaxIndividual : 0,
                            rightMarker != null ? rightMarker.msMaxIndividual : 0);

                        GUILayout.Space(style.lineHeight);

                        DrawTopComparison(10, leftMarker, rightMarker);

                        GUILayout.Space(style.lineHeight);

                        EditorGUILayout.BeginHorizontal();

                        int leftBucketCount = leftMarker != null ? leftMarker.buckets.Length : 0;
                        int rightBucketCount = rightMarker != null ? rightMarker.buckets.Length : 0;

                        float leftMin = leftMarker != null ? leftMarker.msMin : 0;
                        float rightMin = rightMarker != null ? rightMarker.msMin : 0;
                        float msMin = Math.Min(leftMin, rightMin);

                        float leftMax = leftMarker != null ? leftMarker.msMax : 0;
                        float rightMax = rightMarker != null ? rightMarker.msMax : 0;
                        float msMax = Math.Max(leftMax, rightMax);

                        if (leftBucketCount > 0 && rightBucketCount > 0 && leftBucketCount != rightBucketCount)
                        {
                            Debug.Log("Error - number of buckets doesn't match in the left and right marker analysis");
                        }
                        else
                        {
                            float width = 200;
                            float height = 100;
                            float min = msMin;
                            float max = msMax;
                            float spacing = 2;

                            int bucketCount = Math.Max(leftBucketCount, rightBucketCount);
                            //long leftTotalCount = leftMarker != null ? leftMarker.count : 0;
                            //long rightTotalCount = rightMarker != null ? rightMarker.count : 0;
                            int leftFrameCount = leftMarker != null ? leftMarker.presentOnFrameCount : 0;
                            int rightFrameCount = rightMarker != null ? rightMarker.presentOnFrameCount : 0;
                            float x = (spacing / 2);
                            float y = 0;
                            float w = ((width + spacing) / bucketCount) - spacing;
                            float h = height;

                            DrawHistogramStart(width);

                            if (DrawStart(width, height))
                            {
                                float bucketWidth = ((max - min) / bucketCount);
                                Rect rect = GUILayoutUtility.GetLastRect();

                                DrawHistogramBackground(width, height, bucketCount, spacing);

                                for (int bucketAt = 0; bucketAt < bucketCount; bucketAt++)
                                {
                                    float leftBarCount = leftMarker != null ? leftMarker.buckets[bucketAt] : 0;
                                    float rightBarCount = rightMarker != null ? rightMarker.buckets[bucketAt] : 0;
                                    float leftBarHeight = leftMarker != null ? ((h * leftBarCount) / leftFrameCount) : 0;
                                    float rightBarHeight = rightMarker != null ? ((h * rightBarCount) / rightFrameCount) : 0;

                                    /*
                                    DrawBar(x, y + (h - leftBarHeight), w/2, leftBarHeight, m_colorLeft);
                                    DrawBar(x + w/2, y + (h - rightBarHeight), w/2, rightBarHeight, m_colorRight);
                                    */

                                    if ((int)rightBarHeight == (int)leftBarHeight)
                                    {
                                        DrawBar(x, y + (h - leftBarHeight), w, leftBarHeight, m_colorBoth);
                                    }
                                    else if (rightBarHeight > leftBarHeight)
                                    {
                                        DrawBar(x, y + (h - rightBarHeight), w, rightBarHeight, m_colorRight);
                                        DrawBar(x, y + (h - leftBarHeight), w, leftBarHeight, m_colorBoth);
                                    }
                                    else
                                    {
                                        DrawBar(x, y + (h - leftBarHeight), w, leftBarHeight, m_colorLeft);
                                        DrawBar(x, y + (h - rightBarHeight), w, rightBarHeight, m_colorBoth);
                                    }

                                    float bucketStart = min + (bucketAt * bucketWidth);
                                    float bucketEnd = bucketStart + bucketWidth;
                                    GUI.Label(new Rect(rect.x + x, rect.y + y, w, h),
                                              new GUIContent("", string.Format("{0:f2}-{1:f2}ms\nLeft: {2} frames\nRight: {3} frames", bucketStart, bucketEnd, leftBarCount, rightBarCount))
                                             );

                                    x += w;
                                    x += spacing;
                                }

                                DrawEnd();
                            }

                            DrawHistogramEnd(width, msMin, msMax, spacing);
                        }

                        DrawBoxAndWhiskerPlotForMarker(20, 100, m_leftAnalysis, leftMarker, msMin, msMax,
                            m_colorLeftBars[m_colorLeftBars.Length - 1], m_colorLeftBars[0]);
                        DrawBoxAndWhiskerPlotForMarker(20, 100, m_rightAnalysis, rightMarker, msMin, msMax,
                            m_colorRightBars[m_colorRightBars.Length - 1], m_colorRightBars[0]);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No marker data selected");
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void SelectTab(ActiveTab newTab)
        {
            m_nextActiveTab = newTab;
        }

        private void DrawTabSelect()
        {
            ActiveTab newTab = (ActiveTab)GUILayout.Toolbar((int)m_activeTab, new string[] { "Single", "Compare" }, GUILayout.ExpandWidth(false));
            if (newTab != m_activeTab)
            {
                SelectTab(newTab);
            }
        }

        private void Draw()
        {
            EditorGUILayout.BeginVertical();

            switch (m_activeTab)
            {
                case ActiveTab.Summary:
                    DrawAnalysis();
                    break;
                case ActiveTab.Compare:
                    DrawComparison();
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        private int FindSelectionByName(List<MarkerData> markers, string name)
        {
            int index = 0;
            foreach (var marker in markers)
            {
                if (marker.name == name)
                    return index;
                index++;
            }
            return -1; // not found
        }

        public int GetDepthFilter()
        {
            return m_depthFilter;
        }

        public bool CheckMarkerValid(MarkerData marker)
        {
            if (m_depthFilter >= 0 && marker.minDepth != m_depthFilter)
                return false;

            return true;
        }

        public void SelectMarker(string name)
        {
            switch (m_activeTab)
            {
                case ActiveTab.Summary:
                    SelectMarkerByName(name);
                    break;
                case ActiveTab.Compare:
                    SelectPairingByName(name);
                    break;
            }
        }

        private void UpdateSelectedMarkerName(string markerName)
        {
            m_selectedMarkerName = markerName;
            m_profilerWindowInterface.SetProfilerWindowMarkerName(markerName, m_threadFilter);
        }

        public void SelectMarker(int index)
        {
            m_selectedMarker = index;

            if (m_profileTable != null)
            {
                List<int> selection = new List<int>();
                if (index >= 0)
                    selection.Add(index);
                m_profileTable.SetSelection(selection);
            }

            var markerName = GetMarkerName(index);
            if (markerName != null)
                UpdateSelectedMarkerName(markerName);
        }

        public string GetSelectedMarkerName()
        {
            switch (m_activeTab)
            {
                case ActiveTab.Summary:
                    return GetMarkerName(m_selectedMarker);
                case ActiveTab.Compare:
                    return GetPairingName(m_selectedPairing);
            }

            return null;
        }

        private string GetMarkerName(int index)
        {
            if (m_analysis == null)
                return null;

            var marker = m_analysis.GetMarker(index);
            if (marker==null)
                return null;

            return marker.name;
        }

        private void SelectMarkerByName(string markerName)
        {
            int index = (m_analysis != null) ? m_analysis.GetMarkerIndexByName(markerName) : -1;

            SelectMarker(index);
        }

        public void SelectPairing(int index)
        {
            m_selectedPairing = index;

            if (m_comparisonTable != null)
            {
                List<int> selection = new List<int>();
                if (index >= 0)
                    selection.Add(index);
                m_comparisonTable.SetSelection(selection);
            }

            var markerName = GetPairingName(index);
            if (markerName!=null)
                UpdateSelectedMarkerName(markerName);
        }

        private string GetPairingName(int index)
        {
            if (m_pairings == null)
                return null;

            if (index < 0 || index >= m_pairings.Count)
                return null;

            return m_pairings[index].name;
        }

        private void SelectPairingByName(string pairingName)
        {
            if (m_pairings != null && pairingName != null)
            {
                for (int index = 0; index < m_pairings.Count; index++)
                {
                    var pairing = m_pairings[index];
                    if (pairing.name == pairingName)
                    {
                        SelectPairing(index);
                        return;
                    }
                }
            }

            SelectPairing(-1);
        }

        private Color GetBarColor()
        {
            /*
            if (m_profilePath == m_leftPath)
                return m_colorLeft;
            if (m_profilePath == m_rightPath)
                return m_colorRight;
            */

            return m_colorBar;
        }

        private Color[] GetBarColors()
        {
            /*
            if (m_profilePath == m_leftPath)
                return m_colorLeftBars;
            if (m_profilePath == m_rightPath)
                return m_colorRightBars;
            */
            
            return m_colorBars;
        }

        private void DrawFrameSummary()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(m_widthRHS));

            m_showFrameSummary = BoldFoldout(m_showFrameSummary, "Frame Summary");
            if (m_showFrameSummary)
            {
                if (IsAnalysisValid())
                {
                    var frameSummary = m_analysis.GetFrameSummary();

                    SetColumnSizes(m_widthColumn0, m_widthColumn1, m_widthColumn2, m_widthColumn3);
                    DrawColumn(0, "");
                    Draw2Column("Frames", string.Format("{0}", frameSummary.count));
                    DrawColumn(0, "");
                    Draw3Column("", "ms", "frame");
                    Draw2Column("Average", frameSummary.msAverage);

                    Draw3ColumnLabelMsFrame("Min", frameSummary.msMin, frameSummary.minFrameIndex);
                    Draw2Column("Lower Quartile", frameSummary.msLowerQuartile);
                    Draw3ColumnLabelMsFrame("Median", frameSummary.msMedian, frameSummary.medianFrameIndex);
                    Draw2Column("Upper Quartile", frameSummary.msUpperQuartile);
                    Draw3ColumnLabelMsFrame("Max", frameSummary.msMax, frameSummary.maxFrameIndex);

                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    GUILayout.Space(style.lineHeight);

                    EditorGUILayout.BeginHorizontal();
                    DrawHistogram(200, 40, frameSummary.buckets, frameSummary.count, 0, frameSummary.msMax, GetBarColor());
                    DrawBoxAndWhiskerPlot(40 + GUI.skin.box.padding.horizontal, 40, frameSummary.msMin, frameSummary.msLowerQuartile, frameSummary.msMedian, frameSummary.msUpperQuartile, frameSummary.msMax, 0, frameSummary.msMax, m_colorStandardLine, m_colorStandardLine);
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.LabelField("No analysis data selected");
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawThreadSummary()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(m_widthRHS));

            m_showThreadSummary = BoldFoldout(m_showThreadSummary, "Thread Summary");
            if (m_showThreadSummary)
            {
                if (IsAnalysisValid())
                {
                    var frameSummary = m_analysis.GetFrameSummary();

                    SetColumnSizes(m_widthColumn0, m_widthColumn1, m_widthColumn2 + m_widthColumn3, 0);
                    EditorGUILayout.BeginHorizontal();
                    DrawColumn(0, "Range");
                    m_threadRange = (ThreadRange)EditorGUILayout.Popup((int)m_threadRange, m_threadRanges, GUILayout.Width(150));
                    EditorGUILayout.EndHorizontal();
                    
                    Draw3Column("", "Median", "Thread");

                    var threadSummary = m_analysis.GetThreads();
                    foreach (var thread in threadSummary)
                    {
                        bool singleThread = thread.threadsInGroup>1 ? false : true;
                        string threadName = GetFriendlyThreadName(thread.threadNameWithIndex, singleThread);
                        var info = thread;
                        EditorGUILayout.BeginHorizontal();
                        float xAxisMin = 0.0f;
                        float xAxisMax = frameSummary.msMax;
                        switch (m_threadRange)
                        {
                            case ThreadRange.Median:
                                xAxisMax = frameSummary.msMedian;
                                break;
                            case ThreadRange.UpperQuartile:
                                xAxisMax = frameSummary.msUpperQuartile;
                                break;
                            case ThreadRange.Max:
                                xAxisMax = frameSummary.msMax;
                                break;
                        }
                        DrawBoxAndWhiskerPlotHorizontal(100, GUI.skin.label.lineHeight, info.msMin, info.msLowerQuartile, info.msMedian, info.msUpperQuartile, info.msMax, xAxisMin, xAxisMax, GetBarColor(), m_colorBarBackground, GUI.skin.label);
                        DrawColumn(1, info.msMedian);
                        DrawColumn(2, threadName);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No analysis data selected");
                }
            }

            EditorGUILayout.EndVertical();
        }

        public bool DrawStart(Rect r)
        {
            if (Event.current.type != EventType.Repaint)
                return false;

            if (!CheckAndSetupMaterial())
                return false;

            GL.PushMatrix();
            CheckAndSetupMaterial();
            m_material.SetPass(0);

            Matrix4x4 matrix = new Matrix4x4();
            matrix.SetTRS(new Vector3(r.x, r.y, 0), Quaternion.identity, Vector3.one);
            GL.MultMatrix(matrix);
            return true;
        }

        public bool DrawStart(float w, float h, GUIStyle style = null)
        {
            Rect r = GUILayoutUtility.GetRect(w, h, style==null ? m_glStyle : style);
            return DrawStart(r);
        }

        public void DrawEnd()
        {
            GL.PopMatrix();
        }

        public void DrawBar(float x, float y, float w, float h, Color col)
        {
            GL.Begin(GL.TRIANGLE_STRIP);
            GL.Color(col);
            GL.Vertex3(x, y, 0);
            GL.Vertex3(x + w, y, 0);
            GL.Vertex3(x, y + h, 0);
            GL.Vertex3(x + w, y + h, 0);
            GL.End();
        }

        void DrawBar(float x, float y, float w, float h, float r, float g, float b)
        {
            DrawBar(x, y, w, h, new Color(r, g, b));
        }

        void DrawLine(float x, float y, float x2, float y2, Color col)
        {
            GL.Begin(GL.LINES);
            GL.Color(col);
            GL.Vertex3(x , y , 0);
            GL.Vertex3(x2, y2, 0);
            GL.End();
        }

        void DrawLine(float x, float y, float x2, float y2, float r, float g, float b)
        {
            DrawLine(x, y, x2, y2, new Color(r, g, b));
        }

        void DrawBox(float x, float y, float w, float h, Color col)
        {
            GL.Begin(GL.LINE_STRIP);
            GL.Color(col);
            GL.Vertex3(x    , y    , 0);
            GL.Vertex3(x + w, y    , 0);
            GL.Vertex3(x + w, y + h, 0);
            GL.Vertex3(x    , y + h, 0);
            GL.Vertex3(x    , y    , 0);
            GL.End();
        }

        void DrawBox(float x, float y, float w, float h, float r, float g, float b)
        {
            DrawBox(x, y, w, h, new Color(r, g, b));
        }

        private void DrawHistogramForMarker(MarkerData marker)
        {
            //var frameSummary = m_analysis.GetFrameSummary();
            //DrawHistogram(200,100,marker.buckets, marker.presentOnFrameCount, 0, frameSummary.msMax, GetBarColor());
            DrawHistogram(200, 100, marker.buckets, marker.presentOnFrameCount, marker.msMin, marker.msMax, GetBarColor());
        }

        private void DrawHistogramStart(float width)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(width + 10));

            //GUILayoutUtility.GetRect(GUI.skin.box.margin.left, 1);

            EditorGUILayout.BeginVertical();
        }

        private void DrawHistogramEnd(float width, float min, float max, float spacing)
        {
            EditorGUILayout.BeginHorizontal();
            float lastBar = width - 50;
            GUIStyle rightAlignStyle = new GUIStyle(GUI.skin.label);
            rightAlignStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField(string.Format("{0:f2}", min), GUILayout.Width(lastBar));
            EditorGUILayout.LabelField(string.Format("{0:f2}", max), rightAlignStyle, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawHistogramBackground(float width, float height, int bucketCount, float spacing)
        {
            float x = (spacing / 2);
            float y = 0;
            float w = ((width + spacing) / bucketCount) - spacing;
            float h = height;

            for (int i = 0; i < bucketCount; i++)
            {
                DrawBar(x, y, w, h, m_colorBarBackground);
                x += w;
                x += spacing;
            }
        }

        private void DrawHistogramData(float width, float height, int[] buckets, int totalFrameCount, float min, float max, Color barColor, float spacing)
        {     
            float x = (spacing / 2);
            float y = 0;
            float w = ((width + spacing) / buckets.Length) - spacing;
            float h = height;

            int bucketCount = buckets.Length;
            float bucketWidth = ((max - min) / bucketCount);
            Rect rect = GUILayoutUtility.GetLastRect();
            for (int bucketAt = 0; bucketAt < bucketCount; bucketAt++)
            {
                var count = buckets[bucketAt];

                float barHeight = (h * count) / totalFrameCount;
                DrawBar(x, y + (h - barHeight), w, barHeight, barColor);

                float bucketStart = min + (bucketAt * bucketWidth);
                float bucketEnd = bucketStart + bucketWidth;
                GUI.Label(new Rect(rect.x + x, rect.y + y, w, h), new GUIContent("",string.Format("{0:f2}-{1:f2}ms\n{2} frames",bucketStart,bucketEnd,count)) );
                
                x += w;
                x += spacing;
            }
        }

        private void DrawHistogram(float width, float height, int[] buckets, int totalFrameCount, float min, float max, Color barColor)
        {
            DrawHistogramStart(width);

            float spacing = 2;

            if (DrawStart(width, height))
            {
                DrawHistogramBackground(width, height, buckets.Length, spacing);
                DrawHistogramData(width, height, buckets, totalFrameCount, min, max, barColor, spacing);
                DrawEnd();
            }

            DrawHistogramEnd(width, min, max, spacing);
        }

        private void SetColumnSizes(int a, int b, int c, int d)
        {
            m_columnWidth[0] = a;
            m_columnWidth[1] = b;
            m_columnWidth[2] = c;
            m_columnWidth[3] = d;
        }

        private void DrawColumn(int n, string col)
        {
            if (m_columnWidth[n] > 0)
                EditorGUILayout.LabelField(col, GUILayout.Width(m_columnWidth[n]));
            else
                EditorGUILayout.LabelField(col);
        }
        private void DrawColumn(int n, float value)
        {
            DrawColumn(n, string.Format("{0:f2}", value));
        }

        private void Draw2Column(string col1, string col2)
        {
            EditorGUILayout.BeginHorizontal();
            DrawColumn(0, col1);
            DrawColumn(1, col2);
            EditorGUILayout.EndHorizontal();
        }

        private void Draw2Column(string label, float value)
        {
            EditorGUILayout.BeginHorizontal();
            DrawColumn(0, label);
            DrawColumn(1, value);
            EditorGUILayout.EndHorizontal();
        }

        private void Draw3Column(string col1, string col2, string col3)
        {
            EditorGUILayout.BeginHorizontal();
            DrawColumn(0, col1);
            DrawColumn(1, col2);
            DrawColumn(2, col3);
            EditorGUILayout.EndHorizontal();
        }

        private void Draw3Column(string col1, float value2, float value3)
        {
            EditorGUILayout.BeginHorizontal();
            DrawColumn(0, col1);
            DrawColumn(1, value2);
            DrawColumn(2, value3);
            EditorGUILayout.EndHorizontal();
        }

        private void Draw4Column(string col1, string col2, string col3, string col4)
        {
            EditorGUILayout.BeginHorizontal();
            DrawColumn(0, col1);
            DrawColumn(1, col2);
            DrawColumn(2, col3);
            DrawColumn(3, col4);
            EditorGUILayout.EndHorizontal();
        }

        private void Draw4ColumnDiff(string col1, float left, float right)
        {
            EditorGUILayout.BeginHorizontal();
            DrawColumn(0, col1);
            DrawColumn(1, left);
            DrawColumn(2, right);
            DrawColumn(3, right - left);
            EditorGUILayout.EndHorizontal();
        }

        private void Draw4Column(string col1, float value2, float value3, float value4)
        {
            EditorGUILayout.BeginHorizontal();
            DrawColumn(0, col1);
            DrawColumn(1, value2);
            DrawColumn(2, value3);
            DrawColumn(3, value4);
            EditorGUILayout.EndHorizontal();
        }

        public bool IsProfilerWindowOpen()
        {
            return m_profilerWindowInterface.IsProfilerWindowOpen();
        }

        public bool DataMatchesProfiler(ProfileData data, int frameIndex, out string message)
        {
            // Don't check full range match as we may have only captured a single frame from the data
            /*
            int dataFirstFrameIndex = data.OffsetToDisplayFrame(0);
            int dataLastFrameIndex = data.OffsetToDisplayFrame(m_profileData.GetFrameCount() - 1);
            int profilerFirstFrameIndex;
            int profilerLastFrameIndex;
            m_profilerWindowInterface.GetFrameRangeFromProfiler(out firstFrameIndex, out lastFrameIndex);

            if (dataFirstFrameIndex != profilerFirstFrameIndex ||
                dataLastFrameIndex != profilerLastFrameIndex)
            {
                message = string.Format("Data in profiler doesn't match data range({0}-{1}) != profiler range ({2}-{3}",
                                        dataFirstFrameIndex,
                                        dataLastFrameIndex,
                                        profilerFirstFrameIndex,
                                        profilerLastFrameIndex);
                return false;
            }
            */

            // Check check the frame we are jumping to.
            int dataFrameOffset = data.DisplayFrameToOffset(frameIndex);            // Convert from user facing index to zero based offset into analysis data
            float msData = data.GetFrame(dataFrameOffset).msFrame;
            float msProfiler = m_profilerWindowInterface.GetFrameTime(frameIndex-1); // Convert from user facing index to zero based index into profiler data
            if (msData != msProfiler)
            {
                message = string.Format("Timeing data in profiler doesn't match for frame {0} : {1:f2}!={2:f2}",
                                        frameIndex, msData, msProfiler);
                return false;
            }

            message = "";
            return true;
        }

        public void JumpToFrame(int frameindex)
        {
            if (m_profilerWindowInterface.JumpToFrame(frameindex))
            {
                ProfileData data = m_profileData;
                string message;
                bool dataMatch = DataMatchesProfiler(data, frameindex, out message);
                if (!dataMatch)
                {
                    Debug.Log(message);
                }
            }
        }

        private void DrawFrameIndexButton(int index)
        {
            if (index < 0)
                return;
            
            if (!m_profilerWindowInterface.IsProfilerWindowOpen())
                GUI.enabled = false;
            
            if (GUILayout.Button(string.Format("{0}", index), GUILayout.Height(14), GUILayout.Width(50)))
            {
                JumpToFrame(index);
            }

            GUI.enabled = true;
        }

        private void Draw3ColumnLabelMsFrame(string col1, float ms, int frameIndex)
        {
            EditorGUILayout.BeginHorizontal();
            DrawColumn(0, col1);
            DrawColumn(1, ms);
            DrawFrameIndexButton(frameIndex);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBoxAndWhiskerPlotForMarker(float width, float height,ProfileAnalysis analysis, MarkerData marker, float yAxisStart, float yAxisEnd, Color color, Color colorBackground)
        {
            if (marker == null)
            {
                DrawBoxAndWhiskerPlot(width, height, 0, 0, 0, 0, 0, yAxisStart, yAxisEnd, color, colorBackground);
                return;
            }
            
            DrawBoxAndWhiskerPlot(width, height, marker.msMin, marker.msLowerQuartile, marker.msMedian, marker.msUpperQuartile, marker.msMax, yAxisStart, yAxisEnd, color, colorBackground);
        }

        private void DrawBoxAndWhiskerPlotHorizontalForMarker(float width, float height, ProfileAnalysis analysis, MarkerData marker, float yAxisStart, float yAxisEnd, Color color, Color colorBackground)
        {
            DrawBoxAndWhiskerPlotHorizontal(width,height, marker.msMin, marker.msLowerQuartile, marker.msMedian, marker.msUpperQuartile, marker.msMax, yAxisStart, yAxisEnd, color, colorBackground);
        }

        private float ClampToRange(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        private void DrawBoxAndWhiskerPlot(float width, float height, float min, float lowerQuartile, float median, float upperQuartile, float max, float yAxisStart, float yAxisEnd, Color color, Color colorBackground)
        {
            if (DrawStart(width, height))
            {
                Rect rect = GUILayoutUtility.GetLastRect();

                float x = 0;
                float y = 0;
                float w = width;
                float h = height;

                DrawBoxAndWhiskerPlot(rect, x,y,w,h, min, lowerQuartile, median, upperQuartile, max, yAxisStart, yAxisEnd, color, colorBackground);
                DrawEnd();
            }
        }

        private void DrawBoxAndWhiskerPlot(Rect rect, float x, float y, float w, float h, float min, float lowerQuartile, float median, float upperQuartile, float max, float yAxisStart, float yAxisEnd, Color color, Color colorBackground)
        {
            string tooltip = string.Format(
                    "Max :\t\t{0:f2}\n\nUpper Quartile :\t{1:f2}\nMedian :\t\t{2:f2}\nLower Quartile :\t{3:f2}\n\nMin :\t\t{4:f2}",
                    max, upperQuartile, median, lowerQuartile, min);
            GUI.Label(rect, new GUIContent("", tooltip));

            DrawBar(x, y, w, h, m_colorBoxAndWhiskerBackground);

            float first = yAxisStart;
            float last = yAxisEnd;
            float range = last - first;

            bool startCap = (min >= first) ? true : false;
            bool endCap = (max <= last) ? true : false;

            // Range clamping
            min = ClampToRange(min, first, last);
            lowerQuartile = ClampToRange(lowerQuartile, first, last);
            median = ClampToRange(median, first, last);
            upperQuartile = ClampToRange(upperQuartile, first, last);
            max = ClampToRange(max, first, last);

            float yMin = h * (min - first) / range;
            float yLowerQuartile = h * (lowerQuartile - first) / range;
            float yMedian = h * (median - first) / range;
            float yUpperQuartile = h * (upperQuartile - first) / range;
            float yMax = h * (max - first) / range;

            // Min to max line
            //DrawLine(x + (w / 2), y + (h - yMin), x + (w / 2), y + (h - yMax), color);
            DrawLine(x + (w / 2), y + (h - yMin), x + (w / 2), y + (h - yLowerQuartile), color);
            DrawLine(x + (w / 2), y + (h - yUpperQuartile), x + (w / 2), y + (h - yMax), color);

            // Quartile boxes
            float xMargin = (2 * w / 8);
            if (colorBackground != color)
                DrawBar(x + xMargin, y + (h - yMedian), w - (2 * xMargin), (yMedian - yLowerQuartile), colorBackground);
            DrawBox(x + xMargin, y + (h - yMedian), w - (2 * xMargin), (yMedian - yLowerQuartile), color);
            if (colorBackground != color)
                DrawBar(x + xMargin, y + (h - yUpperQuartile), w - (2 * xMargin), (yUpperQuartile - yMedian), colorBackground);
            DrawBox(x + xMargin, y + (h - yUpperQuartile), w - (2 * xMargin), (yUpperQuartile - yMedian), color);

            // Median line
            DrawLine(x + xMargin, y + (h - yMedian), x + (w - xMargin), y + (h - yMedian), color);

            // Line caps
            xMargin = (3 * w / 8);
            if (startCap)
                DrawLine(x + xMargin, y + (h - yMin), x + (w - xMargin), y + (h - yMin), color);

            if (endCap)
                DrawLine(x + xMargin, y + (h - yMax), x + (w - xMargin), y + (h - yMax), color);
        }

        private void DrawBoxAndWhiskerPlotHorizontal(float width, float height, float min, float lowerQuartile, float median, float upperQuartile, float max, float xAxisStart, float xAxisEnd, Color color, Color colorBackground, GUIStyle style = null)
        {
            if (DrawStart(width, height, style))
            {
                Rect rect = GUILayoutUtility.GetLastRect();

                float x = 0;
                float y = 0;
                float w = width;
                float h = height;

                DrawBoxAndWhiskerPlotHorizontal(rect, x, y, w, h, min, lowerQuartile, median, upperQuartile, max, xAxisStart, xAxisEnd, color, colorBackground);
                DrawEnd();
            }
        }

        private void DrawBoxAndWhiskerPlotHorizontal(Rect rect, float x, float y, float w, float h, float min, float lowerQuartile, float median, float upperQuartile, float max, float xAxisStart, float xAxisEnd, Color color, Color colorBackground)
        {
            GUI.Label(rect, new GUIContent("", string.Format("Min: {0:f2}\nLower Quartile: {1:f2}\nMedian: {2:f2}\nUpperQuartile: {3:f2}\nMax: {4:f2}",
                                                                  min, lowerQuartile, median, upperQuartile, max)));

            DrawBar(x, y, w, h, m_colorBoxAndWhiskerBackground);

            float first = xAxisStart;
            float last = xAxisEnd;
            float range = last - first;

            bool startCap = (min >= first) ? true : false;
            bool endCap = (max <= last) ? true : false;

            // Range clamping
            min = ClampToRange(min, first, last);
            lowerQuartile = ClampToRange(lowerQuartile, first, last);
            median = ClampToRange(median, first, last);
            upperQuartile = ClampToRange(upperQuartile, first, last);
            max = ClampToRange(max, first, last);

            float xMin = w * (min - first) / range;
            float xLowerQuartile = w * (lowerQuartile - first) / range;
            float xMedian = w * (median - first) / range;
            float xUpperQuartile = w * (upperQuartile - first) / range;
            float xMax = w * (max - first) / range;

            // Min to max line
            DrawLine(x + xMin, y + (h / 2), x + xMax, y + (h / 2), color);

            // Quartile boxes
            float yMargin = (2 * y / 8);
            if (colorBackground != color)
                DrawBar(x + xLowerQuartile, y + yMargin, xMedian - xLowerQuartile, h - (2 * yMargin), colorBackground);
            DrawBox(x + xLowerQuartile, y + yMargin, xMedian - xLowerQuartile, h - (2 * yMargin), color);
            if (colorBackground != color)
                DrawBar(x + xMedian, y + yMargin, xUpperQuartile - xMedian, h - (2 * yMargin), colorBackground);
            DrawBox(x + xMedian, y + yMargin, xUpperQuartile - xMedian, h - (2 * yMargin), color);

            // Median line
            DrawLine(x + xMedian, y, x + xMedian, y + h, color);

            // Line caps
            if (startCap)
                DrawLine(x + xMin, y + (3 * h / 8), x + xMin, y + (5 * h / 8), color);

            if (endCap)
                DrawLine(x + xMax, y + (3 * h / 8), x + xMax, y + (5 * h / 8), color);
        }

        void DrawFrameRatio(MarkerData marker)
        {
            var frameSummary = m_analysis.GetFrameSummary();

            GUIStyle style = new GUIStyle(GUI.skin.label);
            float w = 100;
            float h = style.lineHeight;
            float ySpacing = 2;
            float barHeight = h - ySpacing;

            EditorGUILayout.BeginVertical(GUILayout.Width(w + 50 + 50));

            float barMax = frameSummary.msAverage;
            float msFrame = marker.msFrameAverage;
            float barLength = Math.Min((w * msFrame) / barMax, w);

            EditorGUILayout.LabelField("Average frame contribution");
            SetColumnSizes(m_widthColumn0, m_widthColumn1, m_widthColumn2, m_widthColumn3);
            Draw2Column("", "");
            EditorGUILayout.BeginHorizontal();
            if (DrawStart(w, h, style))
            {
                DrawBar(0, ySpacing, barLength, barHeight, GetBarColor());
                DrawBar(barLength, ySpacing, w - barLength, barHeight, m_colorBarBackground);
                DrawEnd();

                Rect rect = GUILayoutUtility.GetLastRect();
                GUI.Label(rect, new GUIContent("", string.Format("{0:f2}ms", msFrame)));
            }
            EditorGUILayout.LabelField(string.Format("{0:f2}%", (100 * msFrame) / barMax));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        void DrawComparisonFrameRatio(MarkerData leftMarker, MarkerData rightMarker)
        {
            var leftFrameSummary = m_leftAnalysis.GetFrameSummary();
            var rightFrameSummary = m_rightAnalysis.GetFrameSummary();

            GUIStyle style = new GUIStyle(GUI.skin.label);
            float w = 100;
            float h = style.lineHeight;
            float ySpacing = 2;
            float barHeight = (h - ySpacing) / 2;

            EditorGUILayout.BeginVertical(GUILayout.Width(w + 50 + 50));

            float msLeftFrame = leftMarker != null ? leftMarker.msFrameAverage : 0.0f;
            float msRightFrame = rightMarker != null ? rightMarker.msFrameAverage : 0.0f;

            float leftBarLength = Math.Min((w * msLeftFrame) / leftFrameSummary.msAverage, w);
            float rightBarLength = Math.Min((w * msRightFrame) / rightFrameSummary.msAverage, w);

            EditorGUILayout.LabelField("Average frame contribution");
            SetColumnSizes(m_widthColumn0, m_widthColumn1, m_widthColumn2, m_widthColumn3);
            Draw4Column("", "left", "right", "diff");
            EditorGUILayout.BeginHorizontal();
            if (DrawStart(w, h, style))
            {
                DrawBar(0, ySpacing, w, h - ySpacing, m_colorBarBackground);

                DrawBar(0, ySpacing, leftBarLength, barHeight, m_colorLeft);
                DrawBar(0, ySpacing + barHeight, rightBarLength, barHeight, m_colorRight);
                DrawEnd();

                Rect rect = GUILayoutUtility.GetLastRect();
                GUI.Label(rect, new GUIContent("", string.Format("Left: {0:f2}ms\nRight: {1:f2}ms", msLeftFrame, msRightFrame)));
            }
            float leftPercentage = (100 * msLeftFrame) / leftFrameSummary.msAverage;
            float rightPercentage = (100 * msRightFrame) / rightFrameSummary.msAverage;

            EditorGUILayout.LabelField(string.Format("{0:f2}%", leftPercentage), GUILayout.Width(50));
            EditorGUILayout.LabelField(string.Format("{0:f2}%", rightPercentage), GUILayout.Width(50));
            if (leftMarker!=null && rightMarker!=null)
                EditorGUILayout.LabelField(string.Format("{0:f2}%", rightPercentage - leftPercentage), GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        void DrawTop(int number, MarkerData marker)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            float w = 100;
            float h = style.lineHeight;
            float ySpacing = 2;
            float barHeight = h - ySpacing;

            EditorGUILayout.BeginVertical(GUILayout.Width(w + 50 + 50));
            EditorGUILayout.LabelField(new GUIContent("Top 10 by frame costs", "Contains accumulated marker cost within the frame"));
            /*
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(w));
            EditorGUILayout.LabelField("Value", GUILayout.Width(50));
            EditorGUILayout.LabelField("Frame", GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
            */

            // var frameSummary = m_analysis.GetFrameSummary();
            float barMax = marker.msMax; // frameSummary.msMax
            Color barColor = GetBarColor();

            int index = marker.frames.Count - 1;
            for (int i = 0; i < number; i++)
            {
                float msFrame = (index >= 0 ) ? marker.frames[index].ms : 0.0f;
                float barLength = Math.Min((w * msFrame) / barMax, w);

                EditorGUILayout.BeginHorizontal();
                if (DrawStart(w, h, style))
                {
                    if (i < marker.frames.Count)
                    {
                        DrawBar(0, ySpacing, barLength, barHeight, barColor);
                        DrawBar(barLength, ySpacing, w - barLength, barHeight, m_colorBarBackground);
                    }
                    DrawEnd();

                    Rect rect = GUILayoutUtility.GetLastRect();
                    GUI.Label(rect, new GUIContent("", string.Format("{0:f2}ms", msFrame)));
                }
                if (i < marker.frames.Count)
                { 
                    EditorGUILayout.LabelField(string.Format("{0:f2}", msFrame), GUILayout.Width(50));
                    DrawFrameIndexButton(marker.frames[index].frameIndex);
                }
                EditorGUILayout.EndHorizontal();

                index--;
            }

            EditorGUILayout.EndVertical();
        }

        void DrawTopComparison(int number, MarkerData leftMarker, MarkerData rightMarker)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            float w = 100;
            float h = style.lineHeight;
            float ySpacing = 2;
            float barHeight = (h - ySpacing) /2;

            EditorGUILayout.BeginVertical(GUILayout.Width(w + 50 + 50));
            EditorGUILayout.LabelField(new GUIContent("Top 10 by frame costs","Contains accumulated marker cost within the frame"));

            float leftMax = leftMarker != null ? leftMarker.msMax : 0.0f;
            float rightMax = rightMarker != null ? rightMarker.msMax : 0.0f;
            float barMax = Math.Max(leftMax, rightMax);

            int leftIndex = leftMarker!=null ? leftMarker.frames.Count - 1 : -1;
            int rightIndex = rightMarker!=null ? rightMarker.frames.Count - 1 : -1;
            for (int i = 0; i < number; i++)
            {
                float msLeftFrame = leftIndex>=0 ? leftMarker.frames[leftIndex].ms : 0.0f;
                float msRightFrame = rightIndex>=0 ? rightMarker.frames[rightIndex].ms : 0.0f;

                float leftBarLength = Math.Min((w * msLeftFrame) / barMax, w);
                float rightBarLength = Math.Min((w * msRightFrame) / barMax, w);

                EditorGUILayout.BeginHorizontal();
                if (DrawStart(w, h, style))
                {
                    if (leftIndex >= 0 || rightIndex >= 0)
                    {
                        DrawBar(0, ySpacing, w, h - ySpacing, m_colorBarBackground);

                        DrawBar(0, ySpacing, leftBarLength, barHeight, m_colorLeft);
                        DrawBar(0, ySpacing + barHeight, rightBarLength, barHeight, m_colorRight);
                    }
                    DrawEnd();

                    Rect rect = GUILayoutUtility.GetLastRect();
                    GUI.Label(rect, new GUIContent("", string.Format("Left: {0:f2}ms\nRight: {1:f2}ms", msLeftFrame, msRightFrame)));
                }

                EditorGUILayout.LabelField(leftIndex>=0 ? string.Format("{0:f2}", msLeftFrame) : "", GUILayout.Width(50));
                EditorGUILayout.LabelField(rightIndex>=0 ? string.Format("{0:f2}", msRightFrame) : "", GUILayout.Width(50));
                if (leftIndex >= 0 && rightIndex>=0)
                    EditorGUILayout.LabelField(string.Format("{0:f2}", msRightFrame - msLeftFrame), GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();

                leftIndex--;
                rightIndex--;
            }

            EditorGUILayout.EndVertical();
        }

        void DrawSelected()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(m_widthRHS));

            m_showMarkerSummary = BoldFoldout(m_showMarkerSummary, "Marker Summary");
            if (m_showMarkerSummary)
            {
                if (IsAnalysisValid())
                {
                    List<MarkerData> markers = m_analysis.GetMarkers();
                    if (markers != null)
                    {
                        int markerAt = m_selectedMarker;
                        if (markerAt >= 0 && markerAt < markers.Count)
                        {
                            var marker = markers[markerAt];

                            EditorGUILayout.LabelField(marker.name,
                                GUILayout.MaxWidth(m_widthRHS -
                                                   (GUI.skin.box.padding.horizontal + GUI.skin.box.margin.horizontal)));
                            DrawFrameRatio(marker);

                            EditorGUILayout.BeginHorizontal();
                            DrawColumn(0, "First frame");
                            DrawColumn(1, "");
                            DrawFrameIndexButton(marker.firstFrameIndex);
                            EditorGUILayout.EndHorizontal();

                            GUIStyle style = new GUIStyle(GUI.skin.label);
                            GUILayout.Space(style.lineHeight);

                            SetColumnSizes(m_widthColumn0, m_widthColumn1, m_widthColumn2, m_widthColumn3);
                            Draw3Column("", "ms", "Frame");
                            Draw2Column("Lower Quartile", marker.msLowerQuartile);
                            Draw3ColumnLabelMsFrame("Median", marker.msMedian, marker.medianFrameIndex);
                            Draw2Column("Upper Quartile", marker.msUpperQuartile);
                            Draw3ColumnLabelMsFrame("Individual Min", marker.msMinIndividual,
                                marker.minIndividualFrameIndex);
                            Draw3ColumnLabelMsFrame("Individual Max", marker.msMaxIndividual,
                                marker.maxIndividualFrameIndex);

                            GUILayout.Space(style.lineHeight);

                            DrawTop(10, marker);

                            GUILayout.Space(style.lineHeight);

                            EditorGUILayout.BeginHorizontal();
                            DrawHistogramForMarker(marker);
                            DrawBoxAndWhiskerPlotForMarker(40 + GUI.skin.box.padding.horizontal, 100, m_analysis, marker,
                                                           marker.msMin, marker.msMax, m_colorStandardLine, GetBarColors()[0]);
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No marker data selected");
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}