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
    public static Texture2D TextureFromHeightMap(HeightMap heightMap)
    {
        // Set width and height to equal the noiseMap
        int width = heightMap.values.GetLength(0);
        int height = heightMap.values.GetLength(1);

        // Set colour array
        Color[] colourMap = new Color[width * height];
        // Loop through values in noiseMap
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Draw noise map between a range of 0 to 1 using the inverse lerp passing in the min values and max values
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, Mathf.InverseLerp(heightMap.minValue, heightMap.maxValue, heightMap.values[x, y]));
            }
        }

        return TextureFromColourMap(colourMap, width, height);
    }

}