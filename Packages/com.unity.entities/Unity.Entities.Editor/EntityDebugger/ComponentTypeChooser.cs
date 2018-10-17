using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Entities.Editor
{

    public delegate void CallbackAction();

    public class ComponentTypeChooser : EditorWindow
    {

        private static List<ComponentType> types;
        private static List<bool> typeSelections;

        private static CallbackAction callback;

        private static readonly Vector2 kDefaultSize = new Vector2(300f, 400f);

        public static void Open(Vector2 screenPosition, List<ComponentType> types, List<bool> typeSelections, CallbackAction callback)
        {
            ComponentTypeChooser.callback = callback;
            ComponentTypeChooser.types = types;
            ComponentTypeChooser.typeSelections = typeSelections;
            GetWindowWithRect<ComponentTypeChooser>(new Rect(screenPosition, kDefaultSize), true, "Choose Component", true);
        }

        private SearchField searchField;
        private ComponentTypeListView typeListView;

        private void OnEnable()
        {
            searchField = new SearchField();
            searchField.SetFocus();
            typeListView = new ComponentTypeListView(new TreeViewState(), types, typeSelections, ComponentFilterChanged);
        }

        public void ComponentFilterChanged()
        {
            callback();
        }

        private void OnGUI()
        {
            typeListView.searchString = searchField.OnGUI(typeListView.searchString, GUILayout.Height(20f), GUILayout.ExpandWidth(true));
            typeListView.OnGUI(GUIHelpers.GetExpandingRect());
        }

        private void OnLostFocus()
        {
            Close();
        }
    }
}
