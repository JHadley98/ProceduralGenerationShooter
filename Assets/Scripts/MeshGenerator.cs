using UnityEngine;
using System.Collections;

public static class MeshGenerator
{
	// Generate Terrain Mesh function
	public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail)
	{
		int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

		int borderedSize = heightMap.GetLength(0);
		int meshSize = borderedSize - 2 * meshSimplificationIncrement;
		int meshSizeUnsimplified = borderedSize - 2;

		float topLeftX = (meshSizeUnsimplified - 1) / -2f;
		float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

		// Varible to calculate the number of vertices per line
		int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

		// Calculate the correct number of vertices for the array
		MeshData meshData = new MeshData(verticesPerLine, meshSettings.useFlatShading);

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
					// If it is a BorderVertex the verticesIndicesMap is equal to borderVertexIndex
					vertexIndicesMap[x, y] = borderVertexIndex;
					// Deincrement index
					borderVertexIndex--;
				}
				else
				{
					// Else set vertexIndicesMap to be equal to meshVertexIndex
					vertexIndicesMap[x, y] = meshVertexIndex;
					// Increment the meshVertexIndex
					meshVertexIndex++;
				}
			}
		}

		// Loop through heightMap
		for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
		{
			for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
			{
				// Get vertexIndex from vertexIndicesMap
				int vertexIndex = vertexIndicesMap[x, y];
				// Calculate percent for uvs, looking at the left most side of the map and right most side of the map, looking at the mesh size
				// Take meshSimplificationIncrement from X and Y to ensure the uvs stay centre
				Vector2 percent = new Vector2((x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);
				// Set height equal to heightMap
				float height = heightMap[x, y];
				// Vertex position array - calculate X and Z values for mesh size
				Vector3 vertexPosition = new Vector3((topLeftX + percent.x * meshSizeUnsimplified) * meshSettings.meshScale, height, (topLeftZ - percent.y * meshSizeUnsimplified) * meshSettings.meshScale);
				// Call AddVertex method in meshData, pass in vertexPosition, percent and vertexIndex
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

		meshData.ProcessMesh();

		return meshData;

	}
}

public class MeshData
{
	// Create Arrays
	Vector3[] vertices;
	int[] triangles;
	Vector2[] uvs;
	Vector3[] bakedNormals;

	Vector3[] borderVertices;
	int[] borderTriangles;

	// Create triangle indexes
	int triangleIndex;
	int borderTriangleIndex;

	bool useFlatShading;

	// Mesh Data constructor
	public MeshData(int verticesPerLine, bool useFlatShading)
	{
		this.useFlatShading = useFlatShading;

		// Initialise variables
		vertices = new Vector3[verticesPerLine * verticesPerLine];
		uvs = new Vector2[verticesPerLine * verticesPerLine];
		triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

		borderVertices = new Vector3[verticesPerLine * 4 + 4];
		borderTriangles = new int[24 * verticesPerLine];
	}

	public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
	{
		// If vertexIndex is less than 0 check for borderVertex
		if (vertexIndex < 0)
		{
			borderVertices[-vertexIndex - 1] = vertexPosition;
		}
		else
		{
			// Regular vertex is equal to vertexPosition
			vertices[vertexIndex] = vertexPosition;
			// uvs index is equal to provided uv index
			uvs[vertexIndex] = uv;
		}
	}

	// Add triangles method, take in 3 vertices labled a, b, c
	public void AddTriangle(int a, int b, int c)
	{
		// Check if any of vertices that make up the triangle are borderVertices
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
		// Array to store vertexNormals
		Vector3[] vertexNormals = new Vector3[vertices.Length];
		// Calculate number of triangles
		int triangleCount = triangles.Length / 3;

		// Loop through all the regular triangles
		for (int i = 0; i < triangleCount; i++)
		{
			int normalTriangleIndex = i * 3;
			int vertexIndexA = triangles[normalTriangleIndex];
			int vertexIndexB = triangles[normalTriangleIndex + 1];
			int vertexIndexC = triangles[normalTriangleIndex + 2];

			Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
			// Add triangle normal to the each of the vertices that are part of the triangle
			vertexNormals[vertexIndexA] += triangleNormal;
			vertexNormals[vertexIndexB] += triangleNormal;
			vertexNormals[vertexIndexC] += triangleNormal;
		}

		// Calculate border triangles
		int borderTriangleCount = borderTriangles.Length / 3;

		// Loop through all the triangles belonging to the border
		for (int i = 0; i < borderTriangleCount; i++)
		{
			int normalTriangleIndex = i * 3;

			// Set all the indices for the vertices that make up the current triangle
			int vertexIndexA = borderTriangles[normalTriangleIndex];
			int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
			int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

			Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

			// If vertexIndex is greater than or equal to 0 then that index will exist in the vertexNormals array
			if (vertexIndexA >= 0)
			{
				vertexNormals[vertexIndexA] += triangleNormal;
			}
			if (vertexIndexB >= 0)
			{
				vertexNormals[vertexIndexB] += triangleNormal;
			}
			if (vertexIndexC >= 0)
			{
				vertexNormals[vertexIndexC] += triangleNormal;
			}
		}

		// Loop through the normals array and normalise
		for (int i = 0; i < vertexNormals.Length; i++)
		{
			vertexNormals[i].Normalize();
		}

		return vertexNormals;
	}

	// Method when given the vertex indices returns the normal vector of the triangle
	Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
	{
		// Check if the index is less than 0 then get is from the borderVertices array, otherwise get it from the vertices array
		Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA];
		Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
		Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

		// Cross product calculation
		Vector3 sideAB = pointB - pointA;
		Vector3 sideAC = pointC - pointA;
		// Return cross product and normalise result
		return Vector3.Cross(sideAB, sideAC).normalized;
	}

	public void ProcessMesh()
	{
		if (useFlatShading)
		{
			FlatShading();
		}
		else
		{
			BakeNormals();
		}
	}

	void BakeNormals()
	{
		bakedNormals = CalculateNormals();
	}

	void FlatShading()
	{
		Vector3[] flatShadedVertices = new Vector3[triangles.Length];
		Vector2[] flatShadedUvs = new Vector2[triangles.Length];

		for (int i = 0; i < triangles.Length; i++)
		{
			flatShadedVertices[i] = vertices[triangles[i]];
			flatShadedUvs[i] = uvs[triangles[i]];
			triangles[i] = i;
		}

		vertices = flatShadedVertices;
		uvs = flatShadedUvs;
	}

	public Mesh CreateMesh()
	{
		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		if (useFlatShading)
		{
			mesh.RecalculateNormals();
		}
		else
		{
			mesh.normals = bakedNormals;
		}
		return mesh;
	}

}