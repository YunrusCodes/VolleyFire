using UnityEngine;
using UnityEditor;
using VolleyFire.Funnel;

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

    void OnSceneGUI()
    {
        FunnelSystem funnelSystem = (FunnelSystem)target;
        Vector3 center = funnelSystem.WorldCenterPoint;
        float zOffset = funnelSystem.WorldZOffset;

        // 繪製三個平面的框線
        DrawPlaneFrame(center + Vector3.forward * zOffset, funnelSystem.WorldXRange, funnelSystem.WorldYRange, Color.green);  // 前平面
        DrawPlaneFrame(center, funnelSystem.WorldXRange, funnelSystem.WorldYRange, Color.yellow);  // 中平面
        DrawPlaneFrame(center - Vector3.forward * zOffset, funnelSystem.WorldXRange, funnelSystem.WorldYRange, Color.red);  // 後平面

        // 繪製連接線
        Vector3 apex = funnelSystem.CalculatePyramidApex();
        Handles.color = Color.cyan;

        // 繪製前平面到頂點的連接線
        DrawConnectingLines(center + Vector3.forward * zOffset, funnelSystem.WorldXRange, funnelSystem.WorldYRange, apex);

        // 繪製後平面到頂點的連接線
        DrawConnectingLines(center - Vector3.forward * zOffset, funnelSystem.WorldXRange, funnelSystem.WorldYRange, apex);
    }

    private void DrawPlaneFrame(Vector3 center, Vector2 xRange, Vector2 yRange, Color color)
    {
        Handles.color = color;

        Vector3 p1 = center + new Vector3(xRange.x, yRange.x, 0);
        Vector3 p2 = center + new Vector3(xRange.y, yRange.x, 0);
        Vector3 p3 = center + new Vector3(xRange.y, yRange.y, 0);
        Vector3 p4 = center + new Vector3(xRange.x, yRange.y, 0);

        Handles.DrawLine(p1, p2);
        Handles.DrawLine(p2, p3);
        Handles.DrawLine(p3, p4);
        Handles.DrawLine(p4, p1);
    }

    private void DrawConnectingLines(Vector3 center, Vector2 xRange, Vector2 yRange, Vector3 apex)
    {
        Vector3 p1 = center + new Vector3(xRange.x, yRange.x, 0);
        Vector3 p2 = center + new Vector3(xRange.y, yRange.x, 0);
        Vector3 p3 = center + new Vector3(xRange.y, yRange.y, 0);
        Vector3 p4 = center + new Vector3(xRange.x, yRange.y, 0);

        Handles.DrawLine(p1, apex);
        Handles.DrawLine(p2, apex);
        Handles.DrawLine(p3, apex);
        Handles.DrawLine(p4, apex);
    }
} 