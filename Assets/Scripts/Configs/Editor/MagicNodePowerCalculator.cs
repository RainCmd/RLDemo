using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal class MagicNodePowerCalculator : EditorWindow
{
    struct NodeInfo
    {
        public float cd;
        public float cost;
        public MagicNodeType type;
        public int multiple;//重复触发次数
        public int sequence;//顺序触发次数
        public float dmg;
        public bool areaDmg;
        public float dmgRadious;
        public int number;//使用次数
        public float duration;//持续时间
        public float Power
        {
            get
            {
                var result = 1f;
                result *= CostScore(cd, .1f);
                result *= CostScore(cost / 10, 0);
                if (type == MagicNodeType.Alter)
                {
                    if (multiple > 1) result *= 1 + Mathf.Log(Mathf.Pow(multiple, 3));
                    if (sequence > 1) result *= 1 + Mathf.Log(Mathf.Pow(sequence, 2));
                    if (duration > 0) result *= Mathf.Sqrt(duration + 1);
                    else result *= 1 / (1 - duration);
                }
                else
                {
                    if (multiple > 0) result *= 1 + Mathf.Log(Mathf.Pow(multiple + 1, 3));
                    if (sequence > 0) result *= 1 + Mathf.Log(Mathf.Pow(sequence + 1, 2));
                    result *= Mathf.Sqrt(duration * .3f + 1);
                }
                if (dmg > 0) result *= Mathf.Sqrt(dmg + 1);
                else result *= Mathf.Pow(1 - dmg, .3f);
                if (areaDmg) result *= dmgRadious;
                if (number > 0) result *= 1 - 1 / (1 + Mathf.Log(number + 1));
                return result;
            }
        }
        private static float CostScore(float value, float balance)
        {
            var result = Mathf.Pow(.5f, value) + 1 - Mathf.Pow(.5f, balance);
            if (value < 0) result += .5f;
            return result;
        }
        public static readonly NodeInfo Default = new NodeInfo(0.1f, 3, MagicNodeType.Missile, 0, 0, 10, false, 1, 0, 3);

        public NodeInfo(float cd, float cost, MagicNodeType type, int multiple, int sequence, float dmg, bool areaDmg, float dmgRadious, int number, float duration)
        {
            this.cd = cd;
            this.cost = cost;
            this.type = type;
            this.multiple = multiple;
            this.sequence = sequence;
            this.dmg = dmg;
            this.areaDmg = areaDmg;
            this.dmgRadious = dmgRadious;
            this.number = number;
            this.duration = duration;
        }
    }
    private NodeInfo info = NodeInfo.Default;
    private void OnGUI()
    {
        info.cd = EditorGUILayout.FloatField("冷却", info.cd);
        info.cost = EditorGUILayout.FloatField("魔法消耗", info.cost);
        info.type = (MagicNodeType)EditorGUILayout.EnumPopup("节点类型", info.type);
        info.multiple = EditorGUILayout.IntField("重复触发次数", info.multiple);
        info.sequence = EditorGUILayout.IntField("顺序触发次数", info.sequence);
        info.dmg = EditorGUILayout.FloatField("伤害", info.dmg);
        info.areaDmg = EditorGUILayout.Toggle("范围伤害", info.areaDmg);
        if (info.areaDmg)
        {
            EditorGUI.indentLevel++;
            info.dmgRadious = EditorGUILayout.FloatField("伤害半径", info.dmgRadious);
            EditorGUI.indentLevel--;
        }
        info.number = EditorGUILayout.IntField("可用次数(0表示无限制)", info.number);
        info.duration = EditorGUILayout.FloatField("持续时间", info.duration);
        EditorGUILayout.Space();
        EditorGUILayout.SelectableLabel("节点等级为:" + info.Power);
        if (GUILayout.Button("重置")) info = NodeInfo.Default;
    }
    [MenuItem("Window/魔法节点等级计算器")]
    private static void ShowWindow()
    {
        var window = GetWindow<MagicNodePowerCalculator>();
        window.titleContent = new GUIContent("节点等级计算器", EditorGUIUtility.IconContent("TransformTool").image);
        window.ShowUtility();
    }
}