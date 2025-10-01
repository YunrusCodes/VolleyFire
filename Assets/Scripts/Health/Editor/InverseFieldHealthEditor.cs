using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(InverseFieldHealth))]
public class InverseFieldHealthEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        InverseFieldHealth script = (InverseFieldHealth)target;

        if (GUILayout.Button("Add 3 Seconds"))
        {
            script.SetCountdownTime(script.currentCountdown + 3f);
        }
    }
}
