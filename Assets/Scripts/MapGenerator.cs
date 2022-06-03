using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColourMap, Mesh };
    public DrawMode drawMode;
    public FilterMode filterMode;

    public const int mapchunkSize = 241;

    [Range(0, 6)]
    public int levelOfDetailReduction;
    public float noiseScale;

    // détails
    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;

    // random
    public int seed;
    public Vector2 offset;
    public float meshHeightMultiplier;

    // lissage des valeurs;
    public AnimationCurve meshHeightCurve; 

    public bool autoUpdate;

    public TerrainType[] regions;

    Queue<GenericThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<GenericThreadInfo<MapData>>();
    Queue<GenericThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<GenericThreadInfo<MeshData>>();

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData();
        
        switch (drawMode)
        {
            case DrawMode.NoiseMap:
                transform.GetComponent<MapDisplay>().DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap, filterMode));
                break;
            case DrawMode.ColourMap:
                transform.GetComponent<MapDisplay>().DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapchunkSize, mapchunkSize, filterMode));
                break;
            case DrawMode.Mesh:
                transform.GetComponent<MapDisplay>().DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetailReduction), 
                    TextureGenerator.TextureFromColourMap(mapData.colourMap, mapchunkSize, mapchunkSize, filterMode));
                break;
            default:
                break;
        }
    }

    public void RequestMapData(Action<MapData> callback)
    {
        // exécution de MapDataThread sur un nouveau Thread
        ThreadStart thread = delegate { MapDataThread(callback); };
        new Thread(thread).Start();
    }

    public void MapDataThread(Action<MapData> callback)
    {
        MapData mapData = GenerateMapData();

        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new GenericThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, Action<MeshData> callback)
    {
        // exécution de MapDataThread sur un nouveau Thread
        ThreadStart thread = delegate { MeshDataThread(mapData, callback); };
        new Thread(thread).Start();
    }

    public void MeshDataThread(MapData mapData, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetailReduction);

        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new GenericThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update()
    {
        lock (mapDataThreadInfoQueue)
        {
            if (mapDataThreadInfoQueue.Any())
            {

                for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
                {
                    GenericThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                    threadInfo.callback(threadInfo.parameter);
                }
            }
        }

        lock (meshDataThreadInfoQueue)
        {
            if (meshDataThreadInfoQueue.Any())
            {

                for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
                {
                    GenericThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                    threadInfo.callback(threadInfo.parameter);
                }
            }
        }
    }

    MapData GenerateMapData()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapchunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

        Color[] colourMap = new Color[mapchunkSize * mapchunkSize];
        // application des couleurs en fonction des la hauteur
        for (int y = 0; y < mapchunkSize; y++)
        {
            for (int x = 0; x < mapchunkSize; x++)
            {
                float currentHeight = noiseMap[x, y]; // la hauteur actuelle
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colourMap[y * mapchunkSize + x] = regions[i].colour;
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colourMap);
    }

    private void OnValidate()
    {
        if (lacunarity < 1)
            lacunarity = 1;

        if (octaves < 1)
            octaves = 1;
    }

    struct GenericThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;
        
        public GenericThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colourMap;

    public MapData(float[,] heightMap, Color[] colourMap)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}