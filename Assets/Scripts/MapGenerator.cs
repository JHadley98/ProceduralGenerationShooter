using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
	public enum DrawMode { NoiseMap, ColourMap, Mesh };
	public DrawMode drawMode;

    public const int mapChunkSize = 241;
    [Range(0, 6)]
    public int editorPreviewLOD;
    public float noiseScale;

    public int octaves;
	[Range(0, 1)]
	public float persistance;
	public float lacunarity;

	public int seed;
	public Vector2 offset;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public bool autoUpdate;

	public TerrainType[] terrainTypes;

    // MapThreadInfo queues for MapData and MeshData, used to thread through the MapData and MeshData of the generator
    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);

        // Reference to MapDisplay class
        MapDisplay display = FindObjectOfType<MapDisplay>();

        // If drawMode is equal to noisMap draw noiseMap
        if (drawMode == DrawMode.NoiseMap)
        {
            // Display noiseMap
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        // Else if DrawMode is equal to ColourMap, use ColourMap
        else if (drawMode == DrawMode.ColourMap)
        {
            // Draw colourMap
            display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD), TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
    }

    public void RequestMapData(Vector2 centre, Action<MapData> callback)
    {
        // Create threadstart, this represent the mapDataThread with the callback parameter
        ThreadStart threadStart = delegate
        {
            MapDataThread(centre, callback);
        };

        // Start thread within method
        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 centre, Action<MapData> callback)
    {
        // Execute GenerateMapData method within the MapDataThread
        MapData mapData = GenerateMapData(centre);
        // Lock allows it so that when one thread reaches this point, while it's executing this code no other thread can execute as well, it will have to wait its turn
        lock (mapDataThreadInfoQueue)
        {
            // Create new info queue passing callBack and mapData
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        // Create threadstart, this represent the meshDataThread with the callback parameter
        ThreadStart threadStart = delegate 
        {
            MeshDataThread(mapData, lod, callback);
        };

        // Start thread within method
        new Thread(threadStart).Start();
    }

    public void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        // Pass heightMap, meshHeightMultipler, meshHeightCurve and levelOfDetail to the MeshGenerator, to be threaded through meshData
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);

        // Lock allows it so that when one thread reaches this point, while it's executing this code no other thread can execute as well, it will have to wait its turn
        lock (meshDataThreadInfoQueue)
        {
            // Create new info queue passing callBack and meshData
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    void Update()
    {
        // If mapDataThreadInfoQueue has something in it then loop through all the queue elements
        if (mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                // Thread info is equal to the next thing in the queue
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                // Pass in thread info parameter to callback
                threadInfo.callback(threadInfo.parameter);
            }
        }

        // If mapMeshThreadInfoQueue has something in it then loop through all the queue elements
        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                // Thread info is equal to the next thing in the queue
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                // Pass in thread info parameter to callback
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    MapData GenerateMapData(Vector2 centre)
	{
		// Call 2d noiseMap from Noise class, passing across all the included parameters.
		float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, centre + offset);               

        // 1D colourMap array to save all colours used by the terrainTypes
        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
		for (int y = 0; y < mapChunkSize; y++)
		{
			for (int x = 0; x < mapChunkSize; x++)
			{
				float currentHeight = noiseMap[x, y];
				for (int i = 0; i < terrainTypes.Length; i++)
				{
					if (currentHeight <= terrainTypes[i].height)
					{
						// Save colour for current point
						colourMap[y * mapChunkSize + x] = terrainTypes[i].colour;
							break;
					}
				}
			}
		}

        return new MapData(noiseMap, colourMap);
	}

	void OnValidate()
	{
		// Clamp values to 1 if the value becomes less than one.
		if (lacunarity < 1)
		{
			lacunarity = 1;
		}
		// Clamp octaves to 0 if the values become less than 0.
		if (octaves < 0)
		{
			octaves = 0;
		}
	}

    // Store map data in Map Thread Info struct, setting it as a generic struct setting it as T
    struct MapThreadInfo<T>
    {
        // Readonly declaration used because structs are unmuteable, meaning that the values of the variables can't be changed
        public readonly Action<T> callback;
        public readonly T parameter;

        // Constructor for MapThreadInfo, passing through callback and T parameter
        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
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

public struct MapData
{
    // Readonly declaration used because structs are unmuteable, meaning that the values of the variables can't be changed
    public readonly float[,] heightMap;
    public readonly Color[] colourMap;
    
    // Constructor for MapData, passing through heightMap and colourMap
    public MapData(float[,] heightMap, Color[] colourMap)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}