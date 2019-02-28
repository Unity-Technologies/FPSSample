using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditorInternal;
using System.Collections.Generic;

namespace UnityEditor.VFX.Utils
{
    static class VFXEventTesterWindow
    {
        public static bool Visible;

        [SerializeField]
        static string m_CustomEvent = "CustomEvent";
        [SerializeField]
        static List<EventAttribute> m_Attributes;

        static VisualEffect m_Effect;
        static ReorderableList list;
        static readonly string PreferenceName = "VFXEventTester.Visible";

        static VFXEventTesterWindow()
        {
            m_Attributes = new List<EventAttribute>();

            Visible = EditorPrefs.GetBool(PreferenceName, false);

            list = new ReorderableList(m_Attributes, typeof(EventAttribute), true, true, true, true);
            list.drawHeaderCallback = drawHeader;
            list.drawElementCallback = drawItem;
            list.onAddDropdownCallback = drawAddDropDown;
        }

        [MenuItem("Edit/Visual Effects/Show Event Tester", validate = false, priority = 320)]
        static void ToggleVisibility()
        {
            Visible = !Visible;
            EditorPrefs.SetBool(PreferenceName, Visible);

            UpdateVisibility();
        }

        [MenuItem("Edit/Visual Effects/Show Event Tester", validate = true, priority = 320)]
        static bool CheckToggleVisibility()
        {
            Visible = EditorPrefs.GetBool(PreferenceName, Visible);
            Menu.SetChecked("Edit/Visual Effects/Show Event Tester", Visible);
            return true;
        }


        [InitializeOnLoadMethod]
        static void UpdateVisibility()
        {
            if (Visible)
                SceneView.onSceneGUIDelegate += DrawWindow;
            else
                SceneView.onSceneGUIDelegate -= DrawWindow;


        }

        private static void drawAddDropDown(Rect buttonRect, ReorderableList list)
        {
            GenericMenu menu = new GenericMenu();
            // Add Generic Attributes
            menu.AddItem(new GUIContent("Standard/Position"), false, AddVector3, "position");
            menu.AddItem(new GUIContent("Standard/Velocity"), false, AddVector3, "velocity");
            menu.AddItem(new GUIContent("Standard/Size"), false, AddFloat, "size");
            menu.AddItem(new GUIContent("Standard/Mass"), false, AddFloat, "mass");
            menu.AddItem(new GUIContent("Standard/Color"), false, AddColor, "color");
            menu.AddItem(new GUIContent("Standard/Alpha"), false, AddFloat, "alpha");
            menu.AddItem(new GUIContent("Standard/Age"), false, AddFloat, "age");
            menu.AddItem(new GUIContent("Standard/Lifetime"), false, AddFloat, "lifetime");
            menu.AddItem(new GUIContent("Standard/Alive"), false, AddBool, "alive");
            menu.AddItem(new GUIContent("Standard/Scale.X"), false, AddFloat, "scaleX");
            menu.AddItem(new GUIContent("Standard/Scale.Y"), false, AddFloat, "scaleY");
            menu.AddItem(new GUIContent("Standard/Scale.Z"), false, AddFloat, "scaleZ");
            menu.AddItem(new GUIContent("Standard/Angle.X"), false, AddFloat, "angleX");
            menu.AddItem(new GUIContent("Standard/Angle.Y"), false, AddFloat, "angleY");
            menu.AddItem(new GUIContent("Standard/Angle.Z"), false, AddFloat, "angleZ");
            // Add Advanced Attributes
            menu.AddItem(new GUIContent("Advanced/OldPosition"), false, AddVector3, "oldPosition");
            menu.AddItem(new GUIContent("Advanced/TargetPosition"), false, AddVector3, "targetPosition");
            menu.AddItem(new GUIContent("Advanced/Direction"), false, AddVector3, "direction");
            menu.AddItem(new GUIContent("Advanced/AngularVelocity.X"), false, AddFloat, "angularVelocityX");
            menu.AddItem(new GUIContent("Advanced/AngularVelocity.Y"), false, AddFloat, "angularVelocityY");
            menu.AddItem(new GUIContent("Advanced/AngularVelocity.Z"), false, AddFloat, "angularVelocityZ");
            // Add Generic Attributes
            menu.AddItem(new GUIContent("Custom/Custom bool"), false, AddBool, "customBool");
            menu.AddItem(new GUIContent("Custom/Custom Float"), false, AddFloat, "customFloat");
            menu.AddItem(new GUIContent("Custom/Custom Vector2"), false, AddVector2, "customVector2");
            menu.AddItem(new GUIContent("Custom/Custom Vector3"), false, AddVector3, "customVector3");
            menu.AddItem(new GUIContent("Custom/Custom Color"), false, AddColor, "customColor");

            // Add Custom Types
            menu.ShowAsContext();
        }

        static void AddFloat(object name)
        {
            m_Attributes.Add(new EventAttribute(name as string, EventAttributeType.Float, 0.0f));
        }

        static void AddBool(object name)
        {
            m_Attributes.Add(new EventAttribute(name as string, EventAttributeType.Bool, true));
        }

        static void AddVector2(object name)
        {
            m_Attributes.Add(new EventAttribute(name as string, EventAttributeType.Vector2, Vector2.zero));
        }

        static void AddVector3(object name)
        {
            m_Attributes.Add(new EventAttribute(name as string, EventAttributeType.Vector3, Vector3.zero));
        }

        static void AddColor(object name)
        {
            m_Attributes.Add(new EventAttribute(name as string, EventAttributeType.Color, Color.white));
        }



        static void DrawWindow(SceneView sceneView)
        {
            if (Selection.activeGameObject != null)
            {
                var component = Selection.activeGameObject.GetComponent<VisualEffect>();
                if (component != null)
                {
                    m_Effect = component;
                    SceneViewOverlay.Window(Contents.title, WindowFunction, 601, SceneViewOverlay.WindowDisplayOption.OneWindowPerTitle);
                }
                else
                {
                    m_Effect = null;
                }
            }
        }

        static void WindowFunction(UnityEngine.Object target, SceneView sceneView)
        {
            WindowGUI();
        }

        [System.Serializable]
        enum EventAttributeType
        {
            Float = 0,
            Vector2 = 1,
            Vector3 = 2,
            Color = 3,
            Bool = 4
        }

        [System.Serializable]
        class EventAttribute
        {
            public string name;
            public EventAttributeType type;
            public object value;

            public EventAttribute()
            {
                name = "Attribute";
                type = EventAttributeType.Float;
                value = 0.0f;
            }

            public EventAttribute(string name, EventAttributeType type, object value)
            {
                this.name = name;
                this.type = type;
                this.value = value;
            }
        }

        private static void drawHeader(Rect rect)
        {
            GUI.Label(rect, "Event Attributes");
        }

        static void drawItem(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (m_Attributes[index] == null)
            {
                var color = GUI.color;
                GUI.color = Color.red;
                EditorGUI.LabelField(rect, "NULL OR DELETED");
                GUI.color = color;
                return;
            }

            rect.yMin += 2;
            rect.height = 16;

            var namerect = rect;
            namerect.width = 100;

            m_Attributes[index].name = GUI.TextField(namerect, m_Attributes[index].name);

            var typerect = rect;
            typerect.xMin = rect.xMin + 108;
            typerect.width = 64;
            m_Attributes[index].type = (EventAttributeType)EditorGUI.EnumPopup(typerect, m_Attributes[index].type);

            var valueRect = rect;
            valueRect.xMin = rect.xMin + 180;
            switch (m_Attributes[index].type)
            {
                case EventAttributeType.Bool:
                    if (m_Attributes[index].value == null || !(m_Attributes[index].value is bool))
                        m_Attributes[index].value = true;

                    m_Attributes[index].value = (bool)EditorGUI.Toggle(valueRect, (bool)m_Attributes[index].value);
                    break;

                case EventAttributeType.Float:
                    if (m_Attributes[index].value == null || !(m_Attributes[index].value is float))
                        m_Attributes[index].value = 1.0f;

                    m_Attributes[index].value = (float)EditorGUI.FloatField(valueRect, (float)m_Attributes[index].value);
                    break;

                case EventAttributeType.Vector2:
                    if (m_Attributes[index].value == null || !(m_Attributes[index].value is Vector2))
                        m_Attributes[index].value = Vector2.zero;

                    m_Attributes[index].value = (Vector2)EditorGUI.Vector2Field(valueRect, "", (Vector2)m_Attributes[index].value);
                    break;

                case EventAttributeType.Vector3:
                    if (m_Attributes[index].value == null || !(m_Attributes[index].value is Vector3))
                        m_Attributes[index].value = Vector3.zero;

                    m_Attributes[index].value = (Vector3)EditorGUI.Vector3Field(valueRect, "", (Vector3)m_Attributes[index].value);
                    break;

                case EventAttributeType.Color:
                    if (m_Attributes[index].value == null || !(m_Attributes[index].value is Color))
                        m_Attributes[index].value = Color.white;

                    m_Attributes[index].value = (Color)EditorGUI.ColorField(valueRect, (Color)m_Attributes[index].value);
                    break;
            }
        }

        static void WindowGUI()
        {
            EditorGUI.BeginDisabled(m_Effect == null);
            EditorGUILayout.Space();
            list.DoLayoutList();
            EditorGUILayout.Space();
            using (new GUILayout.HorizontalScope(GUILayout.Width(360)))
            {
                if (GUILayout.Button("Play", Styles.leftButton, GUILayout.Height(24)))
                {
                    SendEvent("OnPlay");
                }
                if (GUILayout.Button("Stop", Styles.middleButton, GUILayout.Height(24)))
                {
                    SendEvent("OnStop");
                }
                if (GUILayout.Button("Custom", Styles.rightButton, GUILayout.Height(24)))
                {
                    SendEvent(m_CustomEvent);
                }
            }
            m_CustomEvent = EditorGUILayout.TextField("Custom Event", m_CustomEvent);
            EditorGUI.EndDisabled();
        }

        static void SendEvent(string name)
        {
            if (m_Effect == null) return;
            var attrib = m_Effect.CreateVFXEventAttribute();

            // set all attributes
            foreach (var attribute in m_Attributes)
            {
                if (attribute == null) continue;
                switch (attribute.type)
                {
                    case EventAttributeType.Bool: attrib.SetBool(attribute.name, (bool)attribute.value); break;
                    case EventAttributeType.Float: attrib.SetFloat(attribute.name, (float)attribute.value); break;
                    case EventAttributeType.Vector2: attrib.SetVector2(attribute.name, (Vector2)attribute.value); break;
                    case EventAttributeType.Vector3: attrib.SetVector3(attribute.name, (Vector3)attribute.value); break;
                    case EventAttributeType.Color: attrib.SetVector4(attribute.name, (Color)attribute.value); break;
                }
            }

            // then send event with attributes
            if (name == "OnPlay")
                m_Effect.Play(attrib);
            else if (name == "OnStop")
                m_Effect.Stop(attrib);
            else
                m_Effect.SendEvent(name, attrib);

        }

        static class Contents
        {
            public static readonly GUIContent title = new GUIContent("VFX Event Tester");
        }

        static class Styles
        {
            public static GUIStyle leftButton;
            public static GUIStyle middleButton;
            public static GUIStyle rightButton;

            static Styles()
            {
                leftButton = new GUIStyle(EditorStyles.miniButtonLeft);
                middleButton = new GUIStyle(EditorStyles.miniButtonMid);
                rightButton = new GUIStyle(EditorStyles.miniButtonRight);
                leftButton.fontSize = 12;
                middleButton.fontSize = 12;
                rightButton.fontSize = 12;
            }
        }

    }

}

