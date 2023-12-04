using Plugins;
using UnityEditor;
using UnityEngine;
using RainLanguage;

[CustomPropertyDrawer(typeof(Real))]
public class RealEditor : CustomPropertyEditor
{
    protected override object OnGUI(object value, Rect position, GUIContent label)
    {
        if (value is Real real) return (Real)EditorGUI.DoubleField(position, label, real);
        return value;
    }
}
