using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UpdatableData), true)]
public class UpdatableDataEditor : Editor
{

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		// Reference UpdatableData class
		UpdatableData data = (UpdatableData)target;

		// If "Update" button is pressed update values
		if (GUILayout.Button("Update"))
		{
			data.NotifyOfUpdatedValues();
			EditorUtility.SetDirty(target);
		}
	}
}