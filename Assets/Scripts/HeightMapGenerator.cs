using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightMapGenerator : MonoBehaviour
{
	public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCentre)
	{
		float[,] values = Noise.GenerateNoiseMap(width, height, settings.noiseSettings, sampleCentre);

		AnimationCurve heightCurve_threadSafe = new AnimationCurve(settings.heightCurve.keys);

		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		// Loop through all the values
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				values[i, j] *= heightCurve_threadSafe.Evaluate(values[i, j]) * settings.heightMultiplier;

				if (values[i, j] > maxValue)
				{
					maxValue = values[i, j];
				}
				if (values[i, j] < minValue)
				{
					minValue = values[i, j];
				}

			}
		}

		return new HeightMap(values, minValue, maxValue);
	}
}

public struct HeightMap
{
	// Readonly declaration used because structs are unmuteable, meaning that the values of the variables can't be changed
	public readonly float[,] values;
	public readonly float minValues;
	public readonly float maxValues;

	// Constructor for MapData, passing through heightMap and colourMap
	public HeightMap(float[,] values, float minValues, float maxValues)
	{
		this.values = values;
		this.minValues = minValues;
		this.maxValues = maxValues;
	}
}