using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.VFX.UI
{
    [InitializeOnLoad]
    public class VFXFilterWindow : EditorWindow
    {
        public interface IProvider
        {
            void CreateComponentTree(List<VFXFilterWindow.Element> tree);
            bool GoToChild(VFXFilterWindow.Element element, bool addIfComponent);

            Vector2 position { get; set; }
        }

        public static readonly float DefaultWidth = 240f;
        public static readonly float DefaultHeight = 300f;

        #region BaseElements

        public class Element : IComparable
        {
            public int level;
            public GUIContent content;

            public string name
            {
                get { return content.text; }
            }

            public int CompareTo(object o)
            {
                return name.CompareTo((o as Element).name);
            }
        }

        [Serializable]
        public class GroupElement : Element
        {
            public Vector2 scroll;
            public int selectedIndex = 0;

            public GroupElement(int level, string name)
            {
                this.level = level;
                content = new GUIContent(name);
            }

            public bool WantsFocus { get; protected set; }

            public virtual bool ShouldDisable
            {
                get { return false; }
            }

            public virtual bool HandleKeyboard(Event evt, VFXFilterWindow w, Action goToParent)
            {
                return false;
            }

            public virtual bool OnGUI(VFXFilterWindow sFilterWindow)
            {
                return false;
            }
        }
        #endregion

        // Styles

        class Styles
        {
            public GUIStyle header = (GUIStyle)typeof(EditorStyles).GetProperty("inspectorBig", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null, null);
            public GUIStyle componentButton = new GUIStyle("PR Label"); //new GUIStyle (EditorStyles.label);
            public GUIStyle groupButton;
            public GUIStyle background = "grey_border";
            public GUIStyle previewBackground = "PopupCurveSwatchBackground";
            public GUIStyle previewHeader = new GUIStyle(EditorStyles.label);
            public GUIStyle previewText = new GUIStyle(EditorStyles.wordWrappedLabel);
            public GUIStyle rightArrow = "AC RightArrow";
            public GUIStyle leftArrow = "AC LeftArrow";


            public Styles()
            {
                header.font = EditorStyles.boldLabel.font;

                componentButton.alignment = TextAnchor.MiddleLeft;
                componentButton.padding.left -= 15;
                componentButton.fixedHeight = 20;

                groupButton = new GUIStyle(componentButton);
                groupButton.padding.left += 17;

                previewText.padding.left += 3;
                previewText.padding.right += 3;
                previewHeader.padding.left += 3 - 2;
                previewHeader.padding.right += 3;
                previewHeader.padding.top += 3;
                previewHeader.padding.bottom += 2;
            }
        }

        // Constants

        private const int kHeaderHeight = 30;
        private const int kWindowHeight = 400 - 80;
        private const int kHelpHeight = 80 * 0;
        private const string kComponentSearch = "NodeSearchString";

        // Static variables

        private static Styles s_Styles;
        private static VFXFilterWindow s_FilterWindow = null;
        private static long s_LastClosedTime;
        private static bool s_DirtyList = false;

        // Member variables
        private IProvider m_Provider;

        private Element[] m_Tree;
        private Element[] m_SearchResultTree;
        private List<GroupElement> m_Stack = new List<GroupElement>();

        private float m_Anim = 1;
        private int m_AnimTarget = 1;
        private long m_LastTime = 0;
        private bool m_ScrollToSelected = false;
        private string m_DelayedSearch = null;
        private string m_Search = "";

        // Properties

        private bool hasSearch { get { return !string.IsNullOrEmpty(m_Search); } }
        private GroupElement activeParent { get { return m_Stack[m_Stack.Count - 2 + m_AnimTarget]; } }
        private Element[] activeTree { get { return hasSearch ? m_SearchResultTree : m_Tree; } }
        private Element activeElement
        {
            get
            {
                if (activeTree == null)
                    return null;

                List<Element> children = GetChildren(activeTree, activeParent);
                if (children.Count == 0)
                    return null;

                return children[activeParent.selectedIndex];
            }
        }
        private bool isAnimating { get { return m_Anim != m_AnimTarget; } }

        // Methods

        static VFXFilterWindow()
        {
            s_DirtyList = true;
        }

        void OnEnable()
        {
            s_FilterWindow = this;
            m_Search = "";
        }

        void OnDisable()
        {
            s_LastClosedTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            s_FilterWindow = null;
        }

        internal static bool ValidateAddComponentMenuItem()
        {
            return true;
        }

        EditorWindow m_mainWindow;

        internal static bool Show(EditorWindow mainWindow, Vector2 graphPosition, Vector2 screenPosition, IProvider provider)
        {
            // We could not use realtimeSinceStartUp since it is set to 0 when entering/exitting playmode, we assume an increasing time when comparing time.
            long nowMilliSeconds = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            bool justClosed = nowMilliSeconds < s_LastClosedTime + 50;
            if (!justClosed)
            {
                if (Event.current != null)
                    Event.current.Use();
                if (s_FilterWindow == null)
                {
                    s_FilterWindow = ScriptableObject.CreateInstance<VFXFilterWindow>();
                    s_FilterWindow.hideFlags = HideFlags.HideAndDontSave;
                }
                s_FilterWindow.Init(graphPosition, screenPosition, provider);
                s_FilterWindow.m_mainWindow = mainWindow;
                return true;
            }
            return false;
        }

        private static object Invoke(Type t, object inst, string method, params object[] args)
        {
            var mi = t.GetMethod(method, (inst == null ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.NonPublic);
            return mi.Invoke(inst, args);
        }

        void Init(Vector2 graphPosition, Vector2 screenPosition, IProvider provider)
        {
            m_Provider = provider;
            // Has to be done before calling Show / ShowWithMode
            m_Provider.position = graphPosition;

            Rect buttonRect = new Rect(screenPosition.x - DefaultWidth / 2, screenPosition.y - 16, DefaultWidth, 1);

            CreateComponentTree();

            ShowAsDropDown(buttonRect, new Vector2(buttonRect.width, kWindowHeight));

            Focus();

            wantsMouseMove = true;
        }

        private void CreateComponentTree()
        {
            var tree = new List<Element>();
            if (m_Provider == null) return;
            m_Provider.CreateComponentTree(tree);


            m_Tree = tree.ToArray();

            // Rebuild stack
            if (m_Stack.Count == 0)
                m_Stack.Add(m_Tree[0] as GroupElement);
            else
            {
                // The root is always the match for level 0
                GroupElement match = m_Tree[0] as GroupElement;
                int level = 0;
                while (true)
                {
                    // Assign the match for the current level
                    GroupElement oldElement = m_Stack[level];
                    m_Stack[level] = match;
                    m_Stack[level].selectedIndex = oldElement.selectedIndex;
                    m_Stack[level].scroll = oldElement.scroll;

                    // See if we reached last element of stack
                    level++;
                    if (level == m_Stack.Count)
                        break;

                    // Try to find a child of the same name as we had before
                    List<Element> children = GetChildren(activeTree, match);
                    Element childMatch = children.FirstOrDefault(c => c.name == m_Stack[level].name);
                    if (childMatch != null && childMatch is GroupElement)
                    {
                        match = childMatch as GroupElement;
                    }
                    else
                    {
                        // If we couldn't find the child, remove all further elements from the stack
                        while (m_Stack.Count > level)
                            m_Stack.RemoveAt(level);
                    }
                }
            }

            //Debug.Log ("Rebuilt tree - "+m_Tree.Length+" elements");
            s_DirtyList = false;
            RebuildSearch();
        }

        internal void OnGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            GUI.Label(new Rect(0, 0, position.width, position.height), GUIContent.none, s_Styles.background);


            if (s_DirtyList)
                CreateComponentTree();

            if (m_Tree == null)
            {
                Close();
                return;
            }

            // Keyboard
            HandleKeyboard();

            GUILayout.Space(7);

            // Search
            if (!(activeParent.WantsFocus))
            {
                EditorGUI.FocusTextInControl("ComponentSearch");
                Focus();
            }
            Rect searchRect = GUILayoutUtility.GetRect(10, 20);
            searchRect.x += 8;
            searchRect.width -= 16;

            GUI.SetNextControlName("ComponentSearch");

            using (new DisabledScope(activeParent.ShouldDisable))
            {
                string newSearch = (string)Invoke(typeof(EditorGUI), null, "SearchField", searchRect, m_DelayedSearch ?? m_Search);

                if (newSearch != m_Search || m_DelayedSearch != null)
                {
                    if (!isAnimating)
                    {
                        m_Search = m_DelayedSearch ?? newSearch;
                        EditorPrefs.SetString(kComponentSearch, m_Search);
                        RebuildSearch();
                        m_DelayedSearch = null;
                    }
                    else
                    {
                        m_DelayedSearch = newSearch;
                    }
                }
            }

            // Show lists
            ListGUI(activeTree, m_Anim, GetElementRelative(0), GetElementRelative(-1));
            if (m_Anim < 1)
                ListGUI(activeTree, m_Anim + 1, GetElementRelative(-1), GetElementRelative(-2));

            // Show help area
            //DrawHelpArea (new Rect (0, position.height - kHelpHeight, position.width, kHelpHeight));

            // Animate
            if (isAnimating && Event.current.type == EventType.Repaint)
            {
                long now = System.DateTime.Now.Ticks;
                float deltaTime = (now - m_LastTime) / (float)System.TimeSpan.TicksPerSecond;
                m_LastTime = now;
                m_Anim = Mathf.MoveTowards(m_Anim, m_AnimTarget, deltaTime * 4);
                if (m_AnimTarget == 0 && m_Anim == 0)
                {
                    m_Anim = 1;
                    m_AnimTarget = 1;
                    m_Stack.RemoveAt(m_Stack.Count - 1);
                }
                Repaint();
            }
        }

        private void HandleKeyboard()
        {
            Event evt = Event.current;
            if (evt.type == EventType.KeyDown)
            {
                // Special handling when in new script panel
                if (!activeParent.HandleKeyboard(evt, s_FilterWindow, GoToParent))
                {
                    // Always do these
                    if (evt.keyCode == KeyCode.DownArrow)
                    {
                        activeParent.selectedIndex++;
                        activeParent.selectedIndex = Mathf.Min(activeParent.selectedIndex, GetChildren(activeTree, activeParent).Count - 1);
                        m_ScrollToSelected = true;
                        evt.Use();
                    }
                    if (evt.keyCode == KeyCode.UpArrow)
                    {
                        activeParent.selectedIndex--;
                        activeParent.selectedIndex = Mathf.Max(activeParent.selectedIndex, 0);
                        m_ScrollToSelected = true;
                        evt.Use();
                    }
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        GoToChild(activeElement, true);
                        evt.Use();
                    }

                    // Do these if we're not in search mode
                    if (!hasSearch)
                    {
                        if (evt.keyCode == KeyCode.LeftArrow || evt.keyCode == KeyCode.Backspace)
                        {
                            GoToParent();
                            evt.Use();
                        }
                        if (evt.keyCode == KeyCode.RightArrow)
                        {
                            GoToChild(activeElement, false);
                            evt.Use();
                        }
                        if (evt.keyCode == KeyCode.Escape)
                        {
                            Close();
                            evt.Use();
                        }
                    }
                }
            }
        }

        const string kSearchHeader = "Search";

        private void RebuildSearch()
        {
            if (!hasSearch)
            {
                m_SearchResultTree = null;
                if (m_Stack[m_Stack.Count - 1].name == kSearchHeader)
                {
                    m_Stack.Clear();
                    m_Stack.Add(m_Tree[0] as GroupElement);
                }
                m_AnimTarget = 1;
                m_LastTime = System.DateTime.Now.Ticks;
                return;
            }

            // Support multiple search words separated by spaces.
            string[] searchWords = m_Search.ToLower().Split(' ');

            // We keep two lists. Matches that matches the start of an item always get first priority.
            List<Element> matchesStart = new List<Element>();
            List<Element> matchesWithin = new List<Element>();

            foreach (Element e in m_Tree)
            {
                if ((e is GroupElement)) //TODO RF
                    continue;

                string name = e.name.ToLower().Replace(" ", "");
                bool didMatchAll = true;
                bool didMatchStart = false;

                // See if we match ALL the seaarch words.
                for (int w = 0; w < searchWords.Length; w++)
                {
                    string search = searchWords[w];
                    if (name.Contains(search))
                    {
                        // If the start of the item matches the first search word, make a note of that.
                        if (w == 0 && name.StartsWith(search))
                            didMatchStart = true;
                    }
                    else
                    {
                        // As soon as any word is not matched, we disregard this item.
                        didMatchAll = false;
                        break;
                    }
                }
                // We always need to match all search words.
                // If we ALSO matched the start, this item gets priority.
                if (didMatchAll)
                {
                    if (didMatchStart)
                        matchesStart.Add(e);
                    else
                        matchesWithin.Add(e);
                }
            }

            matchesStart.Sort();
            matchesWithin.Sort();

            // Create search tree
            List<Element> tree = new List<Element>();
            // Add parent
            tree.Add(new GroupElement(0, kSearchHeader));
            // Add search results
            tree.AddRange(matchesStart);
            tree.AddRange(matchesWithin);
            // Add the new script element
            //tree.Add(m_Tree[m_Tree.Length - 1]);
            // Create search result tree
            m_SearchResultTree = tree.ToArray();
            m_Stack.Clear();
            m_Stack.Add(m_SearchResultTree[0] as GroupElement);

            // Always select the first search result when search is changed (e.g. a character was typed in or deleted),
            // because it's usually the best match.
            if (GetChildren(activeTree, activeParent).Count >= 1)
                activeParent.selectedIndex = 0;
            else
                activeParent.selectedIndex = -1;
        }

        private GroupElement GetElementRelative(int rel)
        {
            int i = m_Stack.Count + rel - 1;
            if (i < 0)
                return null;
            return m_Stack[i] as GroupElement;
        }

        private void GoToParent()
        {
            if (m_Stack.Count > 1)
            {
                m_AnimTarget = 0;
                m_LastTime = System.DateTime.Now.Ticks;
            }
        }

        private void ListGUI(Element[] tree, float anim, GroupElement parent, GroupElement grandParent)
        {
            // Smooth the fractional part of the anim value
            anim = Mathf.Floor(anim) + Mathf.SmoothStep(0, 1, Mathf.Repeat(anim, 1));

            // Calculate rect for animated area
            Rect animRect = position;
            animRect.x = position.width * (1 - anim) + 1;
            animRect.y = kHeaderHeight;
            animRect.height -= kHeaderHeight + kHelpHeight;
            animRect.width -= 2;

            // Start of animated area (the part that moves left and right)
            GUILayout.BeginArea(animRect);

            // Header
            Rect headerRect = GUILayoutUtility.GetRect(10, 25);
            string name = parent.name;
            GUI.Label(headerRect, name, s_Styles.header);

            // Back button
            if (grandParent != null)
            {
                Rect arrowRect = new Rect(headerRect.x + 4, headerRect.y + 7, 13, 13);
                if (Event.current.type == EventType.Repaint)
                    s_Styles.leftArrow.Draw(arrowRect, false, false, false, false);
                if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
                {
                    GoToParent();
                    Event.current.Use();
                }
            }

            //GUILayout.Space (10);

            if (!parent.OnGUI(s_FilterWindow))
                ListGUI(tree, parent);

            GUILayout.EndArea();
        }

        private void GoToChild(Element e, bool addIfComponent)
        {
            if (m_Provider.GoToChild(e, addIfComponent))
            {
                Close();
                if (m_mainWindow != null)
                {
                    m_mainWindow.Focus();
                }
            }
            else if (!hasSearch)//TODO RF || e is NewElement)
            {
                m_LastTime = System.DateTime.Now.Ticks;
                if (m_AnimTarget == 0)
                    m_AnimTarget = 1;
                else if (m_Anim == 1)
                {
                    m_Anim = 0;
                    m_Stack.Add(e as VFXFilterWindow.GroupElement);
                }
            }
        }

        private void ListGUI(Element[] tree, GroupElement parent)
        {
            // Start of scroll view list
            parent.scroll = GUILayout.BeginScrollView(parent.scroll);

            EditorGUIUtility.SetIconSize(new Vector2(16, 16));

            List<Element> children = GetChildren(tree, parent);

            Rect selectedRect = new Rect();


            // Iterate through the children
            for (int i = 0; i < children.Count; i++)
            {
                Element e = children[i];
                Rect r = GUILayoutUtility.GetRect(16, 20, GUILayout.ExpandWidth(true));

                // Select the element the mouse cursor is over.
                // Only do it on mouse move - keyboard controls are allowed to overwrite this until the next time the mouse moves.
                if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDown)
                {
                    if (parent.selectedIndex != i && r.Contains(Event.current.mousePosition))
                    {
                        parent.selectedIndex = i;
                        Repaint();
                    }
                }

                bool selected = false;
                // Handle selected item
                if (i == parent.selectedIndex)
                {
                    selected = true;
                    selectedRect = r;
                }

                // Draw element
                if (Event.current.type == EventType.Repaint)
                {
                    GUIStyle labelStyle = (e is GroupElement) ? s_Styles.groupButton : s_Styles.componentButton;
                    labelStyle.Draw(r, e.content, false, false, selected, selected);
                    if ((e is GroupElement))
                    {
                        Rect arrowRect = new Rect(r.x + r.width - 13, r.y + 4, 13, 13);
                        s_Styles.rightArrow.Draw(arrowRect, false, false, false, false);
                    }
                }
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    parent.selectedIndex = i;
                    GoToChild(e, true);
                }
            }

            EditorGUIUtility.SetIconSize(Vector2.zero);

            GUILayout.EndScrollView();

            // Scroll to show selected
            if (m_ScrollToSelected && Event.current.type == EventType.Repaint)
            {
                m_ScrollToSelected = false;
                Rect scrollRect = GUILayoutUtility.GetLastRect();
                if (selectedRect.yMax - scrollRect.height > parent.scroll.y)
                {
                    parent.scroll.y = selectedRect.yMax - scrollRect.height;
                    Repaint();
                }
                if (selectedRect.y < parent.scroll.y)
                {
                    parent.scroll.y = selectedRect.y;
                    Repaint();
                }
            }
        }

        private List<Element> GetChildren(Element[] tree, Element parent)
        {
            List<Element> children = new List<Element>();
            int level = -1;
            int i = 0;
            for (i = 0; i < tree.Length; i++)
            {
                if (tree[i] == parent)
                {
                    level = parent.level + 1;
                    i++;
                    break;
                }
            }
            if (level == -1)
                return children;

            for (; i < tree.Length; i++)
            {
                Element e = tree[i];

                if (e.level < level)
                    break;
                if (e.level > level && !hasSearch)
                    continue;

                children.Add(e);
            }

            return children;
        }
    }

    public struct DisabledScope : IDisposable
    {
        private static Stack<bool> s_EnabledStack = new Stack<bool>();
        bool m_Disposed;

        public DisabledScope(bool disabled)
        {
            m_Disposed = false;

            s_EnabledStack.Push(GUI.enabled);
            GUI.enabled &= !disabled;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            if (s_EnabledStack.Count > 0)
                GUI.enabled = s_EnabledStack.Pop();
        }
    }
}
