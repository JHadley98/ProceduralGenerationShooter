using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{

	public enum NormaliseMode { Local, Global };

	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormaliseMode normaliseMode)
	{
		float[,] noiseMap = new float[mapWidth, mapHeight];

		// Psuedo Random Number Generator
		System.Random prng = new System.Random(seed);
		// Vector2 octaveOffsets array set to octaves
		Vector2[] octaveOffsets = new Vector2[octaves];

		float maxPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;

		// Loop through octaves
		for (int i = 0; i < octaves; i++)
		{
			float offsetX = prng.Next(-100000, 100000) + offset.x;
			float offsetY = prng.Next(-100000, 100000) - offset.y;
			octaveOffsets[i] = new Vector2(offsetX, offsetY);

			maxPossibleHeight += amplitude;

			// persistance value is in the range 0 to 1, so that decreases each octave
			amplitude *= persistance;
		}

		if (scale <= 0)
		{
			scale = 0.0001f;
		}
	
		// Set float values outside of for loop
		float maxLocalNoiseHeight = float.MinValue;
		float minLocalNoiseHeight = float.MaxValue;

		// Calculate the values of half the map width and height
		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;

		for (int y = 0; y < mapHeight; y++)
		{
			for (int x = 0; x < mapWidth; x++)
			{

				amplitude = 1;
				frequency = 1;
				float noiseHeight = 0;

				for (int i = 0; i < octaves; i++)
				{
					// Calculate and control noise scale
					float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
					float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;
					
					// Set perlinValue
					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
					// Apply perlinValue to noiseHeight and multiply by amplitude
					noiseHeight += perlinValue * amplitude;

					// persistance value is in the range 0 to 1, so that decreases each octave
					amplitude *= persistance;
					// frequency increases each octave, since lacunarity should be > 1
					frequency *= lacunarity;
				}

				// If noiseHeight is greater than the maxNoiseHeight set the maxNoiseHeight to equal noiseHeight
				if (noiseHeight > maxLocalNoiseHeight)
				{
					maxLocalNoiseHeight = noiseHeight;
				}
				// Else if noiseHeight is less than the minNoiseHeight set the minNoiseHeight to equal noiseHeight
				else if (noiseHeight < minLocalNoiseHeight)
				{
					minLocalNoiseHeight = noiseHeight;
				}

				// Set noiseMap x, y to equal noiseHeight
				noiseMap[x, y] = noiseHeight;
			}
		}

		// For loop to normalise noiseMap
		for (int y = 0; y < mapHeight; y++)
		{
			for (int x = 0; x < mapWidth; x++)
			{
                // If normaliseMode equals normal then the entire map can be generated at one knowing the min and max noiseheight values
				if(normaliseMode == NormaliseMode.Local)
				{
					noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
				}
                // Else generate the chunk by chunk by estimating the max possible height of the noiseMap
				else
				{
					float normalisedHeight = noiseMap[x, y] + 1 / (maxPossibleHeight);
                    // Clamp noiseMap to normalisedHeight on X axis, y to 0 and z axis to the max int value
                    noiseMap[x, y] = Mathf.Clamp(normalisedHeight, 0, int.MaxValue);
				}
			}
		}

		return noiseMap;
	}
}
