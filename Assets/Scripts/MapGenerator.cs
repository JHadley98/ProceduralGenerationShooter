using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
	// Public variables:
	public enum DrawMode { NoiseMap, Mesh, FalloffMap };
	public DrawMode drawMode;

	// Data class references
	public MeshSettings meshSettings;
	public HeightMapSettings heightMapSettings;
	public TextureData textureData;

	public Material terrainMaterial;

	[Range(0, MeshSettings.numSupportedLODs - 1)]
	public int editorPreviewLOD;
	public bool autoUpdate;

	float[,] falloffMap;

	// MapThreadInfo queues for HeightMap and MeshData, used to thread through the HeightMap and MeshData of the generator
	Queue<MapThreadInfo<HeightMap>> heightMapThreadInfoQueue = new Queue<MapThreadInfo<HeightMap>>();
	Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	void Start()
	{
		textureData.ApplyToMaterial(terrainMaterial);
		textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
	}

	void OnValuesUpdated()
	{
		if (!Application.isPlaying)
		{
			DrawMapInEditor();
		}
	}

	// Update texture values function
	void OnTextureValuesUpdated()
	{
		// Apply material to terrain material from texture data script
		textureData.ApplyToMaterial(terrainMaterial);
	}


	public void DrawMapInEditor()
	{
		textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
		HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero);

		// Reference to MapDisplay class
		MapDisplay display = FindObjectOfType<MapDisplay>();
		// If drawMode equals a set DrawMode display that mode
		if (drawMode == DrawMode.NoiseMap)
		{
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap.values));
		}
		else if (drawMode == DrawMode.Mesh)
		{
			display.DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings,editorPreviewLOD));
		}
		else if (drawMode == DrawMode.FalloffMap)
		{
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVertsPerLine)));
		}
	}

	// Request map data function, pass through centre vector2 to centrialise the position of the map data and pass callback function to pass on the action function caling HeightMap
	public void RequestHeightMap(Vector2 centre, Action<HeightMap> callback)
	{
		// Create threadstart, this represent the heightMapThread with the callback parameter
		ThreadStart threadStart = delegate
		{
			HeightMapThread(centre, callback);
		};
		// Start thread 
		new Thread(threadStart).Start();
	}

	// Thread function for map data, pass through a Vector2 centre to centre the map position, and pass callback function to pass on the action function calling HeightMap
	void HeightMapThread(Vector2 centre, Action<HeightMap> callback)
	{
		// Execute GenerateHeightMap method within the HeightMapThread
		HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, centre);
		// Lock allows it so that when one thread reaches this point, while it's executing this code no other thread can execute as well, it will have to wait its turn
		lock (heightMapThreadInfoQueue)
		{
			heightMapThreadInfoQueue.Enqueue(new MapThreadInfo<HeightMap>(callback, heightMap));
		}
	}

	// Request Mesh Data function passing through map date, level of detail (lod) and callback data
	public void RequestMeshData(HeightMap heightMap, int lod, Action<MeshData> callback)
	{
		// Create threadstart, this represent the meshDataThread with the callback parameter
		ThreadStart threadStart = delegate
		{
			MeshDataThread(heightMap, lod, callback);
		};
		// Start thread within method
		new Thread(threadStart).Start();
	}

	// Thread mesh data function, pass through map data, level of detail and callback the information passed by the action
	void MeshDataThread(HeightMap heightMap, int lod, Action<MeshData> callback)
	{
		// Pass heightMap, meshHeightMultipler, meshHeightCurve and levelOfDetail to the MeshGenerator, to be threaded through meshData
		MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod);
		// Lock allows it so that when one thread reaches this point, while it's executing this code no other thread can execute as well, it will have to wait its turn
		lock (meshDataThreadInfoQueue)
		{
			// Create new info queue passing callBack and meshData
			meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
		}
	}

	void Update()
	{
		// If heightMapThreadInfoQueue has something in it then loop through all the queue elements
		if (heightMapThreadInfoQueue.Count > 0)
		{
			for (int i = 0; i < heightMapThreadInfoQueue.Count; i++)
			{
				// Thread info is equal to the next thing in the queue
				MapThreadInfo<HeightMap> threadInfo = heightMapThreadInfoQueue.Dequeue();
				// Pass in thread info parameter to callback
				threadInfo.callback(threadInfo.parameter);
			}
		}

		// If meshDataThreadInfoQueue has something in it then loop through all the queue elements
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

	void OnValidate()
	{
		// Data is not null then, set the data value to be OnValuesUpdated, however, doing -= first so that there is no overlap of updated values
		if (meshSettings != null)
		{
			meshSettings.OnValuesUpdated -= OnValuesUpdated;
			meshSettings.OnValuesUpdated += OnValuesUpdated;
		}
		if (heightMapSettings != null)
		{
			heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
			heightMapSettings.OnValuesUpdated += OnValuesUpdated;
		}
		if (textureData != null)
		{
			textureData.OnValuesUpdated -= OnTextureValuesUpdated;
			textureData.OnValuesUpdated += OnTextureValuesUpdated;
		}

	}

	struct MapThreadInfo<T>
	{
		public readonly Action<T> callback;
		public readonly T parameter;

		public MapThreadInfo(Action<T> callback, T parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
		}
	}
}