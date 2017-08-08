using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public enum NormalizeMode { Local, Global }; // local for single chunk maps (know the min/max), global for multi-chunk (estimate min/max)

    public static float[,] GenerateNoiseMap(int seed, int width, int height, float scale, int octaves, float persistence, float lacunarity, Vector2 offset, NormalizeMode mode)
    {
        float[,] noiseMap = new float[width, height];
        System.Random prng = new System.Random(seed);

        float maxPossibleHeight = 0;
        float amplitude = 1f;
        float frequency = 1f;

        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;

            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            // used to estimate max possible height
            maxPossibleHeight += amplitude;
            amplitude *= persistence;
        }

        if (scale <= 0)
        {
            scale = 0.001f;
        }

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                amplitude = 1f;
                frequency = 1f;

                float noiseHeight = 0;

                // go over each octave
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; // move this into the range of -1 to 1
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence; // range 0 - 1, decreases each octave
                    frequency *= lacunarity;  // increases each octave, should be greater than 1
                }

                // update min and max
                if (noiseHeight > maxLocalNoiseHeight) maxLocalNoiseHeight = noiseHeight;
                else if (noiseHeight < minLocalNoiseHeight) minLocalNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }

        // normalize noise
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (mode == NormalizeMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / maxPossibleHeight; // need to reverse the range change from line 57
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }

        return noiseMap;
    }
}
