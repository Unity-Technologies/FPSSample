using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class StateHistorySampler
{
    public class ComponentData
    {
        public ComponentData(Component component, int rowCount)
        {
            this.component = component;
            states = new object[rowCount];
            predictedStates = new object[rowCount];
            predictionValid = new bool[rowCount];
        }

        public Component component;
        public System.Object[] states;
        public System.Object[] predictedStates;
        public bool[] predictionValid;
    }

    public class CaptureResult
    {
        public CaptureResult(int firstTick, int count)
        {
            this.firstTick = firstTick;
            this.count = count;
            commands = new System.Object[count];
        }

        public int firstTick;
        public int count;
        public System.Object[] commands;    
        public List<ComponentData> componentData = new List<ComponentData>();
    }

    public delegate void CaptureDelegate(CaptureResult result);

    public static bool captureOnMispredict;
    public static event CaptureDelegate Capture;

    [MenuItem("FPS Sample/Hotkeys/Capture state history _%H")]
    static void HotkeyCapture()
    {
        if (!StateHistory.Enabled)
            return;

        RequestCapture();
    }

    static StateHistorySampler()
    {
        StateHistory.mispredictEvent += OnMispredict;
    }

    public static void RequestCapture()
    {
        if (!StateHistory.Enabled)
            return;

        if (!RegisterUpate())
            return;

        m_captureRequested = true;
    }

    static void OnMispredict()
    {
        if (captureOnMispredict)
        {
            RequestCapture();
        }
    }



    public static bool RegisterUpate()
    {
        if (m_isUpdateRegistered)
            return true;

        if (Game.game == null)
            return false;

        Game.game.endUpdateEvent += OnUpdateEvent;
        m_isUpdateRegistered = true;
        return m_isUpdateRegistered;
    }

    private static void OnUpdateEvent()
    {
        if (m_captureRequested)
        {
            m_captureRequested = false;


            // Capture
            CaptureResult capture = new CaptureResult(StateHistory.HighestTick - StateHistory.BufferSize + 1, StateHistory.BufferSize);

            // Commands
            {
                for (int i = 0; i < capture.count; i++)
                {
                    int tick = i + capture.firstTick;
                    capture.commands[i] = StateHistory.GetCommand<UserCommand>(tick);
                }
            }

            // Components
            List<Component> components = StateHistory.GetComponents();
            foreach (var component in components)
            {
                if (component == null || component.gameObject == null)
                    continue;
                
                var data = StateHistory.GetComponentData(component);

                var componentData = new ComponentData(component, capture.count);
                capture.componentData.Add(componentData);
                for (int i = 0; i < StateHistory.BufferSize; i++)
                {
                    int tick = i + capture.firstTick;

                    componentData.states[i] = data.GetState(tick);
                    componentData.predictedStates[i] = data.GetPredicted(tick);
                    componentData.predictionValid[i] = !data.mispredictedTicks.Contains(tick);
                }
            }

            // Sort componts by gameObject
            capture.componentData.Sort((a, b) => (a.component.gameObject.name.CompareTo(b.component.gameObject.name)));

            Capture?.Invoke(capture);
        }
    }

    static bool m_isUpdateRegistered;
    static bool m_captureRequested;
}
