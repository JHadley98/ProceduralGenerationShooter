using UnityEngine;
using System.Collections;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
        // Each thread has its own heightCurve
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        // Varible to calculate the number of vertices per line
        int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

        // Calculate the correct number of vertices for the array
        MeshData meshData = new MeshData(verticesPerLine);

        // 2D integer array, for the vertexIndicesMap setting the new array to use borderedsize by borderersize
        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        // Loop through bordered size and see if bordered vertex is true
        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;

                if (isBorderVertex)
                {
                    // If it is BorderVertex then deincrement the borderVertexIndex
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    // Else increment the meshVertexIndex
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        // Loop through heightMap
        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                int vertexIndex = vertexIndicesMap[x, y];

                // Calculate percent for uvs, looking at the left most side of the map and right most side of the map, looking at the mesh size
                // Take meshSimplificationIncrement from X and Y to ensure the uvs stay centre
                Vector2 percent = new Vector2((x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);


                float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;

                // Height equation
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);

                // Add vertex to meshData passing through the vertexPosition, the percent and the vertexIndex
                meshData.AddVertex(vertexPosition, percent, vertexIndex);

                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    // Create triangles
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];
                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }

                vertexIndex++;
            }
        }

        meshData.BakeNormals();

        return meshData;
    }
}

public class MeshData
{
    // Arrays for vertices, triangles & uvs
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    Vector3[] bakedNormals;

    // Arrays for both border vertices & triangles
    Vector3[] borderVertices;
    int[] borderTriangles;

    // int index variables
    int triangleIndex;
    int borderTriangleIndex;

    // Mesh Data constructor
    public MeshData(int verticesPerLine)
    {
        // Initialise variables
        vertices = new Vector3[verticesPerLine * verticesPerLine];
        uvs = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

        // Initialise borderVertices & borderTriangles
        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[24 * verticesPerLine];
    }

    // Add vertex method
    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        // If the vertex is less than 0
        if (vertexIndex < 0)
        {
            // Add borderVertices index to array by starting at -vertexIndex to start at 1 then - 1 to start the index at 0, then set it equal to the provided vertexPosition
            borderVertices[-vertexIndex - 1] = vertexPosition;
        }
        // Else it is a regular vertex then:
        else
        {
            // Set vertices vertexIndex to equal vertex position
            vertices[vertexIndex] = vertexPosition;
            // Set uvs vertexIndex to equal uv
            uvs[vertexIndex] = uv;
        }
    }

    // Add triangles method, take in 3 vertices labled a, b, c
    public void AddTriangle(int a, int b, int c)
    {
        // Check if any of the vertices that make up the triangle are border vertices
        // If a, b & c are less than 0 then the triangle is belonging to the border
        if (a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3;
        }
        // Else it is regular triangle 
        else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }

    Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;

        // Loop through triangles
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;

            // Index in triangle array
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;

        // Loop through triangles belonging to the border
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

            // If vertexIndexA is greater than equal to 0 then that index will exist in the normal vertex index array
            if (vertexIndexA >= 0)
            {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            // If vertexIndexB is greater than equal to 0 then that index will exist in the normal vertex index array
            if (vertexIndexB >= 0)
            {
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            // If vertexIndexC is greater than equal to 0 then that index will exist in the normal vertex index array
            if (vertexIndexC >= 0)
            {
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }

        // For loop to normalise triangle vertex index
        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        // Set points A, B, and C index array
        // Check index is less than 0 by getting the index from the borderVertices array, otherwise we get it from the vertices array
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

        // Cross product calculation, setting sideAB and sideAC for the triangle
        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    // Method to create mesh
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = CalculateNormals();
        return mesh;
    }
}