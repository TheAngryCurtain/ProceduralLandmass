using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float maxHeight;
    public Color color;
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] noise, Color[] color)
    {
        heightMap = noise;
        colorMap = color;
    }
}

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { Noise, Color, Mesh, Falloff };

    public const int mapChunkSize = 239; // don't change this. it needs to be divisible by mesh simplification increments, and account for the borders used for mesh normals

    [Range(0,6)]
    [SerializeField] private int editorPreviewLOD;

    [SerializeField] private int seed = 0;
    [SerializeField] private float mapScale = 1;
    [SerializeField] private int octaves = 4;

    [Range(0, 1)]
    [SerializeField] private float persistence = 0.5f;
    [SerializeField] private float lacunarity = 2f;
    [SerializeField] private Vector2 offset;

    [SerializeField] private TerrainType[] regions;

    [SerializeField] private MapDisplay display;
    [SerializeField] private float MeshHeightModifier = 1f;
    [SerializeField] private AnimationCurve meshHeightCurve;

    public bool useFalloff = true;
    public bool AutoUpdate = true;
    public DrawMode drawMode = DrawMode.Noise;
    public Noise.NormalizeMode noiseMode = Noise.NormalizeMode.Global;

    private Queue<MapThreadInfo<MapData>> threadMapInfoQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> threadMeshInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    private float[,] falloffMap;

    private void Awake()
    {
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    private MapData GenerateMapData(Vector2 center)
    {
        // the + 2 is to allow for the border around the mesh
        float[,] noiseMap = Noise.GenerateNoiseMap(seed, mapChunkSize + 2, mapChunkSize + 2, mapScale, octaves, persistence, lacunarity, center + offset, noiseMode);
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                if (useFalloff)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }

                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight >= regions[i].maxHeight)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colorMap);
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }

    private void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);

        lock (threadMapInfoQueue)
        {
            threadMapInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData data, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(data, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    private void MeshDataThread(MapData data, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(data.heightMap, MeshHeightModifier, meshHeightCurve, lod);

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
                MapThreadInfo<MapData> threadInfo = threadMapInfoQueue.Dequeue();
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
        MapData data = GenerateMapData(Vector2.zero);

        if (drawMode == DrawMode.Noise)
        {
            display.DrawTexture(TextureGenerator.GenerateTextureFromHeightMap(data.heightMap));
        }
        else if (drawMode == DrawMode.Color)
        {
            display.DrawTexture(TextureGenerator.GenerateTextureFromColorMap(data.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(
                MeshGenerator.GenerateTerrainMesh(data.heightMap, MeshHeightModifier, meshHeightCurve, editorPreviewLOD),
                TextureGenerator.GenerateTextureFromColorMap(data.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Falloff)
        {
            display.DrawTexture(TextureGenerator.GenerateTextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
        }
    }

    private void OnValidate()
    {
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;
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
