using System.Collections;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Reflection;

public class LogicConfigEditor : EditorWindow
{
    private Type type;
    private IList list;
    private string path;
    private HashSet<int> fold = new HashSet<int>();
    private void Init(Type type, IList list, string path)
    {
        this.type = type;
        this.list = list;
        this.path = path;
        fold.Clear();
    }
    private object Draw(string label, object value, Type type)
    {
        if (type == typeof(int))
        {
            if (value == null) value = 0;
            return EditorGUILayout.IntField(label, (int)value);
        }
        if (type == typeof(Real))
        {
            if (value == null) value = new Real();
            return (Real)(float)EditorGUILayout.FloatField(label, (float)(Real)value);
        }
        if (type == typeof(string))
        {
            if (value == null) value = "";
            return EditorGUILayout.TextField(label, (string)value);
        }
        if (type == typeof(long))
        {
            if (value == null) value = 0L;
            return EditorGUILayout.LongField(label, (long)value);
        }
        if (type.IsEnum)
        {
            if (value == null) value = type.GetEnumValues().GetValue(0);
            return EditorGUILayout.EnumFlagsField(label, (Enum)value);
        }
        if (label != null) EditorGUILayout.LabelField(label);
        if (value == null) return Activator.CreateInstance(type);
        EditorGUI.indentLevel++;
        if (type.IsSubclassOf(typeof(Array)))
        {
            var arr = (Array)value;
            var cnt = EditorGUILayout.IntField("数量", arr.Length);
            if (cnt != arr.Length)
            {
                var narr = Array.CreateInstance(type.GetElementType(), cnt);
                Array.Copy(arr, narr, Math.Min(arr.Length, narr.Length));
                arr = narr;
                value = narr;
            }
            for (int i = 0; i < arr.Length; i++)
            {
                arr.SetValue(Draw(i.ToString(), arr.GetValue(i), type.GetElementType()), i);
            }
        }
        else
        {
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.FieldType == typeof(long) && field.GetCustomAttribute<ConfigIdAttribute>() != null)
                {
                    field.SetValue(value, (long)ConfigIdAttributeEditor.Draw(EditorGUILayout.GetControlRect(), field.Name, (ulong)(long)field.GetValue(value)));
                }
                else
                {
                    field.SetValue(value, Draw(field.Name, field.GetValue(value), field.FieldType));
                }
            }
        }
        EditorGUI.indentLevel--;
        return value;
    }
    private Vector2 scroll;
    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(48)))
        {
            LogicConfig.Save(path, list);
        }
        if (GUILayout.Button(path, EditorStyles.toolbarButton, GUILayout.ExpandWidth(true)))
        {
            var cfg = AssetDatabase.LoadAssetAtPath("Assets/StreamingAssets" + path, typeof(UnityEngine.Object));
            if (cfg != null) EditorGUIUtility.PingObject(cfg);
        }
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+"))
        {
            list.Add(Activator.CreateInstance(type));
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndHorizontal();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < list.Count; i++)
        {
            var f = fold.Contains(i);
            EditorGUILayout.BeginHorizontal();
            var r = EditorGUILayout.Foldout(f, i.ToString() + "  " + type.Name);
            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                list.RemoveAt(i);
                continue;
            }
            EditorGUILayout.EndHorizontal();
            if (r)
            {
                if (!f) fold.Add(i);
                EditorGUI.indentLevel++;
                Draw(null, list[i], type);
                EditorGUI.indentLevel--;
            }
            else if (f) fold.Remove(i);
        }
        EditorGUILayout.EndScrollView();
    }
    private void OnDestroy()
    {
        if (list != null) LogicConfig.Save(path, list);
    }
    private static void ShowWindow<T>(string path)
    {
        if (!File.Exists(Application.streamingAssetsPath + path))
        {
            LogicConfig.Save(path, new List<T>());
            AssetDatabase.Refresh();
        }
        var window = CreateInstance<LogicConfigEditor>();
        window.titleContent = new GUIContent("逻辑配置", EditorGUIUtility.IconContent("MetaFile Icon").image);
        window.type = typeof(T);
        window.list = LogicConfig.Load(path);
        window.path = path;
        window.Show();
    }
    [MenuItem("配置文件/逻辑配置/魔法节点")]
    private static void ShowMagicNode()
    {
        ShowWindow<ConfigMagicNode>(ConfigMagicNode.Path);
    }
    [MenuItem("配置文件/逻辑配置/实体")]
    private static void ShowEntity()
    {
        ShowWindow<ConfigEntity>(ConfigEntity.Path);
    }
    [MenuItem("配置文件/逻辑配置/单位")]
    private static void ShowUnit()
    {
        ShowWindow<ConfigUnit>(ConfigUnit.Path);
    }
}
