using UnityEngine;
using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(StateBar), true)]
[CanEditMultipleObjects]
public class StateBarEditor : ImageEditor
{
    private SerializedProperty state;
    protected override void OnEnable()
    {
        base.OnEnable();
        state = serializedObject.FindProperty("state");
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(state, true);
        serializedObject.ApplyModifiedProperties();
        base.OnInspectorGUI();
    }
}
