using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Reference MapGenerator class
        MapGenerator map = (MapGenerator)target;

        if (DrawDefaultInspector())
        {
            // If autoUpate is ticked then draw map automatically
            if (map.autoUpdate)
            {
                map.DrawMapInEditor();
            }
        }

        // If "Generate" button is pressed draw map
        if (GUILayout.Button("Generate"))
        {
            map.DrawMapInEditor();
        }
    }
}
