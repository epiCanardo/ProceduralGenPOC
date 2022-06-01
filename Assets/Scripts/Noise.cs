using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static float[,] GenerateNoiseMap(int mapchunkSize, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset)
    {
        float[,] noiseMap = new float[mapchunkSize, mapchunkSize];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        if (scale <= 0) scale = 0.0001f;

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        // utilisation du centre de la map pour les générations
        float halfWidth = mapchunkSize / 2;
        float halfHeight = mapchunkSize / 2;

        for (int y = 0; y < mapchunkSize; y++)
        {
            for (int x = 0; x < mapchunkSize; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;
                
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = ((x - halfWidth) / scale) * frequency + octaveOffsets[i].x;
                    float sampleY = ((y - halfHeight) / scale) * frequency + octaveOffsets[i].y;

                    // clamp -1;1
                    float perlinValue = (Mathf.PerlinNoise(sampleX, sampleY) * 2) - 1;

                    noiseHeight += perlinValue * amplitude;

                    // variations des amplitude et fréquence entre les octaves
                    amplitude *= persistance;
                    frequency *= lacunarity;                    
                }

                // normalisation sur 0;1
                if (noiseHeight > maxNoiseHeight)
                    maxNoiseHeight = noiseHeight;
                else if (noiseHeight < minNoiseHeight)
                    minNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < mapchunkSize; y++)
        {
            for (int x = 0; x < mapchunkSize; x++)
            {
                // redéfinition de la valeur : 0 si minNoiseHeight, 1 si maxNoiseHeight
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }

        return noiseMap;
    }
}
