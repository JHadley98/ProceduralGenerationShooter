using UnityEngine;
using System.Collections;

/* This class is used to store the data for the noiseMap */

[CreateAssetMenu()]
public class HeightMapSettings : UpdatableData
{
	public NoiseSettings noiseSettings;

	public bool useFalloff;

	public float heightMultiplier;
	public AnimationCurve heightCurve;

	// Minimum height of terrain
	public float minHeight
	{
		get
		{
			return heightMultiplier * heightCurve.Evaluate(0);
		}
	}

	// Maximum height of terrain
	public float maxHeight
	{
		get
		{
			return heightMultiplier * heightCurve.Evaluate(1);
		}
	}

#if UNITY_EDITOR

	protected override void OnValidate()
	{
		noiseSettings.ValidateValues();
		base.OnValidate();
	}
#endif

}