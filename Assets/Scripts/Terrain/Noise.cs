using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoiseSettings
{
    public int seed = 0;
    public float scale = 50;
    public int octaves = 6;

    [Range(0, 1)]
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public Vector2 offset;

    public Noise.NormalizeMode normalizeMode = Noise.NormalizeMode.Global;

    public void ValidateValues()
    {
        scale = Mathf.Max(scale, 0.01f);
        octaves = Mathf.Max(octaves, 1);
        lacunarity = Mathf.Max(lacunarity, 1);
        persistence = Mathf.Clamp01(persistence);
    }
}

public static class Noise
{
    public enum NormalizeMode { Local, Global }; // local for single chunk maps (know the min/max), global for multi-chunk (estimate min/max)

    public static float[,] GenerateNoiseMap(int width, int height, NoiseSettings settings, Vector2 sampleCenter)
    {
        float[,] noiseMap = new float[width, height];
        System.Random prng = new System.Random(settings.seed);

        float maxPossibleHeight = 0;
        float amplitude = 1f;
        float frequency = 1f;

        Vector2[] octaveOffsets = new Vector2[settings.octaves];
        for (int i = 0; i < settings.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + settings.offset.x + sampleCenter.x;
            float offsetY = prng.Next(-100000, 100000) - settings.offset.y - sampleCenter.y;

            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            // used to estimate max possible height
            maxPossibleHeight += amplitude;
            amplitude *= settings.persistence;
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
                for (int i = 0; i < settings.octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / settings.scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / settings.scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; // move this into the range of -1 to 1
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= settings.persistence; // range 0 - 1, decreases each octave
                    frequency *= settings.lacunarity;  // increases each octave, should be greater than 1
                }

                // update min and max
                if (noiseHeight > maxLocalNoiseHeight) maxLocalNoiseHeight = noiseHeight;
                if (noiseHeight < minLocalNoiseHeight) minLocalNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;

                if (settings.normalizeMode == NormalizeMode.Global)
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight / 0.9f); // need to reverse the range change from line 57
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }

        // normalize noise
        if (settings.normalizeMode == NormalizeMode.Local)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
            }
        }

        return noiseMap;
    }
}
