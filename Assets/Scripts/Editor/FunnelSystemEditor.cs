using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FunnelSystem))]
public class FunnelSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 繪製原始的 Inspector
        DrawDefaultInspector();

        // 獲取 FunnelSystem 實例
        FunnelSystem funnelSystem = (FunnelSystem)target;

        // 添加分隔線
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Funnel 控制", EditorStyles.boldLabel);

        // Enable/Disable 按鈕
        GUI.backgroundColor = funnelSystem.enableAction ? Color.green : Color.red;
        if (GUILayout.Button(funnelSystem.enableAction ? "Disable Funnel" : "Enable Funnel"))
        {
            funnelSystem.SetEnableAction(!funnelSystem.enableAction);
        }

        // 重置按鈕顏色
        GUI.backgroundColor = Color.white;

        EditorGUILayout.BeginHorizontal();

        // Attack 按鈕
        GUI.enabled = funnelSystem.enableAction;
        if (GUILayout.Button("Attack"))
        {
            funnelSystem.Attack();
        }

        // StandBy 按鈕
        if (GUILayout.Button("StandBy"))
        {
            funnelSystem.StandBy();
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }
} 