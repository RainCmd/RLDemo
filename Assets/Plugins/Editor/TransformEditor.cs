using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace Plugins
{
    [CustomEditor(typeof(Transform))]
    [CanEditMultipleObjects]
    internal class TransformEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var ft = targets[0] as Transform;
            var lp = ft.localPosition;
            var le = GetLocalEulerAngles(ft);
            var ls = ft.localScale;
            var pdf = new bool[3];
            var edf = new bool[3];
            var sdf = new bool[3];
            for (int i = 1; i < targets.Length; i++)
            {
                var t = targets[i] as Transform;
                SetF(lp, t.localPosition, pdf);
                SetF(le, GetLocalEulerAngles(ft), edf);
                SetF(ls, t.localScale, sdf);
            }
            Draw(ref lp, ref le, ref ls, pdf, edf, sdf);
            for (int i = 0; i < targets.Length; i++)
            {
                var t = targets[i] as Transform;
                t.localPosition = SetT(lp, t.localPosition, pdf);
                SetLocalEulerAngles(t, SetT(le, GetLocalEulerAngles(t), edf));
                t.localScale = SetT(ls, t.localScale, sdf);
            }
            Undo.RecordObjects(targets, "TransformEditor");
        }
        private static void SetF(Vector3 s, Vector3 t, bool[] df)
        {
            df[0] |= s.x != t.x;
            df[1] |= s.y != t.y;
            df[2] |= s.z != t.z;
        }
        private static Vector3 SetT(Vector3 s, Vector3 t, bool[] df)
        {
            if (!df[0])
            {
                t.x = s.x;
            }
            if (!df[1])
            {
                t.y = s.y;
            }
            if (!df[2])
            {
                t.z = s.z;
            }
            return t;
        }
        private static bool Draw(ref Vector3 lp, ref Vector3 le, ref Vector3 ls, bool[] pdf, bool[] edf, bool[] sdf)
        {
            bool flag = false;
            EditorGUIUtility.labelWidth = 16;
            flag |= DrawVF("P", ref lp, Vector3.zero, pdf);
            flag |= DrawVF("R", ref le, Vector3.zero, edf);
            flag |= DrawVF("S", ref ls, Vector3.one, sdf);
            return flag;
        }
        private static bool DrawVF(string s, ref Vector3 v, Vector3 dv, bool[] df)
        {
            var flag = false;
            EditorGUILayout.BeginHorizontal();
            if (GUI.Button(EditorGUILayout.GetControlRect(GUILayout.Width(EditorGUIUtility.singleLineHeight + 2)), s))
            {
                EditorGUI.FocusTextInControl("");
                v = dv;
                df[0] = df[1] = df[2] = false;
            }
            flag |= DrawFF("X", ref v.x, ref df[0]);
            flag |= DrawFF("Y", ref v.y, ref df[1]);
            flag |= DrawFF("Z", ref v.z, ref df[2]);
            EditorGUILayout.EndHorizontal();
            return flag;
        }
        private static bool DrawFF(string s, ref float f, ref bool b)
        {
            var lf = f;
            EditorGUI.showMixedValue = b;
            f = EditorGUILayout.FloatField(s, f);
            EditorGUI.showMixedValue = false;
            b &= lf == f;
            return !b;
        }
        private static Vector3 GetLocalEulerAngles(Transform transform)
        {
            return (Vector3)transform_GetLocalEulerAngles.Invoke(transform, new object[] { GetRotationOrder(transform) });

        }
        private static void SetLocalEulerAngles(Transform transform, Vector3 euler)
        {
            transform_SetLocalEulerAngles.Invoke(transform, new object[] { euler, GetRotationOrder(transform) });
        }
        private static object GetRotationOrder(Transform transform)
        {
            return transform_get_rotationOrder.Invoke(transform, EmptyObjects);
        }
        private static readonly object[] EmptyObjects = new object[0];
        private static readonly MethodInfo transform_SetLocalEulerAngles = typeof(Transform).GetMethod("SetLocalEulerAngles", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo transform_GetLocalEulerAngles = typeof(Transform).GetMethod("GetLocalEulerAngles", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo transform_get_rotationOrder = typeof(Transform).GetMethod("get_rotationOrder", BindingFlags.Instance | BindingFlags.NonPublic);
    }
}
