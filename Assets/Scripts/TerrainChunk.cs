using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk
{
	// Used to see how close the play is to the edge of the terrain chunk before it will create the collider
	const float colliderGenerationDistanceThreshold = 5;

	public event System.Action<TerrainChunk, bool> onVisibilityChanged;

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

	bool heightMapReceived;
	int previousLODIndex = -1;
	bool hasSetCollider;
	float maxViewDist;

	// Class references
	HeightMapSettings heightMapSettings;
	MeshSettings meshSettings;
	HeightMap heightMap;

	Transform viewer;

	// Public constructor
	public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material)
	{
		this.coord = coord;
		this.detailLevels = detailLevels;
		this.colliderLODIndex = colliderLODIndex;
		this.heightMapSettings = heightMapSettings;
		this.meshSettings = meshSettings;
		this.viewer = viewer;

		// Initialise values
		sampleCentre = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
		Vector2 position = coord * meshSettings.meshWorldSize;
		bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);

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
			// lodMeshes equals a new LODMesh constructor
			lodMeshes[i] = new LODMesh(detailLevels[i].lod);

			// Set update call back to equal update terrain chunk method
			lodMeshes[i].updateCallback += UpdateTerrainChunk;

			// If i equals collider level of detail index then:
			if (i == colliderLODIndex)
			{
				// Update collision mesh method
				lodMeshes[i].updateCallback += UpdateCollisionMesh;
			}
		}

		maxViewDist = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
	}

	// Load method, store request data function
	public void Load()
	{
		ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, sampleCentre), OnHeightMapReceived);
	}
	void OnHeightMapReceived(object heightMapObject)
	{
		// mapData is equal to mapData and data received for the map is set to 
		this.heightMap = (HeightMap)heightMapObject;
		heightMapReceived = true;

		UpdateTerrainChunk();
	}

	// Vector 2 accessor used to return a new vector for the viewers position on the X and Z axes
	Vector2 viewerPosition
	{
		get
		{
			return new Vector2(viewer.position.x, viewer.position.z);
		}
	}

	// Update terrain chunk method
	public void UpdateTerrainChunk()
	{
		// if mapDataReceived to the following:
		if (heightMapReceived)
		{
			// Set viewer distance from nearest edge to equal the sqrt distance of the viewerPosition
			float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

			// Declare wasVisible true or false bool and set it equal to IsVisible
			bool wasVisible = IsVisible();

			// Declare whether or not the chunk is visible is determined from the viewer distance from nearest edge being less than or equal to the maxViewD
			bool visible = viewerDstFromNearestEdge <= maxViewDist;

			// If terrain is visible do the following
			if (visible)
			{
				// int array level of detail index starting at 0
				int lodIndex = 0;

				// Loop through detailLevels length
				for (int i = 0; i < detailLevels.Length - 1; i++)
				{
					if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold)
					{
						// If the viewerDistanceFromNearestEdge is morethan the detailLevels that mean the lodIndex = i + 1
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
						lodMesh.RequestMesh(heightMap, meshSettings);
					}
				}
			}

			if (wasVisible != visible)
			{
				// Set terrain chunk to be visible
				SetVisible(visible);

				if (onVisibilityChanged != null)
				{
					// Invoke this, passing in the terrain chunk by calling 'this' and visible bool
					onVisibilityChanged(this, visible);
				}
			}
		}
	}

	public void UpdateCollisionMesh()
	{
		// Only do the code in the method if the collider is not set
		if (!hasSetCollider)
		{
			float sqrDistFromViewerToEdge = bounds.SqrDistance(viewerPosition);

			// If the squared distance to viewer is less than visible threshold then
			if (sqrDistFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDistThreshold)
			{
				// If lodMesh for collider has not yet requested a mesh then 
				if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
				{
					// Request the mesh straight away from heightMap
					lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);
				}
			}

			// If the square distance to the viewer edge is less than the collider generation distance threhold squaded then:
			if (sqrDistFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
			{
				// Check if the collider level of detail index has requested and received a mesh the mesh generator:
				if (lodMeshes[colliderLODIndex].hasMesh)
				{
					// Set collision mesh
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

	// Function to receive mesh data
	void OnMeshDataReceived(object meshDataObject)
	{
		// Create mesh
		mesh = ((MeshData)meshDataObject).CreateMesh();
		hasMesh = true;

		updateCallback();
	}

	// Function to request mesh from map data
	public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
	{
		hasRequestedMesh = true;
		ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod), OnMeshDataReceived);
	}
}