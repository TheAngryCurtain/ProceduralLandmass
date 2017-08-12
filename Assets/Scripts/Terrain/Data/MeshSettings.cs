using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class MeshSettings : UpdatableData
{
    public bool useFlatShading = false;
    public float meshScale = 1f;

    [Range(0, numSupportedChunkSizes - 1)]
    public int chunkSizeIndex = 0;

    [Range(0, numSupportedFlatShadedChunkSizes - 1)]
    public int flatShadedChunkSizeIndex = 0;

    public const int numSupportedLODs = 5;
    public const int numSupportedChunkSizes = 9;
    public const int numSupportedFlatShadedChunkSizes = 3;
    public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

    // number of vertices per line of mesh rendered with LOD = 0. Includes the extra 2 verts that are excluded from the final mesh, but used for calculating normals
    public int NumVertsPerLine
    {
        get
        {
            return supportedChunkSizes[(useFlatShading ? flatShadedChunkSizeIndex : chunkSizeIndex)] + 1;
        }
    }

    public float MeshWorldSize
    {
        get
        {
            return (NumVertsPerLine - 3) * meshScale;
        }
    }
}
