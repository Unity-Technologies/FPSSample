using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnumBitFieldAttribute : PropertyAttribute
{
	public EnumBitFieldAttribute(Type enumType)
	{
		this.enumType = enumType;
	}

	public Type enumType;
}
 
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(EnumBitFieldAttribute))]
public class EnumBitFieldAttributeDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		var enumFlagsAttribute = attribute as EnumBitFieldAttribute;
		var names = Enum.GetNames(enumFlagsAttribute.enumType);
		property.intValue = EditorGUI.MaskField( position, label, property.intValue, names );
	}
}
#endif
