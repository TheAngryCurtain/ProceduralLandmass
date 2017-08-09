using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public struct MapData
{
    public readonly float[,] heightMap;

    public MapData(float[,] noise)
    {
        heightMap = noise;
    }
}

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { Noise, Mesh, Falloff };

    public TerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;

    public Material terrainMaterial;

    [Range(0,6)]
    [SerializeField] private int editorPreviewLOD;

    [SerializeField] private MapDisplay display;

    public bool AutoUpdate = true;
    public DrawMode drawMode = DrawMode.Noise;

    private Queue<MapThreadInfo<MapData>> threadMapInfoQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> threadMeshInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    private float[,] falloffMap;

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

    public int mapChunkSize
    {
        get
        {
            if (terrainData.useFlatShading)
            {
                return 95;
            }
            else
            {
                return 239;
            }
        }
    }

    private MapData GenerateMapData(Vector2 center)
    {
        // the + 2 is to allow for the border around the mesh
        int mapChunkBorderSize = mapChunkSize + 2;
        float[,] noiseMap = Noise.GenerateNoiseMap(noiseData.seed, mapChunkBorderSize, mapChunkBorderSize, noiseData.mapScale, noiseData.octaves, noiseData.persistence, noiseData.lacunarity, center + noiseData.offset, noiseData.normalizeMode);

        if (terrainData.useFalloff)
        {
            if (falloffMap == null)
            {
                falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkBorderSize);
            }

            for (int y = 0; y < mapChunkBorderSize; y++)
            {
                for (int x = 0; x < mapChunkBorderSize; x++)
                {
                    if (terrainData.useFalloff)
                    {
                        noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                    }
                }
            }
        }

        textureData.UpdateMeshHeights(terrainMaterial, terrainData.MinHeight, terrainData.MaxHeight);

        return new MapData(noiseMap);
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
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(data.heightMap, terrainData.MeshHeightModifier, terrainData.meshHeightCurve, lod, terrainData.useFlatShading);

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
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(data.heightMap, terrainData.MeshHeightModifier, terrainData.meshHeightCurve, editorPreviewLOD, terrainData.useFlatShading));
        }
        else if (drawMode == DrawMode.Falloff)
        {
            display.DrawTexture(TextureGenerator.GenerateTextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
        }
    }

    private void OnValidate()
    {
        if (terrainData != null)
        {
            terrainData.OnValuesUpdated -= OnValuesUpdated;
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }

        if (noiseData != null)
        {
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
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
