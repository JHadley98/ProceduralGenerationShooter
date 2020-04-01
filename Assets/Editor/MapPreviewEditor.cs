using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapPreview))]
public class MapPreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Reference MapPreview class
        MapPreview mapPreview = (MapPreview)target;

        if (DrawDefaultInspector())
        {
            // If autoUpate is ticked then draw map automatically
            if (mapPreview.autoUpdate)
            {
                mapPreview.DrawMapInEditor();
            }
        }

        // If "Generate" button is pressed draw map
        if (GUILayout.Button("Generate"))
        {
            mapPreview.DrawMapInEditor();
        }
    }
}