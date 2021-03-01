using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateMap : MonoBehaviour
{

    public enum DrawMode {NoiseMap, ColorMap, Mesh};
    public DrawMode drawMode;

    public const int mapChunkSize = 241;
    [Range(0,6)]
    public int LOD;
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
    

    public bool autoUpdate = true;

    public void GenerateNewMap()
    {
        float[,] noiseMap = Noise.generateNoiseMap(mapChunkSize, mapChunkSize, noiseScale, octaves, persistance, lacunarity, seed, offset);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for (int x = 0; x < mapChunkSize; x++)
        {
            for (int y = 0; y < mapChunkSize; y++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        break;
                    }
                }
            }
        }
        //Draw the map the way the drawmode wants it to be drawn
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.texFromHeightMap(noiseMap,mapChunkSize,mapChunkSize));
        } else if(drawMode == DrawMode.ColorMap)
        {
            display.DrawTexture(TextureGenerator.texFromColorMap(colorMap, mapChunkSize, mapChunkSize));
        } else if(drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap,meshHeightMultiplier, meshHeightControl,LOD), TextureGenerator.texFromColorMap(colorMap, mapChunkSize, mapChunkSize));
        }

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
    }

    [System.Serializable]
    public struct TerrainTypes
    {
        public string name;
        public float height;
        public Color color;
    }

}
