using UnityEngine;
using System.Collections;

/* This class is used to store all the data that needs updating in the editor */

public class UpdatableData : ScriptableObject
{

	public event System.Action OnValuesUpdated;
	public bool autoUpdate;

#if UNITY_EDITOR

	protected virtual void OnValidate()
	{
		if (autoUpdate)
		{
			// Subscribe to callback
			UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
		}
	}

	public void NotifyOfUpdatedValues()
	{
		// Unsubscribe from callback
		UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
		if (OnValuesUpdated != null)
		{
			OnValuesUpdated();
		}
	}

#endif

}