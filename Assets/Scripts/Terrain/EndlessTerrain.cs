using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    public class TerrainChunk
    {
        public Vector2 position;
        public GameObject meshObject;
        public Bounds bounds;

        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;
        private LODInfo[] detailLevels;
        private LODMesh[] lodMeshes;
        private LODMesh collisionLODMesh;
        private MapData mapData;
        private bool mapDataReceived;
        private int previousLodIndex = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] lods, Transform parent, Material material)
        {
            detailLevels = lods;
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0f, position.y);

            meshObject = new GameObject("Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;

            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();

            meshObject.transform.position = positionV3 * mapGenerator.terrainData.uniformScale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale; ;

            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateChunk);
                if (detailLevels[i].useForCollider)
                {
                    collisionLODMesh = lodMeshes[i];
                }
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        private void OnMapDataReceived(MapData data)
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
                bool visible = viewerDistToNearestEdge <= maxViewDistance;

                if (visible)
                {
                    int lodDetailIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++) // don't have to check the last one, because visible will be false there
                    {
                        if (viewerDistToNearestEdge > detailLevels[i].distanceThreshold)
                        {
                            lodDetailIndex += 1;
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

                    // only set collsion mesh if the lod is 0 (player is in it)
                    if (lodDetailIndex == 0)
                    {
                        if (collisionLODMesh.hasMesh)
                        {
                            meshCollider.sharedMesh = collisionLODMesh.mesh;
                        }
                        else if (!collisionLODMesh.hasRequestedMesh)
                        {
                            collisionLODMesh.RequestMesh(mapData);
                        }
                    }

                    lastViewedChunks.Add(this);
                }

                SetVisible(visible);
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
        public int lod;
        public float distanceThreshold;
        public bool useForCollider;
    }

    public class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;

        private int lod;
        private System.Action UpdateCallback;

        public LODMesh(int levelOfDetail, System.Action updateCallback)
        {
            lod = levelOfDetail;
            UpdateCallback = updateCallback;
        }

        public void RequestMesh(MapData data)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(data, lod, OnMeshDataReceived);
        }

        private void OnMeshDataReceived(MeshData data)
        {
            mesh = data.CreateMesh();
            hasMesh = true;

            UpdateCallback();
        }
    }


    private const float viewerMoveForUpdateThreshold = 25f;
    private const float sqrViewerMoveForUpdateThreshold = viewerMoveForUpdateThreshold * viewerMoveForUpdateThreshold;

    [SerializeField] private Transform viewer;
    [SerializeField] private Material mapMaterial;
    [SerializeField] private LODInfo[] detailLevels;
    public static float maxViewDistance;

    public static Vector2 previousViewerPosition;
    public static Vector2 viewerPosition;
    public static MapGenerator mapGenerator;

    private int chunkSize;
    private int chunksVisibleInDistance;

    private Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
    private static List<TerrainChunk> lastViewedChunks = new List<TerrainChunk>();

    private void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>(); // gross

        maxViewDistance = detailLevels[detailLevels.Length - 1].distanceThreshold;
        chunkSize = mapGenerator.mapChunkSize - 1;
        chunksVisibleInDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);

        // run once to generate start chunks
        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / mapGenerator.terrainData.uniformScale;

        if ((previousViewerPosition - viewerPosition).sqrMagnitude > sqrViewerMoveForUpdateThreshold)
        {
            previousViewerPosition = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    private void UpdateVisibleChunks()
    {
        for (int i = 0; i < lastViewedChunks.Count; i++)
        {
            lastViewedChunks[i].SetVisible(false);
        }
        lastViewedChunks.Clear();

        int currentChunkXCoord = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkYCoord = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisibleInDistance; yOffset <= chunksVisibleInDistance; yOffset++)
        {
            for (int xOffset = -chunksVisibleInDistance; xOffset <= chunksVisibleInDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkXCoord + xOffset, currentChunkYCoord + yOffset);
                if (terrainChunkDict.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDict[viewedChunkCoord].UpdateChunk();
                }
                else
                {
                    terrainChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }
}
