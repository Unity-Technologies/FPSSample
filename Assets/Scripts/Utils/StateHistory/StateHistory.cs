using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


#if UNITY_EDITOR
public class StateHistory      
{
    public delegate void MispredictDelegate();

    public static event MispredictDelegate mispredictEvent;

    public abstract class ComponentData
    {
        public Component owner;

        public List<int> mispredictedTicks = new List<int>();
        public abstract System.Object GetState(int tick);
        public abstract System.Object GetPredicted(int tick);
    }

    public const int BufferSize = 128;

    public static int HighestTick { get; private set; }

    public static bool Enabled
    {
        get { return m_enabled; }
        set
        {
            if (value != m_enabled)
            {
                m_enabled = value;
                Reset();
            }
        }
    }


    public static void Initialize()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.pauseStateChanged += OnPauseModeChanged;
        EditorApplication.quitting += OnQuitting; ;
    }

    private static void OnQuitting()
    {
        Reset();
    }

    private static void OnPauseModeChanged(PauseState state)
    {
        if(state == PauseState.Unpaused)
            Reset();
    }

    private static void OnPlayModeChanged(PlayModeStateChange obj)
    {
        Reset();
    }

    static void Reset()
    {
        m_entries.Clear();
        m_CommandData = null;
    }

    public static List<Component> GetComponents()
    {
        return new List<Component>(m_entries.Keys);
    }

    public static ComponentData GetComponentData(Component owner)
    {
        if (!m_enabled)
            return null;

        ComponentData entryBase;
        if (m_entries.TryGetValue(owner, out entryBase))
        {
            return entryBase;
        }
        return null;
    }
    
    public static void SetState<T>(Component owner, int tick, ref T state) where T : struct
    {
        if (!m_enabled)
            return;

        GenericData<T> entry = GetEntry<T>(owner);

        int index = entry.stateTicks.Register((uint)tick);
        entry.states[index] = state;

        int predictionIndex = entry.predictionTicks.GetIndex((uint)tick);
        if (predictionIndex != -1)
        {
            var predictedState = entry.predictions[predictionIndex] as IPredictedData<T>;
            if (predictedState != null)
            {
                if (!predictedState.VerifyPrediction(ref state))
                {
                    entry.mispredictedTicks.Add(tick);
                    System.Text.StringBuilder strBuilder = new System.Text.StringBuilder();
                    strBuilder.AppendLine("Prediction fail tick:" + tick);
                    strBuilder.AppendLine("PREDICTED");
                    strBuilder.Append(predictedState.ToString());
                    strBuilder.AppendLine("SERVER");
                    strBuilder.Append(state.ToString());
                    GameDebug.LogWarning(strBuilder.ToString());

                    if(mispredictEvent != null)
                        mispredictEvent();
                }

            }
        }

        HighestTick = Mathf.Max(HighestTick, tick);
    }

    public static void SetPredictedState<T>(Component owner, int tick, ref T state) where T : struct //, IPredictedState<T>
    {
        if (!m_enabled)
            return;

        var entry = GetEntry<T>(owner);

        var index = entry.predictionTicks.Register((uint)tick);
        entry.predictions[index] = state;

        HighestTick = Mathf.Max(HighestTick, tick);
    }

    public static bool GetPredictedState<T>(Component owner, int tick, ref T state) where T : struct //, IPredictedState<T>
    {
        if (!m_enabled)
            return false;

        var entry = GetEntry<T>(owner);

        var index = entry.predictionTicks.GetIndex((uint) tick);
        if (index == -1)
            return false;
        state = entry.predictions[index];
        return true;
    }


    public static void SetCommand<T>(int tick, ref T command) where T : struct
    {
        if (!m_enabled)
            return;

        if (m_CommandData == null)
            m_CommandData = new StateHistoryBuffer<T>(BufferSize);

        var commandData = m_CommandData as StateHistoryBuffer<T>;
        commandData.SetState(tick, ref command);
    }

    public static System.Object GetCommand<T>(int tick) where T : struct
    {
        if (!m_enabled)
            return null;

        if (m_CommandData == null)
            return null;
        
        var commandData = m_CommandData as StateHistoryBuffer<T>;
        return commandData.GetState(tick);
    }

    static GenericData<T> GetEntry<T>(Component owner) where T : struct
    {
        ComponentData entryBase;
        if (m_entries.TryGetValue(owner, out entryBase))
        {
            return entryBase as GenericData<T>;
        }

        GenericData<T>  entry = new GenericData<T>(BufferSize);
        entry.owner = owner;
        m_entries.Add(owner, entry);
        return entry;
    }

    class GenericData<T> : ComponentData where T : struct
    {
        public GenericData(int size)
        {
            states = new T[size];
            stateTicks = new SparseTickBuffer(size);
            predictions = new T[size];
            predictionTicks = new SparseTickBuffer(size);
        }

        public T[] states;
        public SparseTickBuffer stateTicks;


        public T[] predictions;
        public SparseTickBuffer predictionTicks;

        public override System.Object GetState(int tick)
        {
            int index = stateTicks.GetIndex((uint)tick);
            if (index == -1)
                return null;
            return states[index];  
        }

        public override System.Object GetPredicted(int tick)
        {
            int index = predictionTicks.GetIndex((uint)tick);
            if (index == -1)
                return null;
            return predictions[index];
        }
    }

    static bool m_enabled = false;
    static System.Object m_CommandData;     
    static Dictionary<Component, ComponentData> m_entries = new Dictionary<Component, ComponentData>();
}
#endif
