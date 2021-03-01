using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateMap : MonoBehaviour
{

    public enum DrawMode {NoiseMap, ColorMap, Mesh};
    public DrawMode drawMode;
    public int width;
    public int height;
    public float noiseScale;
    public int octaves;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;
    public int seed;
    public Vector2 offset;
    public float meshHeightMultiplier;
    public TerrainTypes[] regions;
    

    public bool autoUpdate = true;

    public void GenerateNewMap()
    {
        float[,] noiseMap = Noise.generateNoiseMap(width, height, noiseScale, octaves, persistance, lacunarity, seed, offset);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        Color[] colorMap = new Color[width * height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * width + x] = regions[i].color;
                        break;
                    }
                }
            }
        }
        //Draw the map the way the drawmode wants it to be drawn
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.texFromHeightMap(noiseMap,width,height));
        } else if(drawMode == DrawMode.ColorMap)
        {
            display.DrawTexture(TextureGenerator.texFromColorMap(colorMap, width, height));
        } else if(drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap,meshHeightMultiplier), TextureGenerator.texFromColorMap(colorMap, width, height));
        }

    }

    //Keeping data within acceptable bounds
    private void OnValidate()
    {
        if (width < 1)
        {
            width = 1;
        }
        if(height < 1)
        {
            height = 1;
        }
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
