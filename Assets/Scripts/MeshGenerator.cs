using UnityEngine;
using System.Collections;

public static class MeshGenerator
{
	public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail)
	{

		int skipIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

		// Varible to calculate the number of vertices per line
		int numVertsPerLine = meshSettings.numVertsPerLine;

		Vector2 topLeft = new Vector2(-1, 1) * meshSettings.meshWorldSize / 2f;

		// Calculate the correct number of vertices for the array
		MeshData meshData = new MeshData(numVertsPerLine, skipIncrement, meshSettings.useFlatShading);

		// 2D integer array, for the vertexIndicesMap setting the new array to use numVertsPerLine by numVertsPerLine
		int[,] vertexIndicesMap = new int[numVertsPerLine, numVertsPerLine];
		int meshVertexIndex = 0;
		int outOfMeshVertexIndex = -1;

		// Loop through numVertsPerLine to check if the mesh isOutofMeshVertex
		for (int y = 0; y < numVertsPerLine; y++)
		{
			for (int x = 0; x < numVertsPerLine; x++)
			{
				// Calculate if the mesh isOutofMeshVertex
				bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;

				// Calculate whether or not it's a skipped vertex
				// If either X or Y aren't evenly divisible by skip increment then it's a main vertex and isSkippedVertex will be set to false
				bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);

				// If the current vertex is an out of mesh vertex then, assign the outOfMeshVertexIndex to the vertexIndicesMap
				if (isOutOfMeshVertex)
				{
					vertexIndicesMap[x, y] = outOfMeshVertexIndex;
					outOfMeshVertexIndex--;
				}
				// Else if it is not isSkippedVertex, then assign the meshVertexIndex to the vertexIndicesMap
				else if (!isSkippedVertex)
				{
					vertexIndicesMap[x, y] = meshVertexIndex;
					meshVertexIndex++;
				}
			}
		}

		// Loop through heightMap
		for (int y = 0; y < numVertsPerLine; y++)
		{
			for (int x = 0; x < numVertsPerLine; x++)
			{
				// Calculate whether or not it's a skipped vertex
				// If either X or Y aren't evenly divisible by skip increment then it's a main vertex and isSkippedVertex will be set to false
				bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);

				if (!isSkippedVertex)
				{
					/// Calculations for which type of vertex is being used ///
					// Calculate if the mesh isOutofMeshVertex
					bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
					// Calculate if it is a mesh edge vertex, excluding the out of mesh vertex
					bool isMeshEdgeVertex = (y == 1 || y == numVertsPerLine - 2 || x == 1 || x == numVertsPerLine - 2) && !isOutOfMeshVertex;
					// Calculate if it is a main vertex, excluding the out of mesh vertex and mesh edge vertex
					bool isMainVertex = (x - 2) % skipIncrement == 0 && (y - 2) % skipIncrement == 0 && !isOutOfMeshVertex && !isMeshEdgeVertex;
					// Calculate if it is an edge connection vertex, exclude the  half of mesh vertices, as well as mesh edge vertices and the main vertices
					bool isEdgeConnectionVertex = (y == 2 || y == numVertsPerLine - 3 || x == 2 || x == numVertsPerLine - 3) && !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;
					
					// Get vertexIndex from vertexIndicesMap
					int vertexIndex = vertexIndicesMap[x, y];

					// Vector 2 equation to calculate the percentage used find where mesh will start and end
					// X is equal to 1 as that is where the mesh will start
					// Percent needs to equal 1 when X is equal to the number of vertices per line - 2 this is where mesh will end
					Vector2 percent = new Vector2(x - 1, y - 1) / (numVertsPerLine - 3);
					
					// Set vector2 vertexPosition2D
					Vector2 vertexPosition2D = topLeft + new Vector2(percent.x, -percent.y) * meshSettings.meshWorldSize;
					
					// Set hieght equal to heightMapfloat height = heightMap[x, y];
					float height = heightMap[x, y];

					// If it is an edge connection vertex then:
					if (isEdgeConnectionVertex)
					{
						// isVertical is used to see if we are working on the x axis or working on the y axis
						// It isVertical if x equals 2 or x equals the numVertsPerLine - 3
						bool isVertical = x == 2 || x == numVertsPerLine - 3;
						
						// Distance to main vertex A is the closest main vertex above the current edge connection vertex
						int dstToMainVertexA = ((isVertical) ? y - 2 : x - 2) % skipIncrement;
						int dstToMainVertexB = skipIncrement - dstToMainVertexA;
						// Calculate the distance percent between A and B vertexes
						float dstPercentFromAToB = dstToMainVertexA / (float)skipIncrement;

						// Calculate the height of main vertex A and vertex B, using the heightMap
						float heightMainVertexA = heightMap[(isVertical) ? x : x - dstToMainVertexA, (isVertical) ? y - dstToMainVertexA : y];
						float heightMainVertexB = heightMap[(isVertical) ? x : x + dstToMainVertexB, (isVertical) ? y + dstToMainVertexB : y];

						// Set height equal to a weighted sum of height A and height B with the weight based on the distance percent
						height = heightMainVertexA * (1 - dstPercentFromAToB) + heightMainVertexB * dstPercentFromAToB;
					}

					// Add vertex to meshData passing through the vertexPosition for both x & y, the height, the percent and the vertexIndex
					meshData.AddVertex(new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), percent, vertexIndex);

					// Create triangle, this will only be true if the mesh is not in the bottom most row or the right most column
					bool createTriangle = x < numVertsPerLine - 1 && y < numVertsPerLine - 1 && (!isEdgeConnectionVertex || (x != 2 && y != 2));

					// if create triangles, create triangles
					if (createTriangle)
					{
						// If it is a main vertex and both x, y are not equal to numVertsPerLine - 3 then it is going to be a main triangle
						// Set the current increment to skipIncrement otherwise it is just a small triangle given the value of 1
						int currentIncrement = (isMainVertex && x != numVertsPerLine - 3 && y != numVertsPerLine - 3) ? skipIncrement : 1;

						// Set vertices for the triangle
						int a = vertexIndicesMap[x, y];
						int b = vertexIndicesMap[x + currentIncrement, y];
						int c = vertexIndicesMap[x, y + currentIncrement];
						int d = vertexIndicesMap[x + currentIncrement, y + currentIncrement];
						meshData.AddTriangle(a, d, c);
						meshData.AddTriangle(d, a, b);
					}
				}
			}
		}

		meshData.ProcessMesh();

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

	// Arrays for both out of mesh vertices & out of mesh triangles
	Vector3[] outOfMeshVertices;
	int[] outOfMeshTriangles;

	// int index variables
	int triangleIndex;
	int outOfMeshTriangleIndex;

	bool useFlatShading;

	// Mesh Data constructor
	public MeshData(int numVertsPerLine, int skipIncrement, bool useFlatShading)
	{
		this.useFlatShading = useFlatShading;

		// Calculate number of mesh edge vertices
		int numMeshEdgeVertices = (numVertsPerLine - 2) * 4 - 4;
		// Calculate the number of edge connection vertices
		int numEdgeConnectionVertices = (skipIncrement - 1) * (numVertsPerLine - 5) / skipIncrement * 4;
		// Caluclate number of main vertices per line
		int numMainVerticesPerLine = (numVertsPerLine - 5) / skipIncrement + 1;
		// Calculate total number of main vertices
		int numMainVertices = numMainVerticesPerLine * numMainVerticesPerLine;

		// Initialise variables
		vertices = new Vector3[numMeshEdgeVertices + numEdgeConnectionVertices + numMainVertices];
		uvs = new Vector2[vertices.Length];

		// Calculate the number of mesh edge triangles: ((numMeshEdgeVertices - 3) * 4 - 4) * 2
		// Triangle edge is multiplied by 4 for each side, then subtracted by 4 for the double counting of corners
		// Finally times everything by 2 since there are 2 triangles in each square
		// numMeshEdgeTriangles is then simplified to equal:
		int numMeshEdgeTriangles = 8 * (numVertsPerLine - 4);
		
		// Calculate the number of main triangles
		int numMainTriangles = (numMainVerticesPerLine - 1) * (numMainVerticesPerLine - 1) * 2;

		// Length of triangles array:
		triangles = new int[(numMeshEdgeTriangles + numMainTriangles) * 3];

		///	Calculate and initialise outOfMeshVertices & outOfMeshTriangles arrays: ///
		// outOfMeshVertices equals the numVertsPerLine which includes the mesh vertices times 4 for each edge then subtract 4 for each corner
		outOfMeshVertices = new Vector3[numVertsPerLine * 4 - 4];
		// outOfMeshTriangles equals ((numVertsPerLine - 1) * 4 - 4) * 2 * 3
		// NumVertsPerLine is multiplied by 4 for each side of the square, then subracted by 4 for each corner
		// NumVertsPerLine is then further multiplied by 2 as each square contains 2 triangles, then multiply by 3 as there are 3 vertices in a triangle
		// outOfMeshTriangles calculation is then simplified to be:
		outOfMeshTriangles = new int[24 * (numVertsPerLine - 2)];
	}

	// Add vertex method
	public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
	{
		// If the vertex is less than 0
		if (vertexIndex < 0)
		{
			// Add out of mesh vertices to the array by starting at -vertexIndex to start at 1 then - 1 to start the index at 0, then set it equal to the provided vertexPosition
			outOfMeshVertices[-vertexIndex - 1] = vertexPosition;
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
		// Check if any of the vertices that make up the triangle are out of mesh triangles
		// If a, b & c are less than 0 then the triangle is belonging to the border
		if (a < 0 || b < 0 || c < 0)
		{
			outOfMeshTriangles[outOfMeshTriangleIndex] = a;
			outOfMeshTriangles[outOfMeshTriangleIndex + 1] = b;
			outOfMeshTriangles[outOfMeshTriangleIndex + 2] = c;
			outOfMeshTriangleIndex += 3;
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
			// Index in triangle array
			int normalTriangleIndex = i * 3;
			int vertexIndexA = triangles[normalTriangleIndex];
			int vertexIndexB = triangles[normalTriangleIndex + 1];
			int vertexIndexC = triangles[normalTriangleIndex + 2];

			Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
			vertexNormals[vertexIndexA] += triangleNormal;
			vertexNormals[vertexIndexB] += triangleNormal;
			vertexNormals[vertexIndexC] += triangleNormal;
		}

		// Calculate border triangles
		int borderTriangleCount = outOfMeshTriangles.Length / 3;

		// Loop through all the triangles belonging to the border
		for (int i = 0; i < borderTriangleCount; i++)
		{
			// Set all the indices for the vertices that make up the current triangle
			int normalTriangleIndex = i * 3;
			int vertexIndexA = outOfMeshTriangles[normalTriangleIndex];
			int vertexIndexB = outOfMeshTriangles[normalTriangleIndex + 1];
			int vertexIndexC = outOfMeshTriangles[normalTriangleIndex + 2];

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

	// Method when given the vertex indices returns the normal vector of the triangle
	Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
	{
		// Set points A, B, and C index array
		// Check index is less than 0 by getting the index from the borderVertices array, otherwise we get it from the vertices array
		Vector3 pointA = (indexA < 0) ? outOfMeshVertices[-indexA - 1] : vertices[indexA];
		Vector3 pointB = (indexB < 0) ? outOfMeshVertices[-indexB - 1] : vertices[indexB];
		Vector3 pointC = (indexC < 0) ? outOfMeshVertices[-indexC - 1] : vertices[indexC];

		// Cross product calculation, setting sideAB and sideAC for the triangle
		Vector3 sideAB = pointB - pointA;
		Vector3 sideAC = pointC - pointA;
		return Vector3.Cross(sideAB, sideAC).normalized;
	}

	// Process mesh function
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

	// Bake normals function
	private void BakeNormals()
	{
		bakedNormals = CalculateNormals();
	}

	// Flat shading method
	private void FlatShading()
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

	// Method to create mesh
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