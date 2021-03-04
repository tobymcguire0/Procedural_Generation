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

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;

    public Material terrainMaterial;

    [Range(0, MeshSettings.levelOfSupportedLOD - 1)]
    public int previewLOD;

    




    Queue<MapThreadInfo<HeightMap>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<HeightMap>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public bool autoUpdate = true;


    //Chunk size differs if the map is flat shaded, because flat shading generates many more verticies
    

    private void Start()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
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
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        //Calls on the MapDisplay to draw the selected type of map
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertPerLine,meshSettings.numVertPerLine,heightMapSettings, Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.texFromHeightMap(heightMap.values));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values,meshSettings,previewLOD));
        } else if(drawMode == DrawMode.FalloffMap)
        {
            display.DrawTexture(TextureGenerator.texFromHeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVertPerLine)));
        }
    }

    public void RequestHeightMap(Vector2 center, Action<HeightMap> callback)
    {
        //Creates a new thread to generate and send map data to the caller
        ThreadStart threadStart = delegate
        {
            HeightMapThread(center, callback);
        };
        new Thread(threadStart).Start();
    }

    void HeightMapThread(Vector2 center,Action<HeightMap> callback)
    {
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertPerLine, meshSettings.numVertPerLine, heightMapSettings, center);
        lock (mapDataThreadInfoQueue) //Only one thread can access this line of code at a time
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<HeightMap>(callback, heightMap));
        }
        
    }

    
    public void RequestMeshData(HeightMap heightMap, int lod, Action<MeshData> callback)
    {
        //Creates a new thread to generate and send mesh data to the caller
        ThreadStart threadStart = delegate
        {
            MeshDataThread(heightMap,lod,callback);
        };
        new Thread(threadStart).Start();
    }

    void MeshDataThread(HeightMap heightMap, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings,lod);
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
                MapThreadInfo<HeightMap> threadInfo = mapDataThreadInfoQueue.Dequeue();
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


    
    private void OnValidate()
    {      
        //Make the GenerateMap method OnValuesUpdated a subscriber to the event in UpdateableData
        if(meshSettings != null)
        {
            meshSettings.OnValuesUpdated -= OnValuesUpdated; //Make sure not to have it subscribe multiple times
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (heightMapSettings != null)
        {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
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

