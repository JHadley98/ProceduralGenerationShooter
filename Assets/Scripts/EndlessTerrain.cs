using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndlessTerrain : MonoBehaviour
{

	const float viewerMoveThresholdForChunkUpdate = 25f;
	const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
	const float colliderGenerationDistanceThreshold = 5;

	public int colliderLODIndex;
	public LODInfo[] detailLevels;
	public static float maxViewDst;

	public Transform viewer;
	public Material mapMaterial;

	public static Vector2 viewerPosition;
	Vector2 viewerPositionOld;
	static MapGenerator mapGenerator;
	float meshWorldSize;
	int chunksVisibleInViewDst;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	static List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

	void Start()
	{
		// Assign mapGenerator to object of type MapGenerator class
		mapGenerator = FindObjectOfType<MapGenerator>();

		// Set maxViewDst to be detailLevels length - 1
		maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
		// Set chunksize
		meshWorldSize = mapGenerator.meshSettings.meshWorldSize;
		// Set number of chunk visible in view distance
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);

		UpdateVisibleChunks();
	}

	void Update()
	{
		// Update viewer Position variable
		viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

		// Check if viewerPosition is not equal to viewerPositionOld
		if (viewerPosition != viewerPositionOld)
		{
			// Loop through chunk in terrain chunk checking are visible terrain chunks
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
						// Add new terrain chunk to terrainChunkDictionary
						terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, meshWorldSize, detailLevels, colliderLODIndex, transform, mapMaterial));
					}
				}

			}
		}
	}

	public class TerrainChunk
	{

		public Vector2 coord;


		// Set gameobject
		GameObject meshObject;
		// Set position and bounds
		Vector2 sampleCentre;
		Bounds bounds;

		// Set mesh components
		MeshRenderer meshRenderer;
		MeshFilter meshFilter;
		MeshCollider meshCollider;
		
		// Set LOD arrays
		LODInfo[] detailLevels;
		LODMesh[] lodMeshes;
		int colliderLODIndex;

		// MapData class reference
		HeightMap mapData;
		bool mapDataReceived;
		int previousLODIndex = -1;
		bool hasSetCollider;

		// Public constructor
		public TerrainChunk(Vector2 coord, float meshWorldSize, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Material material)
		{
			this.coord = coord;
			this.detailLevels = detailLevels;
			this.colliderLODIndex = colliderLODIndex;

			// Initialise 
			sampleCentre = coord * meshWorldSize / mapGenerator.meshSettings.meshScale;
			Vector2 position = coord * meshWorldSize;
			bounds = new Bounds(sampleCentre, Vector2.one * meshWorldSize);


			// Assign components
			meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshCollider = meshObject.AddComponent<MeshCollider>();
			meshRenderer.material = material;

			// Assign transforms
			meshObject.transform.position = new Vector3(position.x, 0, position.y);
			meshObject.transform.parent = parent;
			SetVisible(false);

			// Create new LOD array
			lodMeshes = new LODMesh[detailLevels.Length];

			// Loop through all the detail levels for meshes
			for (int i = 0; i < detailLevels.Length; i++)
			{
				lodMeshes[i] = new LODMesh(detailLevels[i].lod);
				lodMeshes[i].updateCallback += UpdateTerrainChunk;
				if (i == colliderLODIndex)
				{
					lodMeshes[i].updateCallback += UpdateCollisionMesh;
				}
			}

			mapGenerator.RequestHeightMap(sampleCentre, OnMapDataReceived);
		}

		void OnMapDataReceived(HeightMap mapData)
		{
			// mapData is equal to mapData and data received for the map is set to true
			this.mapData = mapData;
			mapDataReceived = true;

			UpdateTerrainChunk();
		}

		// Update terrain chunk method
		public void UpdateTerrainChunk()
		{
			// if mapDataReceived to the following:
			if (mapDataReceived)
			{
				// Set viewer distance from nearest edge to equal the sqrt distance of the viewerPosition
				float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

				// Declare wasVisible true or false bool and set it equal to IsVisible
				bool wasVisible = IsVisible();

				// Declare whether or not the chunk is visible is determined from the viewer distance from nearest edge being less than or equal to the maxViewD
				bool visible = viewerDstFromNearestEdge <= maxViewDst;

				// If terrain is visible do the following
				if (visible)
				{
					// int array level of detail index starting at 0
					int lodIndex = 0;

					// Loop through detailLevels length
					for (int i = 0; i < detailLevels.Length - 1; i++)
					{
						// If the viewerDistanceFromNearestEdge is morethan the detailLevels that mean the lodIndex = i + 1
						if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold)
						{
							lodIndex = i + 1;
						}
						// Else if it is less than, then it is the correct level of detail index
						else
						{
							break;
						}
					}

					// If Level of Detail Index is not equal to the previous Level Of Detail Index then:
					if (lodIndex != previousLODIndex)
					{
						// The Level of Detail mesh we want to be working with is the one from the level of detail meshes array with a index of the current Level of Detail Index
						LODMesh lodMesh = lodMeshes[lodIndex];

						// If level of detail has a mesh then:
						if (lodMesh.hasMesh)
						{
							// Only if successful in setting the mesh, the previous level of detail index is then equal to the current level of detail index
							previousLODIndex = lodIndex;
							// Then set current mesh to the current level of detail mesh
							meshFilter.mesh = lodMesh.mesh;
						}
						// Otherwise if level of detail mesh has not requested for a mesh then:
						else if (!lodMesh.hasRequestedMesh)
						{
							lodMesh.RequestMesh(mapData);
						}
					}
				}

				if (wasVisible != visible)
				{
					if (visible)
					{
						// If terrain chunk is visible add itself to the list, using the 'this' key word
						visibleTerrainChunks.Add(this);
					}
					else
					{
						// Else remove itself from the list
						visibleTerrainChunks.Remove(this);
					}
					// Update to determine if the terrain chunk is visible
					SetVisible(visible);
				}
			}
		}

		public void UpdateCollisionMesh()
		{
			if (!hasSetCollider)
			{
				float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

				if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDstThreshold)
				{
					if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
					{
						lodMeshes[colliderLODIndex].RequestMesh(mapData);
					}
				}

				if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
				{
					if (lodMeshes[colliderLODIndex].hasMesh)
					{
						meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
						hasSetCollider = true;
					}
				}
			}
		}

		// Set active state for mesh object to be visible
		public void SetVisible(bool visible)
		{
			meshObject.SetActive(visible);
		}

		// Public bool function to be called to check is mesh is visible
		public bool IsVisible()
		{
			return meshObject.activeSelf;
		}

	}

	// Level of Detail Mesh Class
	class LODMesh
	{
		// Declare variables
		public Mesh mesh;
		public bool hasRequestedMesh;
		public bool hasMesh;
		int lod;
		// updateCallback function
		public event System.Action updateCallback;

		// Level of Detail function, set lod to equal lod and upateCallBack
		public LODMesh(int lod)
		{
			this.lod = lod;
		}

		void OnMeshDataReceived(MeshData meshData)
		{
			mesh = meshData.CreateMesh();
			hasMesh = true;

			updateCallback();
		}

		public void RequestMesh(HeightMap mapData)
		{
			hasRequestedMesh = true;
			mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
		}
	}

	[System.Serializable]
	public struct LODInfo
	{
		[Range(0, MeshSettings.numSupportedLODs - 1)]
		public int lod;
		public float visibleDstThreshold;


		public float sqrVisibleDstThreshold
		{
			get
			{
				return visibleDstThreshold * visibleDstThreshold;
			}
		}
	}
}