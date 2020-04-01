using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator
{
	// Generate height map method
	public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCentre)
	{
		// 2D float array used for the values for the noise map
		float[,] values = Noise.GenerateNoiseMap(width, height, settings.noiseSettings, sampleCentre);

		AnimationCurve heightCurve_threadSafe = new AnimationCurve(settings.heightCurve.keys);

		// Declare min and max value possible for height map
		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		// Loop through all the values
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				values[i, j] *= heightCurve_threadSafe.Evaluate(values[i, j]) * settings.heightMultiplier;

				// If values is more than or less than either maxValue or minValue set both maxValue and minValue to equal values
				if (values[i, j] > maxValue || values[i, j] < minValue)
				{
					maxValue = values[i, j];
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
	public readonly float minValue;
	public readonly float maxValue;

	// Constructor for MapData, passing through heightMap and colourMap
	public HeightMap(float[,] values, float minValue, float maxValue)
	{
		this.values = values;
		this.minValue = minValue;
		this.maxValue = maxValue;
	}
}