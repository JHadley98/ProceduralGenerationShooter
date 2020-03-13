using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Reference MapGenerator class
        MapGenerator mapGen = (MapGenerator)target;

        if (DrawDefaultInspector())
        {
            // If autoUpate is ticked then draw map automatically
            if (mapGen.autoUpdate)
            {
                mapGen.DrawMapInEditor();
            }
        }

        // If "Generate" button is pressed draw map
        if (GUILayout.Button("Generate"))
        {
            mapGen.DrawMapInEditor();
        }
    }
}