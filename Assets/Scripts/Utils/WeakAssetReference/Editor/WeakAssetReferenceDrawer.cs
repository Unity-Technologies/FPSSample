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
        var assetType = assetTypeProperty != null ? assetTypeProperty.assetType : typeof(Object);

        var val0 = prop.FindPropertyRelative("val0");
        var val1 = prop.FindPropertyRelative("val1");
        var val2 = prop.FindPropertyRelative("val2");
        var val3 = prop.FindPropertyRelative("val3");

        var reference = new WeakAssetReference(val0.intValue, val1.intValue, val2.intValue, val3.intValue);

        var guidStr = "";
        Object obj = null;
        if (reference.IsSet())
        {
            guidStr = reference.GetGuidStr();
            var path = AssetDatabase.GUIDToAssetPath(guidStr);

            if(assetType == null)
                assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
            
            obj =  AssetDatabase.LoadAssetAtPath(path,assetType);
        }

        pos = EditorGUI.PrefixLabel(pos, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(label.text+"("+guidStr+")"));
        Object newObj = EditorGUI.ObjectField(pos, obj, assetType, false);

        if(newObj != obj)
        {
            var newRef = new WeakAssetReference();            
            if (newObj != null)
            {
                var path = AssetDatabase.GetAssetPath(newObj);
                newRef = new WeakAssetReference(AssetDatabase.AssetPathToGUID(path));
            }

            val0.intValue = newRef.val0;
            val1.intValue = newRef.val1;
            val2.intValue = newRef.val2;
            val3.intValue = newRef.val3;
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
