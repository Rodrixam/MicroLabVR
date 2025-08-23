using UnityEditor;
using UnityEngine;

namespace ModularLab.Scripts.Editor
{
    [CustomEditor(typeof(LabGenerator))]
    public class LabGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            LabGenerator generator = (LabGenerator)target;

            DrawDefaultInspector();
            if (GUILayout.Button("Generate Lab"))
            {
                generator.LoadLabScene();
            }
        }
    }
}
