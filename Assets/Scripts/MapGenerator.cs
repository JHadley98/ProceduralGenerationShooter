using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{

	public enum DrawMode { NoiseMap, ColourMap };
	public DrawMode drawMode;

	public int mapWidth;
	public int mapHeight;
	public float noiseScale;

	public int octaves;
	[Range(0, 1)]
	public float persistance;
	public float lacunarity;

	public int seed;
	public Vector2 offset;

	public bool autoUpdate;

	public TerrainType[] terrainTypes;

    public Terrain t;

   

	public void GenerateMap()
	{
		// Call 2d noiseMap from Noise class, passing across all the included parameters.
		float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistance, lacunarity, offset);               
        t.terrainData.SetHeights(0, 0, noiseMap);

        // 1D colourMap array to save all colours used by the terrainTypes
        Color[] colourMap = new Color[mapWidth * mapHeight];
		for (int y = 0; y < mapHeight; y++)
		{
			for (int x = 0; x < mapWidth; x++)
			{
				float currentHeight = noiseMap[x, y];
				for (int i = 0; i < terrainTypes.Length; i++)
				{
					if (currentHeight <= terrainTypes[i].height)
					{
						// Save colour for current point
						colourMap[y * mapWidth + x] = terrainTypes[i].colour;
							break;
					}
				}
			}
		}

		// Reference to MapDisplay class
		MapDisplay display = FindObjectOfType<MapDisplay>();

        // If drawMode is equal to noisMap draw noiseMap
		if (drawMode == DrawMode.NoiseMap)
		{
            // Display noiseMap
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));            

		}
        // Else if DrawMode is equal to ColourMap, use ColourMap
		else if (drawMode == DrawMode.ColourMap)
		{
            display.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, mapWidth, mapHeight));
		}
	}

	void OnValidate()
	{
		// If statements to clamp all the values to 1 if the value becomes less than one.
		if (mapWidth < 1)
		{
			mapWidth = 1;
		}
		if (mapHeight < 1)
		{
			mapHeight = 1;
		}
		if (lacunarity < 1)
		{
			lacunarity = 1;
		}
		// If statement to clamp octaves to 0 is the values become less than 0.
		if (octaves < 0)
		{
			octaves = 0;
		}
	}
}

[System.Serializable]
public struct TerrainType
{
	public string name;
	public float height;
	public Color colour;

}