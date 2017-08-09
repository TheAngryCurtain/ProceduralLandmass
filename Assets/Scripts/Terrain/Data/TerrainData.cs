using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData
{
    public float MeshHeightModifier = 1f;
    public AnimationCurve meshHeightCurve;
    public bool useFalloff = true;
    public bool useFlatShading = false;

    public float uniformScale = 1f;

    public float MinHeight
    {
        get
        {
            return uniformScale * MeshHeightModifier * meshHeightCurve.Evaluate(0);
        }
    }

    public float MaxHeight
    {
        get
        {
            return uniformScale * MeshHeightModifier * meshHeightCurve.Evaluate(1);
        }
    }
}
