using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdatableData
{
    public int seed = 0;
    public float mapScale = 1;
    public int octaves = 4;

    [Range(0, 1)]
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public Vector2 offset;

    public Noise.NormalizeMode normalizeMode = Noise.NormalizeMode.Global;

    protected override void OnValidate()
    {
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;

        base.OnValidate();
    }
}
