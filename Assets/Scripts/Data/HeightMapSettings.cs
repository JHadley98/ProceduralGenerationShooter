using UnityEngine;

/* Class to store all the data for the height map settings
 * Storing the noise settings, falloff, height multipler and height curve 
 */
[CreateAssetMenu()]
public class HeightMapSettings : UpdatableData
{
	Terrain terrain;

	public NoiseSettings noiseSettings;

	public bool useFalloff;

	// Scales on the Y axis
	public float heightMultiplier;
	public AnimationCurve heightCurve;

	// Create accessor for both minimum and maximum height for the terrain
	public float minHeight
	{
		get
		{
			return heightMultiplier * heightCurve.Evaluate(0);
		}
	}

	public float maxHeight
	{
		get
		{
			return heightMultiplier * heightCurve.Evaluate(1);
		}
	}

	// Only compile the code below when in the unity editor
#if UNITY_EDITOR

	protected override void OnValidate()
	{
		noiseSettings.ValidateValues();
		base.OnValidate();
	}
#endif

}