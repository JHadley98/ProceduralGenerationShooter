using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    // Reference to renderer
    public Renderer textureRender;
    // Reference to mesh filter
    public MeshFilter meshFilter;
    // Reference to mesh renderer
    public MeshRenderer meshRenderer;

    // Method to Draw Texture to the screen
    public void DrawTexture(Texture2D texture)
    {
        // Apply texture to textureRender
        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawMesh(MeshData meshData , Texture2D texture)
    {
        // Share mesh filter as it maybe generating the mesh outside of game mode
        meshFilter.sharedMesh = meshData.CreateMesh();
        // Share mesh renderer as it maybe generating the texture for the mesh outside of game mode
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
}
