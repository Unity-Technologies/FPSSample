using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(WeakAssetReference))]
public class WeakAssetReferenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
    {
        // Figure out what asset types we allow. Default to all.
        AssetTypeAttribute assetTypeProperty = System.Attribute.GetCustomAttribute(fieldInfo, typeof(AssetTypeAttribute)) as AssetTypeAttribute;
        var assetType = assetTypeProperty != null ? assetTypeProperty.assetType : typeof(GameObject);

        SerializedProperty guid = prop.FindPropertyRelative("guid");

        string path = AssetDatabase.GUIDToAssetPath(guid.stringValue);

        Object obj =  AssetDatabase.LoadAssetAtPath(path,assetType);

        pos = EditorGUI.PrefixLabel(pos, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(label.text+"("+guid.stringValue+")"));
        Object newObj = EditorGUI.ObjectField(pos, obj, assetType, false);

        if(newObj != obj)
        {
            if (newObj != null)
            {
                path = AssetDatabase.GetAssetPath(newObj);
                guid.stringValue = AssetDatabase.AssetPathToGUID(path);
            }
            else
                guid.stringValue = "";
        }
    }
}

[CustomPropertyDrawer(typeof(WeakBase), true)]
public class WeakBaseDrawer : PropertyDrawer
{
    public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
    {
        // Figure out what asset types we allow. Default to all.
        var assetType = typeof(GameObject);
        var baseType = fieldInfo.FieldType.BaseType;
        if (baseType != null && baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(Weak<>))
            assetType = baseType.GetGenericArguments()[0];

        SerializedProperty guid = prop.FindPropertyRelative("guid");

        string path = AssetDatabase.GUIDToAssetPath(guid.stringValue);

        Object obj =  AssetDatabase.LoadAssetAtPath(path,assetType);

        pos = EditorGUI.PrefixLabel(pos, GUIUtility.GetControlID(FocusType.Passive), label);
        Object newObj = EditorGUI.ObjectField(pos, obj, assetType, false);            
        if(newObj != obj)
        {
            if (newObj != null)
            {
                path = AssetDatabase.GetAssetPath(newObj);
                guid.stringValue = AssetDatabase.AssetPathToGUID(path);
            }
            else
            {
                guid.stringValue = "";
            }
            guid.serializedObject.ApplyModifiedProperties();
        }
    }
}
