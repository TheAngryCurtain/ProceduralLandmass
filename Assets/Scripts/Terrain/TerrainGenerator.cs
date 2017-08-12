using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int lod;
    public float distanceThreshold;

    public float SqrDistThreshold
    {
        get
        {
            return distanceThreshold * distanceThreshold;
        }
    }
}

public class TerrainGenerator : MonoBehaviour
{
    private const float viewerMoveForUpdateThreshold = 25f;
    private const float sqrViewerMoveForUpdateThreshold = viewerMoveForUpdateThreshold * viewerMoveForUpdateThreshold;

    [SerializeField] private Transform viewer;
    [SerializeField] private Material mapMaterial;
    [SerializeField] private LODInfo[] detailLevels;

    public int colliderLODIndex;

    public Vector2 previousViewerPosition;
    public Vector2 viewerPosition;

    public HeightMapSettings heightMapSettings;
    public MeshSettings meshSettings;
    public TextureData textureSettings;

    private float meshWorldSize;
    private int chunksVisibleInDistance;

    private Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    private void Start()
    {
        textureSettings.ApplyToMaterial(mapMaterial);
        textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);

        float maxViewDistance = detailLevels[detailLevels.Length - 1].distanceThreshold;
        meshWorldSize = meshSettings.MeshWorldSize;
        chunksVisibleInDistance = Mathf.RoundToInt(maxViewDistance / meshWorldSize);

        // run once to generate start chunks
        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if (viewerPosition != previousViewerPosition)
        {
            for (int i = 0; i < visibleTerrainChunks.Count; i++)
            {
                visibleTerrainChunks[i].UpdateCollisionMesh();
            }
        }

        if ((previousViewerPosition - viewerPosition).sqrMagnitude > sqrViewerMoveForUpdateThreshold)
        {
            previousViewerPosition = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    private void UpdateVisibleChunks()
    {
        HashSet<Vector2> updatedChunks = new HashSet<Vector2>();
        for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--)
        {
            updatedChunks.Add(visibleTerrainChunks[i].coordinate);
            visibleTerrainChunks[i].UpdateChunk();
        }

        int currentChunkXCoord = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
        int currentChunkYCoord = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

        for (int yOffset = -chunksVisibleInDistance; yOffset <= chunksVisibleInDistance; yOffset++)
        {
            for (int xOffset = -chunksVisibleInDistance; xOffset <= chunksVisibleInDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkXCoord + xOffset, currentChunkYCoord + yOffset);
                if (!updatedChunks.Contains(viewedChunkCoord))
                {
                    if (terrainChunkDict.ContainsKey(viewedChunkCoord))
                    {
                        terrainChunkDict[viewedChunkCoord].UpdateChunk();
                    }
                    else
                    {
                        TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
                        terrainChunkDict.Add(viewedChunkCoord, newChunk);

                        newChunk.OnVisibilityChanged += OnTerrainChunkVisibilityChanged;
                        newChunk.Load();
                    }
                }
            }
        }
    }

    private void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool visible)
    {
        if (visible)
        {
            visibleTerrainChunks.Add(chunk);
        }
        else
        {
            visibleTerrainChunks.Remove(chunk);
        }
    }
}
