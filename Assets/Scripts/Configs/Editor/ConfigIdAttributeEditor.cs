using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ConfigIdAttribute))]
public class ConfigIdAttributeEditor : PropertyDrawer
{
    static GUIContent tipLable;
    static GUIStyle tipStyle;
    static void Init()
    {
        if (tipLable != null) return;
        tipLable = new GUIContent();
        tipStyle = new GUIStyle(EditorStyles.label);
        tipStyle.richText = true;
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var result = base.GetPropertyHeight(property, label);
        if (property.type != "long") result *= 2;
        return result;
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.type == "long")
        {
            property.longValue = (long)Draw(position, label, (ulong)property.longValue);
        }
        else
        {
            position.height /= 2;
            Init();
            tipLable.text = "<color=#ff0000>ConfigId属性仅对long类型生效</color>";
            EditorGUI.LabelField(position, tipLable, tipStyle);
            position.y += position.height;
            EditorGUI.PropertyField(position, property, label);
        }
    }
    public static ulong Draw(Rect position, GUIContent label, ulong value)
    {
        var txt = "";
        var src = value;
        while (value > 0)
        {
            txt = (char)(value & 0xff) + txt;
            value >>= 8;
        }
        txt = EditorGUI.TextField(position, label, txt);
        if (txt.Length > 8) return src;
        for (int i = 0; i < txt.Length; i++)
            if (txt[i] > 255) return src;
            else
            {
                value <<= 8;
                value += txt[i];
            }
        return value;
    }
    public static ulong Draw(Rect position, string label, ulong value)
    {
        Init();
        tipLable.text = label;
        return Draw(position, tipLable, value);
    }
}
