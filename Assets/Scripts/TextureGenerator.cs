using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator
{
    // Public static Texture2D function, create texture from colour map, initialising width and height to be used by the texture
    public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height)
    {
        // Set new Texture2D to equal width and height
        Texture2D texture = new Texture2D(width, height);
        
        // Fix blurr of textures
        texture.filterMode = FilterMode.Point;
        // Fix texture wrapping
        texture.wrapMode = TextureWrapMode.Clamp;

        // Set and apply colourMap to texture
        texture.SetPixels(colourMap);
        texture.Apply();
        return texture;
    }

    // Public static Texture2D function, create texture from height map generatation
    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        // Set width and height to equal the noiseMap
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
           
        // Set colour array
        Color[] colourMap = new Color[width * height];

        float[,] data = new float[width, height];

        // Loop through values in noiseMap
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }

        return TextureFromColourMap(colourMap, width, height);
    }
}
