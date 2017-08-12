using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    public class TerrainChunk
    {
        public Vector2 coordinate;
        private Vector2 sampleCenter;
        private GameObject meshObject;
        private Bounds bounds;

        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;
        private LODInfo[] detailLevels;
        private LODMesh[] lodMeshes;
        private int colliderLODIndex;
        private HeightMap mapData;
        private bool mapDataReceived;
        private int previousLodIndex = -1;
        private bool hasSetCollider = false;

        public TerrainChunk(Vector2 coord, float meshWorldSize, LODInfo[] lods, int colliderIndex, Transform parent, Material material)
        {
            detailLevels = lods;
            colliderLODIndex = colliderIndex;
            coordinate = coord;
            sampleCenter = coord * meshWorldSize / mapGenerator.meshSettings.meshScale;
            Vector2 position = coord * meshWorldSize;
            bounds = new Bounds(position, Vector2.one * meshWorldSize);

            meshObject = new GameObject("Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;

            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();

            meshObject.transform.position = new Vector3(position.x, 0, position.y);
            meshObject.transform.parent = parent;

            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod);
                lodMeshes[i].UpdateCallback += UpdateChunk;

                if (i == colliderLODIndex)
                {
                    lodMeshes[i].UpdateCallback += UpdateCollisionMesh;
                }
            }

            mapGenerator.RequestHeightMap(sampleCenter, OnMapDataReceived);
        }

        private void OnMapDataReceived(HeightMap data)
        {
            mapData = data;
            mapDataReceived = true;

            UpdateChunk();
        }

        public void UpdateChunk()
        {
            if (mapDataReceived)
            {
                float viewerDistToNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool wasVisible = IsVisible();
                bool visible = viewerDistToNearestEdge <= maxViewDistance;

                if (visible)
                {
                    int lodDetailIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++) // don't have to check the last one, because visible will be false there
                    {
                        if (viewerDistToNearestEdge > detailLevels[i].distanceThreshold)
                        {
                            lodDetailIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lodDetailIndex != previousLodIndex)
                    {
                        LODMesh lod = lodMeshes[lodDetailIndex];
                        if (lod.hasMesh)
                        {
                            previousLodIndex = lodDetailIndex;
                            meshFilter.mesh = lod.mesh;
                        }
                        else if (!lod.hasRequestedMesh)
                        {
                            lod.RequestMesh(mapData);
                        }
                    }

                }

                if (wasVisible != visible)
                {
                    if (visible)
                    {
                        visibleTerrainChunks.Add(this);
                    }
                    else
                    {
                        visibleTerrainChunks.Remove(this);
                    }

                    SetVisible(visible);
                }
            }
        }

        public void UpdateCollisionMesh()
        {
            if (!hasSetCollider)
            {
                float sqrDistToEdge = bounds.SqrDistance(viewerPosition);
                if (sqrDistToEdge < detailLevels[colliderLODIndex].SqrDistThreshold)
                {
                    if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
                    {
                        lodMeshes[colliderLODIndex].RequestMesh(mapData);
                    }
                }

                if (sqrDistToEdge < colliderGenerationDistThreshold * colliderGenerationDistThreshold)
                {
                    if (lodMeshes[colliderLODIndex].hasMesh)
                    {
                        meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                        hasSetCollider = true;
                    }
                }
            }
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

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

    public class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;

        private int lod;
        public event System.Action UpdateCallback;

        public LODMesh(int levelOfDetail)
        {
            lod = levelOfDetail;
        }

        public void RequestMesh(HeightMap data)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(data, lod, OnMeshDataReceived);
        }

        private void OnMeshDataReceived(MeshData data)
        {
            mesh = data.CreateMesh();
            hasMesh = true;

            if (UpdateCallback != null)
            {
                UpdateCallback();
            }
        }
    }
    

    private const float viewerMoveForUpdateThreshold = 25f;
    private const float sqrViewerMoveForUpdateThreshold = viewerMoveForUpdateThreshold * viewerMoveForUpdateThreshold;
    private const float colliderGenerationDistThreshold = 5f;

    [SerializeField] private Transform viewer;
    [SerializeField] private Material mapMaterial;
    [SerializeField] private LODInfo[] detailLevels;
    public static float maxViewDistance;

    public int colliderLODIndex;

    public static Vector2 previousViewerPosition;
    public static Vector2 viewerPosition;
    public static MapGenerator mapGenerator;

    private float meshWorldSize;
    private int chunksVisibleInDistance;

    private Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
    private static List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    private void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>(); // gross

        maxViewDistance = detailLevels[detailLevels.Length - 1].distanceThreshold;
        meshWorldSize = mapGenerator.meshSettings.MeshWorldSize;
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
                        terrainChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, meshWorldSize, detailLevels, colliderLODIndex, transform, mapMaterial));
                    }
                }
            }
        }
    }
}
