using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class GenerateMap : MonoBehaviour
{

    public enum DrawMode {NoiseMap, ColorMap, Mesh, FalloffMap};
    public DrawMode drawMode;
    public bool useFalloff;
    float[,] falloffMap;


    public Noise.NormalizeMode normalizeMode;
    public const int mapChunkSize = 239;
    [Range(0,6)]
    public int previewLOD;
    public float noiseScale;

    public int octaves;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public AnimationCurve meshHeightControl;
    public float meshHeightMultiplier;
    public TerrainTypes[] regions;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public bool autoUpdate = true;

    private void Awake()
    {
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    public void DrawMapInEditor()
    {
        //Calls on the MapDisplay to draw the selected type of map
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.texFromHeightMap(mapData.heightMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.ColorMap)
        {
            display.DrawTexture(TextureGenerator.texFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightControl, previewLOD), TextureGenerator.texFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        } else if(drawMode == DrawMode.FalloffMap)
        {
            display.DrawTexture(TextureGenerator.texFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize),mapChunkSize,mapChunkSize));
        }
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        //Creates a new thread to generate and send map data to the caller
        ThreadStart threadStart = delegate
        {
            MapDataThread(center, callback);
        };
        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center,Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);
        lock (mapDataThreadInfoQueue) //Only one thread can access this line of code at a time
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
        
    }

    
    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        //Creates a new thread to generate and send mesh data to the caller
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData,lod,callback);
        };
        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap,meshHeightMultiplier,meshHeightControl,lod);
        lock (meshDataThreadInfoQueue) //Only one thread can access this line of code at a time
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }

    }

    private void Update()
    {
        //Used as an endpoint for all threads, where the data they passed into their thread info queue is sent to the respective caller
        if (mapDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    MapData GenerateMapData(Vector2 center)
    {
        //Assigns colors for each noise value based on the regions setup in the editor
        float[,] noiseMap = Noise.generateNoiseMap(mapChunkSize+2, mapChunkSize+2, noiseScale, octaves, persistance, lacunarity, seed, center+offset, normalizeMode);
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for (int x = 0; x < mapChunkSize; x++)
        {
            for (int y = 0; y < mapChunkSize; y++)
            {
                if (useFalloff)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight >= regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        
                    } else
                    {
                        break;
                    }
                }
            }
        }
        return new MapData(noiseMap, colorMap);

    }

    //Keeping data within acceptable bounds
    private void OnValidate()
    {
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        if (octaves < 0)
        {
            octaves = 0;
        }
        if (meshHeightMultiplier < 0)
        {
            meshHeightMultiplier = 0;
        }
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);


    }
    //Holds generic information to be sent to Update so it can be called back on the main thread instead of seperate threads
    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;
        public MapThreadInfo(Action<T> call, T param)
        {
            callback = call;
            parameter = param;
        }
    }



}
//Holds data for the different colors and heights of terrain for detail
[System.Serializable]
public struct TerrainTypes
{
    public string name;
    public float height;
    public Color color;
}
//Holds important imformation about the height and color of the map
public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;
    public MapData(float[,] height, Color[] colors)
    {
        heightMap = height;
        colorMap = colors;
    }
}