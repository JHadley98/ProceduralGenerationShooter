using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    // Reference to renderer
    public Renderer textureRender;

    // Method to DrawNoiseMap that references the 2D noiseMap
    public void DrawNoiseMap(float[,] noiseMap)
    {
        // Set width and height to equal the noiseMap
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        // Create 2D texture
        Texture2D texture = new Texture2D(width, height);

        // Set colour array
        Color[] colourMap = new Color[width * height];

        // Loop through values in noiseMap
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Set colourMap array to be have a colour between black and white, given a percentage between 0 and 1 using the noiseMap set with x, y
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, noiseMap[x, y]);
            }
        }
        // Apply colours to texture
        texture.SetPixels(colourMap);
        texture.Apply();

        // Apply texture to textureRender
        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(width, 1, height);
    }
}
