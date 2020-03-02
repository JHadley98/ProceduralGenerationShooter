using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator 
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
        // Each thread has its own heightCurve
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        int meshSimplificationIncrement = (levelOfDetail == 0) ?1:levelOfDetail * 2;
        // Varible to calculate the number of vertices per line
        int verticesPerline = (width - 1) / meshSimplificationIncrement + 1;

        // Calculate the correct number of vertices for the array
        MeshData meshData = new MeshData(verticesPerline, verticesPerline);
        int vertexIndex = 0;

        // Loop through heightMap
        for (int y = 0; y < height; y+= meshSimplificationIncrement)
        {
            for (int x = 0; x < width; x+= meshSimplificationIncrement)
            {
                // Calculate triangle vertices within a new Vector3
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y);

                // Calculate where the vertex is in relation to the map as a percentage for both the X and Y between 0 and 1
                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                if (x < width - 1 && y < height - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerline + 1, vertexIndex + verticesPerline);
                    meshData.AddTriangle(vertexIndex + verticesPerline + 1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }

        return meshData;
    }
}


public class MeshData
{
    // Vector 3 vertices array
    public Vector3[] vertices;
    // Int triangles array
    public int[] triangles;
    // Vector 2 array uvs
    public Vector2[] uvs;

    int triangleIndex;

    // Mesh Data constructor
    public MeshData(int meshWidth, int meshHeight)
    {
        // Initialise variables
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
    }

    // Add triangles method, take in 3 vertices labled a, b, c 
    public void AddTriangle(int a, int b, int c)
    {
        triangles [triangleIndex] = a;
        triangles [triangleIndex + 1] = b;
        triangles [triangleIndex + 2] = c;
        // Increment triangleIndex by 3
        triangleIndex += 3;
    }

    // Method to create mesh
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}