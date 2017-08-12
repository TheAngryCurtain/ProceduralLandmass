using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk
{
    public event System.Action<TerrainChunk, bool> OnVisibilityChanged;

    private const float colliderGenerationDistThreshold = 5f;

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
    private HeightMap heightMapData;
    private bool heightMapDataReceived;
    private int previousLodIndex = -1;
    private bool hasSetCollider = false;
    private float maxViewDist;

    private HeightMapSettings heightMapSettings;
    private MeshSettings meshSettings;
    private Transform viewerTransform;

    public TerrainChunk(Vector2 coord, HeightMapSettings hSettings, MeshSettings mSettings, LODInfo[] lods, int colliderIndex, Transform parent, Transform viewer, Material material)
    {
        detailLevels = lods;
        colliderLODIndex = colliderIndex;
        coordinate = coord;
        sampleCenter = coord * mSettings.MeshWorldSize / mSettings.meshScale;
        Vector2 position = coord * mSettings.MeshWorldSize;
        bounds = new Bounds(position, Vector2.one * mSettings.MeshWorldSize);

        heightMapSettings = hSettings;
        meshSettings = mSettings;
        viewerTransform = viewer;

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

        maxViewDist = detailLevels[detailLevels.Length - 1].distanceThreshold;
    }

    public void Load()
    {
        // RequestData needs a function that returns an object and has no parameters, so we use a lambda expression to get around that
        ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.NumVertsPerLine, meshSettings.NumVertsPerLine, heightMapSettings, sampleCenter), OnHeightMapReceived);
    }

    private Vector2 viewerPosition
    {
        get { return new Vector2(viewerTransform.position.x, viewerTransform.position.z); }
    }

    private void OnHeightMapReceived(object data)
    {
        heightMapData = (HeightMap)data;
        heightMapDataReceived = true;

        UpdateChunk();
    }

    public void UpdateChunk()
    {
        if (heightMapDataReceived)
        {
            float viewerDistToNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool wasVisible = IsVisible();
            bool visible = viewerDistToNearestEdge <= maxViewDist;

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
                        lod.RequestMesh(heightMapData, meshSettings);
                    }
                }

            }

            if (wasVisible != visible)
            {
                SetVisible(visible);
                if (OnVisibilityChanged != null)
                {
                    OnVisibilityChanged(this, visible);
                }
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
                    lodMeshes[colliderLODIndex].RequestMesh(heightMapData, meshSettings);
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

    public void RequestMesh(HeightMap data, MeshSettings settings)
    {
        hasRequestedMesh = true;
        ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(data.Values, settings, lod), OnMeshDataReceived);
    }

    private void OnMeshDataReceived(object data)
    {
        mesh = ((MeshData)data).CreateMesh();
        hasMesh = true;

        if (UpdateCallback != null)
        {
            UpdateCallback();
        }
    }
}
