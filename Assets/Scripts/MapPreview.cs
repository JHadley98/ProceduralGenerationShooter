using UnityEngine;
using System.Collections;

public class MapPreview : MonoBehaviour
{
	public Renderer textureRender;
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;

	// Public variables:
	public enum DrawMode { NoiseMap, Mesh, FalloffMap };
	public DrawMode drawMode;

	// Data class references
	public MeshSettings meshSettings;
	public HeightMapSettings heightMapSettings;
	public TextureSettings textureData;

	public Material terrainMaterial;

	[Range(0, MeshSettings.numSupportedLODs - 1)]
	public int editorPreviewLOD;
	public bool autoUpdate;

	public void DrawMapInEditor()
	{
		// Apply material
		textureData.ApplyToMaterial(terrainMaterial);

		// Update mesh heights
		textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

		// Create heightMap
		HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero);

		// If drawMode equals a set DrawMode display that mode
		if (drawMode == DrawMode.NoiseMap)
		{
			DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
		}
		else if (drawMode == DrawMode.Mesh)
		{
			DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorPreviewLOD));
		}
		else if (drawMode == DrawMode.FalloffMap)
		{
			DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVertsPerLine), 0, 1)));
		}
	}

	// Method to Draw Texture to the screen
	public void DrawTexture(Texture2D texture)
	{
		// Apply texture to textureRender
		textureRender.sharedMaterial.mainTexture = texture;
		textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;

		textureRender.gameObject.SetActive(true);
		meshFilter.gameObject.SetActive(false);
	}

	public void DrawMesh(MeshData meshData)
	{
		// Share mesh filter as it maybe generating the mesh outside of game mode
		meshFilter.sharedMesh = meshData.CreateMesh();

		textureRender.gameObject.SetActive(false);
		meshFilter.gameObject.SetActive(true);
	}

	private void OnValuesUpdated()
	{
		if (!Application.isPlaying)
		{
			DrawMapInEditor();
		}
	}

	// Update texture values function
	private void OnTextureValuesUpdated()
	{
		// Apply material to terrain material from texture data script
		textureData.ApplyToMaterial(terrainMaterial);
	}

	private void OnValidate()
	{
		// Data is not null then, set the data value to be OnValuesUpdated, however, doing -= first so that there is no overlap of updated values
		if (meshSettings != null)
		{
			meshSettings.OnValuesUpdated -= OnValuesUpdated;
			meshSettings.OnValuesUpdated += OnValuesUpdated;
		}
		if (heightMapSettings != null)
		{
			heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
			heightMapSettings.OnValuesUpdated += OnValuesUpdated;
		}
		if (textureData != null)
		{
			textureData.OnValuesUpdated -= OnTextureValuesUpdated;
			textureData.OnValuesUpdated += OnTextureValuesUpdated;
		}
	}
}