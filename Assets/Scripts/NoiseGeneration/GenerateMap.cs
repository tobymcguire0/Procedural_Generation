using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class GenerateMap : MonoBehaviour
{

    public enum DrawMode {NoiseMap, Mesh, FalloffMap};
    public DrawMode drawMode;
    
    float[,] falloffMap;

    public TerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;

    public Material terrainMaterial;

    [Range(0,6)]
    public int previewLOD;


    

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public bool autoUpdate = true;


    //Chunk size differs if the map is flat shaded, because flat shading generates many more verticies
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

    private void Awake()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    public void DrawMapInEditor()
    {
        //Updates heights in the main thread
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
        //Calls on the MapDisplay to draw the selected type of map
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.texFromHeightMap(mapData.heightMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightControl, previewLOD, terrainData.useFlatShading));
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
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightControl, lod, terrainData.useFlatShading);
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
        //Generates and prints a noise map to act as a height map or just stay a noise map
        float[,] noiseMap = Noise.generateNoiseMap(mapChunkSize+2, mapChunkSize+2, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, noiseData.seed, center+ noiseData.offset, noiseData.normalizeMode);
        if (terrainData.useFalloff)
        {
            if(falloffMap == null)
            {
                falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);
            }
            for (int x = 0; x < mapChunkSize+2; x++)
            {
                for (int y = 0; y < mapChunkSize+2; y++)
                {
                    if (terrainData.useFalloff)
                    {
                        noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                    }
                }
            }
        }
        
        return new MapData(noiseMap);

    }

    
    private void OnValidate()
    {      
        //Make the GenerateMap method OnValuesUpdated a subscriber to the event in UpdateableData
        if(terrainData != null)
        {
            terrainData.OnValuesUpdated -= OnValuesUpdated; //Make sure not to have it subscribe multiple times
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }
        if (noiseData != null)
        {
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
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
//Holds important imformation about the height and color of the map
public struct MapData
{
    public readonly float[,] heightMap;
    public MapData(float[,] height)
    {
        heightMap = height;
    }
}