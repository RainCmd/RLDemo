using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(CircleIcon))]
public class CircleIconEditor : ImageEditor
{
    private SerializedProperty grayscale;
    protected override void OnEnable()
    {
        base.OnEnable();
        grayscale = serializedObject.FindProperty("grayscale");
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(grayscale);
        serializedObject.ApplyModifiedProperties();
        base.OnInspectorGUI();
    }
}
