﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndlessTerrain : MonoBehaviour
{
    const float scale = 2f;

    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public LODInfo[] detailLevels;
    public static float maxViewDst;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start()
    {
        // Assign mapGenerator to object of type MapGenerator class
        mapGenerator = FindObjectOfType<MapGenerator>();

        // Set maxViewDst to be detailLevels length - 1
        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        // Set chunksize
        chunkSize = MapGenerator.mapChunkSize - 1;
        // Set number of chunk visible in view distance
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);

        UpdateVisibleChunks();
    }

    void Update()
    {
        // Update viewer Position variable
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / scale;

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        // Loop through all the newly visible chunks
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            // Set visible to false
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        // Clear list
        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
            {
                // Vector2 set viewChunkCoord to equal a new Vector2 set the X value to currentChunkCoordX + xOffset and set the Y value to currentChunkCoordY + yOffset
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                // If chunk is already generated for the given coordinate then update terrain chunk
                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                }
                else
                {
                    // Add new terrain chunk to terrainChunkDictionary
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk
    {
        // Instaniate a plane object
        GameObject meshObject;
        // Set Vector2 position variable
        Vector2 position;
        // Set aligned bounding box
        Bounds bounds;

        // MeshFilter component
        MeshRenderer meshRenderer;
        // MeshRenderer component
        MeshFilter meshFilter;
        // MeshCollider component
        MeshCollider meshCollider;

        // Level Of Detail info array for detail levels
        LODInfo[] detailLevels;
        // Level of Details mesh array for LOD for the meshes
        LODMesh[] lodMeshes;
        // Collision Level of detail mesh
        LODMesh collisionLODMesh;

        // MapData reference
        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;

        // Public constructor
        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;

            // Initialise Vector2 position
            position = coord * size;
            // Initialise bounds
            bounds = new Bounds(position, Vector2.one * size);
            // Set position in 3D space
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            // Set meshObject to equal new GameObject
            meshObject = new GameObject("Terrain Chunk");
            // Add MeshRenderer component
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            // Add MeshFilter component
            meshFilter = meshObject.AddComponent<MeshFilter>();
            // Add MeshCollider component
            meshCollider = meshObject.AddComponent<MeshCollider>();
            // Add the give material to the renderer
            meshRenderer.material = material;

            // Set object position using the Vector3 position
            meshObject.transform.position = positionV3 * scale;
            // Set meshObject to attached to the parent map generator
            meshObject.transform.parent = parent;
            // Set meshjObject scale
            meshObject.transform.localScale = Vector3.one * scale;
            // Set default state of chunk to be invisible
            SetVisible(false);

            // New Level of Detail Mesh array
            lodMeshes = new LODMesh[detailLevels.Length];
            // Loop through all the detail levels for meshes
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
                if (detailLevels[i].useForCollider)
                {
                    collisionLODMesh = lodMeshes[i];
                }
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            // mapData is equal to mapData and data received for the map is set to true
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

        // Public update method - to get the terrain chunk to update itself
        public void UpdateTerrainChunk()
        {
            // if mapDataReceived to the following:
            if (mapDataReceived)
            {
                // Set viewer distance from nearest edge to equal the sqrt distance of the viewerPosition
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                // Declare whether or not the chunk is visible is determined from the viewer distance from nearest edge being less than or equal to the maxViewDistance				
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
                            // Break out of the loop
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

                    // If lodIndex is equal 0, and the player is close enough then render the terrain at it's highest possible resolution, then add collisions
                    if (lodIndex == 0)
                    {
                        // If collisionLODMesh has a mesh then set meshCollider.shared mesh to have a mesh
                        if (collisionLODMesh.hasMesh)
                        {
                            meshCollider.sharedMesh = collisionLODMesh.mesh;
                        }
                        // Else if collisionLODMesh has not been requested then RequestMesh
                        else if (!collisionLODMesh.hasRequestedMesh)
                        {
                            collisionLODMesh.RequestMesh(mapData);
                        }
                    }

                    // If terrain chunk is visible add itself to the list, using the 'this' key word
                    terrainChunksVisibleLastUpdate.Add(this);
                }
                // Update to determine if the terrain chunk is visible
                SetVisible(visible);
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
        // Public mesh object
        public Mesh mesh;
        // Bool has a mesh been requested
        public bool hasRequestedMesh;
        // Bool has mesh been received
        public bool hasMesh;
        // Integer for the Level of Detail
        int lod;
        // updateCallback function
        System.Action updateCallback;

        // Level of Detail function, set lod to equal lod and upateCallBack
        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            // Create mesh
            mesh = meshData.CreateMesh();
            // Has mesh is now true
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            // Mesh has been requested so set to true
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDstThreshold;
        public bool useForCollider;
    }
}