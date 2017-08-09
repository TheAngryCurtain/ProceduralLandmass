using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FalloffGenerator
{
    public static float[,] GenerateFalloffMap(int size)
    {
        float[,] map = new float[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                float x = i / (float)size * 2 - 1; // move range from 0 to 1 to -1 to 1
                float y = j / (float)size * 2 - 1; // move range from 0 to 1 to -1 to 1

                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i, j] = Evaluate(value);
            }
        }

        return map;
    }

    // creates a curve to control how much falloff happens with the falloff map
    private static float Evaluate(float value)
    {
        float a = 3f;
        float b = 2.2f;

        float valueToPowA = Mathf.Pow(value, a);
        return valueToPowA / (valueToPowA + Mathf.Pow(b - b * value, a));
    }
}
