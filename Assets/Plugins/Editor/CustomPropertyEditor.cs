using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Reflection;
using System;

namespace Plugins
{
    public abstract class CustomPropertyEditor : PropertyDrawer
    {
        private const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Regex regex = new Regex(@"(\d+)");
        /// <summary>
        /// 绘制对象回调
        /// </summary>
        /// <param name="o"></param>
        /// <param name="position"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public delegate object Replace(object o, Rect position, GUIContent label);
        private static readonly Queue<string> paths = new Queue<string>();
        private static object DrawObject(object o, Queue<string> path, Rect position, GUIContent label, Replace replace)
        {
            if (o != null)
            {
                var fieldName = path.Dequeue();
                var type = o.GetType();
                var info = type.GetField(fieldName, bindingFlags);
                while (info == null)
                {
                    type = type.BaseType;
                    info = type.GetField(fieldName, bindingFlags);
                }
                var v = info.GetValue(o);
                if (v is IList)
                {
                    path.Dequeue();
                    var i = int.Parse(regex.Match(path.Dequeue()).Value);
                    var list = v as IList;
                    if (i < list.Count)
                    {
                        if (path.Count > 0)
                        {
                            list[i] = DrawObject(list[i], path, position, label, replace);
                        }
                        else
                        {
                            list[i] = replace(list[i], position, label);
                        }
                    }
                }
                else if (path.Count > 0)
                {
                    info.SetValue(o, DrawObject(v, path, position, label, replace));
                }
                else
                {
                    info.SetValue(o, replace(v, position, label));
                }
            }
            return o;
        }
        /// <summary>
        /// 绘制对象
        /// </summary>
        /// <param name="o">需要绘制的对象</param>
        /// <param name="path">对象路径</param>
        /// <param name="position">绘制区域</param>
        /// <param name="label">标签</param>
        /// <param name="replace">回调</param>
        public static void DrawObject(object o, string path, Rect position, GUIContent label, Replace replace)
        {
            paths.Clear();
            foreach (var p in path.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries))
            {
                paths.Enqueue(p);
            }
            DrawObject(o, paths, position, label, replace);
        }
        /// <summary>
        /// 绘制属性
        /// </summary>
        /// <param name="property">属性</param>
        /// <param name="position">位置</param>
        /// <param name="label">标签</param>
        /// <param name="replace">回调</param>
        public static void DrawProperty(SerializedProperty property, Rect position, GUIContent label, Replace replace)
        {
            DrawObject(property.serializedObject.targetObject, property.propertyPath, position, label, replace);
        }
        public sealed override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawProperty(property, position, label, OnGUI);
        }
        protected abstract object OnGUI(object value, Rect position, GUIContent label);
    }
}