using UnityEngine;

public static class Noise
{
    public enum NormaliseMode { Local, Global };

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings noiseSettings, Vector2 sampleCentre)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        // Psuedo Random Number Generator
        System.Random prng = new System.Random(noiseSettings.seed);
        // Array set to octaves
        Vector2[] octaveOffsets = new Vector2[noiseSettings.octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        // Loop through octaves
        for (int i = 0; i < noiseSettings.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + noiseSettings.offset.x + sampleCentre.x;
            float offsetY = prng.Next(-100000, 100000) - noiseSettings.offset.y - sampleCentre.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            // persistance value is in the range 0 to 1, so that decreases each octave
            amplitude *= noiseSettings.persistance;
        }

        if (noiseSettings.scale <= 0)
        {
            noiseSettings.scale = 0.0001f;
        }

        // Set float values outside of for loop
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        // Loop through noiseMap
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {

                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < noiseSettings.octaves; i++)
                {
                    // Calculate and control noise scale
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / noiseSettings.scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / noiseSettings.scale * frequency;

                    // Set perlinValue
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    // Apply perlinValue to noiseHeight and multiply by amplitude
                    noiseHeight += perlinValue * amplitude;

                    // persistance value is in the range 0 to 1, so that decreases each octave
                    amplitude *= noiseSettings.persistance;
                    // frequency increases each octave, since lacunarity should be > 1
                    frequency *= noiseSettings.lacunarity;
                }

                // If noiseHeight is greater than the maxNoiseHeight set the maxNoiseHeight to equal noiseHeight
                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }
                // If noiseHeight is less than the minNoiseHeight set the minNoiseHeight to equal noiseHeight
                if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }
                // Set noiseMap x, y to equal noiseHeight
                noiseMap[x, y] = noiseHeight;

                if (noiseSettings.normalizeMode == NormaliseMode.Global)
                {
                    // Global normalisation
                    float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight / 0.9f);
                    // Clamp noiseMap to normalisedHeight on X axis, y to 0 and z axis to the max int value
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }

        if (noiseSettings.normalizeMode == NormaliseMode.Local)
        {
            // For loop to normalise noiseMap
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    // If normaliseMode equals normal then the entire map can be generated at one knowing the min and max noiseheight values

                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
            }
        }
        return noiseMap;
    }


    [System.Serializable]
    public class NoiseSettings
    {
        public Noise.NormaliseMode normalizeMode;

        public float scale = 50;

        public int octaves = 6;
        [Range(0, 1)]
        public float persistance = .6f;
        public float lacunarity = 2;

        public int seed;
        public Vector2 offset;

        public void ValidateValues()
        {
            scale = Mathf.Max(scale, 0.01f);
            octaves = Mathf.Max(octaves, 1);
            lacunarity = Mathf.Max(lacunarity, 1);
            persistance = Mathf.Clamp01(persistance);
        }
    }
}