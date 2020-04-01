using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TerrainGenerator : MonoBehaviour
{
	const float viewerMoveThresholdForChunkUpdate = 25f;
	const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

	public int colliderLODIndex;
	public LODInfo[] detailLevels;

	// Class references
	public MeshSettings meshSettings;
	public HeightMapSettings heightMapSettings;
	public TextureSettings textureSettings;

	public Transform viewer;
	public Material terrainMaterial;

	public Vector2 viewerPosition;
	Vector2 viewerPositionOld;
	
	float meshWorldSize;
	int chunksVisibleInViewDst;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

	void Start()
	{
		// At the start of the game apply the material to the terrain material asset and update mesh heights for the terrain.
		textureSettings.ApplyToMaterial(terrainMaterial);
		textureSettings.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

		// Set maxViewDst to be detailLevels length - 1
		float maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
		// Set chunksize
		meshWorldSize = meshSettings.meshWorldSize;
		// Set number of chunk visible in view distance
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);

		UpdateVisibleChunks();
	}

	void Update()
	{
		// Update viewer Position variable
		viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

		// Call every frame as long as the player has moved
		if (viewerPosition != viewerPositionOld)
		{
			// Loop through all the currently visible chunks
			foreach (TerrainChunk chunk in visibleTerrainChunks)
			{
				// Update collision mesh on the chunk if they are visible
				chunk.UpdateCollisionMesh();
			}
		}

		if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
		{
			viewerPositionOld = viewerPosition;
			UpdateVisibleChunks();
		}
	}

	void UpdateVisibleChunks()
	{
		HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
		// Loop through all the newly visible chunks starting backwards
		for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--)
		{
			alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
			// Update visible terrain chunks
			visibleTerrainChunks[i].UpdateTerrainChunk();
		}

		int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
		int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
		{
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
			{

				// Vector2 set viewChunkCoord to equal a new Vector2 set the X value to currentChunkCoordX + xOffset and set the Y value to currentChunkCoordY + yOffset
				Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

				// If we haven't already updated the chunks coordinate then run the code below
				if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
				{
					// If chunk is already generated for the given coordinate then update terrain chunk
					if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
					{
						terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
					}
					else
					{
						TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer, terrainMaterial);

						// Add new terrain chunk to terrainChunkDictionary
						terrainChunkDictionary.Add(viewedChunkCoord, newChunk);

						// Subscribe to onVisiblityChanged event
						newChunk.onVisibilityChanged += OnTerrainChunkVisibiltyChanged;

						// Call load method - load request data
						newChunk.Load();
					}
				}
			}
		}
	}

	// Method to create terrain chunk, check if the terrain chunks visibility changed and if it is visible
	void OnTerrainChunkVisibiltyChanged(TerrainChunk terrainChunk, bool isVisible)
	{
		if (isVisible)
		{
			visibleTerrainChunks.Add(terrainChunk);
		}
		else
		{
			visibleTerrainChunks.Remove(terrainChunk);
		}
	}
}

[System.Serializable]
public struct LODInfo
{
	[Range(0, MeshSettings.numSupportedLODs - 1)]
	public int lod;
	public float visibleDstThreshold;


	public float sqrVisibleDistThreshold
	{
		get
		{
			// Simply returns visibleDstThreshold times visibleDstThreshold
			return visibleDstThreshold * visibleDstThreshold;
		}
	}
}