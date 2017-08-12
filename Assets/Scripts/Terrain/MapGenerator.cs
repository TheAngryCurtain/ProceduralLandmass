using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { Noise, Mesh, Falloff };

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;

    public Material terrainMaterial;

    [Range(0, MeshSettings.numSupportedLODs - 1)]
    [SerializeField] private int editorPreviewLOD;

    [SerializeField] private MapDisplay display;

    public bool AutoUpdate = true;
    public DrawMode drawMode = DrawMode.Noise;

    private Queue<MapThreadInfo<HeightMap>> threadMapInfoQueue = new Queue<MapThreadInfo<HeightMap>>();
    private Queue<MapThreadInfo<MeshData>> threadMeshInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    private float[,] falloffMap;

    private void Start()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);
    }

    private void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    private void OnTextureValueUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    public void RequestHeightMap(Vector2 center, Action<HeightMap> callback)
    {
        ThreadStart threadStart = delegate
        {
            HeightMapThread(center, callback);
        };

        new Thread(threadStart).Start();
    }

    private void HeightMapThread(Vector2 center, Action<HeightMap> callback)
    {
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.NumVertsPerLine, meshSettings.NumVertsPerLine, heightMapSettings, center);

        lock (threadMapInfoQueue)
        {
            threadMapInfoQueue.Enqueue(new MapThreadInfo<HeightMap>(callback, heightMap));
        }
    }

    public void RequestMeshData(HeightMap data, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(data, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    private void MeshDataThread(HeightMap data, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(data.Values, meshSettings, lod);

        lock (threadMeshInfoQueue)
        {
            threadMeshInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update()
    {
        if (threadMapInfoQueue.Count > 0)
        {
            for (int i = 0; i < threadMapInfoQueue.Count; i++)
            {
                MapThreadInfo<HeightMap> threadInfo = threadMapInfoQueue.Dequeue();
                threadInfo.Callback(threadInfo.Parameter);
            }
        }

        if (threadMeshInfoQueue.Count > 0)
        {
            for (int i = 0; i < threadMeshInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = threadMeshInfoQueue.Dequeue();
                threadInfo.Callback(threadInfo.Parameter);
            }
        }
    }

    public void DrawMapInEditor()
    {
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);
        HeightMap data = HeightMapGenerator.GenerateHeightMap(meshSettings.NumVertsPerLine, meshSettings.NumVertsPerLine, heightMapSettings, Vector2.zero);

        if (drawMode == DrawMode.Noise)
        {
            display.DrawTexture(TextureGenerator.GenerateTextureFromHeightMap(data.Values));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(data.Values, meshSettings, editorPreviewLOD));
        }
        else if (drawMode == DrawMode.Falloff)
        {
            display.DrawTexture(TextureGenerator.GenerateTextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.NumVertsPerLine)));
        }
    }

    private void OnValidate()
    {
        if (meshSettings != null)
        {
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (heightMapSettings != null)
        {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValueUpdated;
            textureData.OnValuesUpdated += OnTextureValueUpdated;
        }
    }

    private struct MapThreadInfo<T>
    {
        public readonly Action<T> Callback;
        public readonly T Parameter;

        public MapThreadInfo (Action<T> callback, T parameter)
        {
            Callback = callback;
            Parameter = parameter;
        }
    }
}
