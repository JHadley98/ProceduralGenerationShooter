using UnityEngine;
using System.Collections;

public class MapDisplay : MonoBehaviour
{
	public Renderer textureRender;
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;

	// Method to Draw Texture to the screen
	public void DrawTexture(Texture2D texture)
	{
		// Apply texture to textureRender
		textureRender.sharedMaterial.mainTexture = texture;
		textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height);
	}

	public void DrawMesh(MeshData meshData)
	{
		// Share mesh filter as it maybe generating the mesh outside of game mode
		meshFilter.sharedMesh = meshData.CreateMesh();

		// Set meshes size equal to terrainData uniform scale
		meshFilter.transform.localScale = Vector3.one * FindObjectOfType<MapGenerator>().meshSettings.meshScale;
	}
}