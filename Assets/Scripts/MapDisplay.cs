using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    // Reference to renderer
    public Renderer textureRender;

    
    // Method to DrawTexturethat 
    public void DrawTexture(Texture2D texture)
    {
        // Apply texture to textureRender
        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }
}
