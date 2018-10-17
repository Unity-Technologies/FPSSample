using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering
{
    public enum DebugAction
    {
        EnableDebugMenu,
        PreviousDebugPanel,
        NextDebugPanel,
        Action,
        MakePersistent,
        MoveVertical,
        MoveHorizontal,
        Multiplier,
        DebugActionCount
    }

    enum DebugActionRepeatMode
    {
        Never,
        Delay
    }

    public sealed partial class DebugManager
    {
        const string kEnableDebugBtn1  = "Enable Debug Button 1";
        const string kEnableDebugBtn2  = "Enable Debug Button 2";
        const string kDebugPreviousBtn = "Debug Previous";
        const string kDebugNextBtn     = "Debug Next";
        const string kValidateBtn      = "Debug Validate";
        const string kPersistentBtn    = "Debug Persistent";
        const string kDPadVertical     = "Debug Vertical";
        const string kDPadHorizontal   = "Debug Horizontal";
        const string kMultiplierBtn    = "Debug Multiplier";

        DebugActionDesc[] m_DebugActions;
        DebugActionState[] m_DebugActionStates;

        void RegisterActions()
        {
            m_DebugActions = new DebugActionDesc[(int)DebugAction.DebugActionCount];
            m_DebugActionStates = new DebugActionState[(int)DebugAction.DebugActionCount];

            var enableDebugMenu = new DebugActionDesc();
            enableDebugMenu.buttonTriggerList.Add(new[] { kEnableDebugBtn1, kEnableDebugBtn2 });
            enableDebugMenu.keyTriggerList.Add(new[] { KeyCode.LeftControl, KeyCode.Backspace });
            enableDebugMenu.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.EnableDebugMenu, enableDebugMenu);

            var nextDebugPanel = new DebugActionDesc();
            nextDebugPanel.buttonTriggerList.Add(new[] { kDebugNextBtn });
            nextDebugPanel.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.NextDebugPanel, nextDebugPanel);

            var previousDebugPanel = new DebugActionDesc();
            previousDebugPanel.buttonTriggerList.Add(new[] { kDebugPreviousBtn });
            previousDebugPanel.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.PreviousDebugPanel, previousDebugPanel);

            var validate = new DebugActionDesc();
            validate.buttonTriggerList.Add(new[] { kValidateBtn });
            validate.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.Action, validate);

            var persistent = new DebugActionDesc();
            persistent.buttonTriggerList.Add(new[] { kPersistentBtn });
            persistent.repeatMode = DebugActionRepeatMode.Never;
            AddAction(DebugAction.MakePersistent, persistent);

            var multiplier = new DebugActionDesc();
            multiplier.buttonTriggerList.Add(new[] { kMultiplierBtn });
            multiplier.repeatMode = DebugActionRepeatMode.Delay;
            validate.repeatDelay = 0f;
            AddAction(DebugAction.Multiplier, multiplier);

            AddAction(DebugAction.MoveVertical, new DebugActionDesc { axisTrigger = kDPadVertical, repeatMode = DebugActionRepeatMode.Delay, repeatDelay = 0.16f });
            AddAction(DebugAction.MoveHorizontal, new DebugActionDesc { axisTrigger = kDPadHorizontal, repeatMode = DebugActionRepeatMode.Delay, repeatDelay = 0.16f });
        }

        void AddAction(DebugAction action, DebugActionDesc desc)
        {
            int index = (int)action;
            m_DebugActions[index] = desc;
            m_DebugActionStates[index] = new DebugActionState();
        }

        void SampleAction(int actionIndex)
        {
            var desc = m_DebugActions[actionIndex];
            var state = m_DebugActionStates[actionIndex];

            //bool canSampleAction = (state.actionTriggered == false) || (desc.repeatMode == DebugActionRepeatMode.Delay && state.timer > desc.repeatDelay);
            if (state.runningAction == false)
            {
                // Check button triggers
                for (int buttonListIndex = 0; buttonListIndex < desc.buttonTriggerList.Count; ++buttonListIndex)
                {
                    var buttons = desc.buttonTriggerList[buttonListIndex];
                    bool allButtonPressed = true;

                    foreach (var button in buttons)
                    {
                        allButtonPressed = Input.GetButton(button);
                        if (!allButtonPressed)
                            break;
                    }

                    if (allButtonPressed)
                    {
                        state.TriggerWithButton(buttons, 1f);
                        break;
                    }
                }

                // Check axis triggers
                if (desc.axisTrigger != "")
                {
                    float axisValue = Input.GetAxis(desc.axisTrigger);

                    if (axisValue != 0f)
                        state.TriggerWithAxis(desc.axisTrigger, axisValue);
                }

                // Check key triggers
                for (int keyListIndex = 0; keyListIndex < desc.keyTriggerList.Count; ++keyListIndex)
                {
                    var keys = desc.keyTriggerList[keyListIndex];
                    bool allKeyPressed = true;

                    foreach (var key in keys)
                    {
                        allKeyPressed = Input.GetKey(key);
                        if (!allKeyPressed)
                            break;
                    }

                    if (allKeyPressed)
                    {
                        state.TriggerWithKey(keys, 1f);
                        break;
                    }
                }
            }
        }

        void UpdateAction(int actionIndex)
        {
            var desc = m_DebugActions[actionIndex];
            var state = m_DebugActionStates[actionIndex];

            if (state.runningAction)
                state.Update(desc);
        }

        public void UpdateActions()
        {
            for (int actionIndex = 0; actionIndex < m_DebugActions.Length; ++actionIndex)
            {
                UpdateAction(actionIndex);
                SampleAction(actionIndex);
            }
        }

        public float GetAction(DebugAction action)
        {
            return m_DebugActionStates[(int)action].actionState;
        }

        void RegisterInputs()
        {
#if UNITY_EDITOR
            var inputEntries = new List<InputManagerEntry>
            {
                new InputManagerEntry { name = kEnableDebugBtn1,  kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "left ctrl",   altBtnPositive = "joystick button 8" },
                new InputManagerEntry { name = kEnableDebugBtn2,  kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "backspace",   altBtnPositive = "joystick button 9" },
                new InputManagerEntry { name = kDebugNextBtn,     kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "page down",   altBtnPositive = "joystick button 5" },
                new InputManagerEntry { name = kDebugPreviousBtn, kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "page up",     altBtnPositive = "joystick button 4" },
                new InputManagerEntry { name = kValidateBtn,      kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "return",      altBtnPositive = "joystick button 0" },
                new InputManagerEntry { name = kPersistentBtn,    kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "right shift", altBtnPositive = "joystick button 2" },
                new InputManagerEntry { name = kMultiplierBtn,    kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "left shift",  altBtnPositive = "joystick button 3" },
                new InputManagerEntry { name = kDPadHorizontal,   kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "right",       btnNegative = "left", gravity = 1000f, deadZone = 0.001f, sensitivity = 1000f },
                new InputManagerEntry { name = kDPadVertical,     kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "up",          btnNegative = "down", gravity = 1000f, deadZone = 0.001f, sensitivity = 1000f },
                new InputManagerEntry { name = kDPadVertical,     kind = InputManagerEntry.Kind.Axis, axis = InputManagerEntry.Axis.Seventh, btnPositive = "up",    btnNegative = "down", gravity = 1000f, deadZone = 0.001f, sensitivity = 1000f },
                new InputManagerEntry { name = kDPadHorizontal,   kind = InputManagerEntry.Kind.Axis, axis = InputManagerEntry.Axis.Sixth,   btnPositive = "right", btnNegative = "left", gravity = 1000f, deadZone = 0.001f, sensitivity = 1000f },
            };

            InputRegistering.RegisterInputs(inputEntries);
#endif
        }
    }

    class DebugActionDesc
    {
        public List<string[]> buttonTriggerList = new List<string[]>();
        public string axisTrigger = "";
        public List<KeyCode[]> keyTriggerList = new List<KeyCode[]>();
        public DebugActionRepeatMode repeatMode = DebugActionRepeatMode.Never;
        public float repeatDelay;
    }

    class DebugActionState
    {
        enum DebugActionKeyType
        {
            Button,
            Axis,
            Key
        }

        DebugActionKeyType m_Type;
        string[] m_PressedButtons;
        string m_PressedAxis = "";
        KeyCode[] m_PressedKeys;
        bool[] m_TriggerPressedUp;
        float m_Timer;

        internal bool runningAction { get; private set; }
        internal float actionState { get; private set; }

        void Trigger(int triggerCount, float state)
        {
            actionState = state;
            runningAction = true;
            m_Timer = 0f;

            m_TriggerPressedUp = new bool[triggerCount];
            for (int i = 0; i < m_TriggerPressedUp.Length; ++i)
                m_TriggerPressedUp[i] = false;
        }

        public void TriggerWithButton(string[] buttons, float state)
        {
            m_Type = DebugActionKeyType.Button;
            m_PressedButtons = buttons;
            m_PressedAxis = "";
            Trigger(buttons.Length, state);
        }

        public void TriggerWithAxis(string axis, float state)
        {
            m_Type = DebugActionKeyType.Axis;
            m_PressedAxis = axis;
            Trigger(1, state);
        }

        public void TriggerWithKey(KeyCode[] keys, float state)
        {
            m_Type = DebugActionKeyType.Key;
            m_PressedKeys = keys;
            m_PressedAxis = "";
            Trigger(keys.Length, state);
        }

        void Reset()
        {
            runningAction = false;
            m_Timer = 0f;
            m_TriggerPressedUp = null;
        }

        public void Update(DebugActionDesc desc)
        {
            // Always reset this so that the action can only be caught once until repeat/reset
            actionState = 0f;

            if (m_TriggerPressedUp != null)
            {
                m_Timer += Time.deltaTime;

                for (int i = 0; i < m_TriggerPressedUp.Length; ++i)
                {
                    if (m_Type == DebugActionKeyType.Button)
                        m_TriggerPressedUp[i] |= Input.GetButtonUp(m_PressedButtons[i]);
                    else if (m_Type == DebugActionKeyType.Axis)
                        m_TriggerPressedUp[i] |= Mathf.Approximately(Input.GetAxis(m_PressedAxis), 0f);
                    else
                        m_TriggerPressedUp[i] |= Input.GetKeyUp(m_PressedKeys[i]);
                }

                bool allTriggerUp = true;
                foreach (bool value in m_TriggerPressedUp)
                    allTriggerUp &= value;

                if (allTriggerUp || (m_Timer > desc.repeatDelay && desc.repeatMode == DebugActionRepeatMode.Delay))
                    Reset();
            }
        }
    }
}
