using UnityEngine;
using System.Collections;

/* This class is used to store data for the MeshSettings */

[CreateAssetMenu()]
public class MeshSettings : UpdatableData
{
	// Variables to controll the chunk size of the mesh
	public const int numSupportedLODs = 5;
	public const int numSupportedChunkSizes = 9;
	public const int numSupportedFlatshadedChunkSizes = 3;
	public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

	public float meshScale = 2.5f;
	public bool useFlatShading;

	// Chunk size index for chunk size and flat shaded chunk size
	[Range(0, numSupportedChunkSizes - 1)]
	public int chunkSizeIndex;
	[Range(0, numSupportedFlatshadedChunkSizes - 1)]
	public int flatshadedChunkSizeIndex;

	// Number of vertices per line of mesh rendered at LOD = 0. Includes the 2 extra vertices that are excluded from final mesh, but used for calculating normals.
	public int numVertsPerLine
	{
		get
		{
			return supportedChunkSizes[(useFlatShading) ? flatshadedChunkSizeIndex : chunkSizeIndex] + 5;
		}
	}

	// Method used to know how much space the mesh takes up
	public float meshWorldSize
	{
		get
		{
			return (numVertsPerLine - 3) * meshScale;
		}
	}
}