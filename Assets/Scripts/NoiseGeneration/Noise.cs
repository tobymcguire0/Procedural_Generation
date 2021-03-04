using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public enum NormalizeMode {Local, Global };
    public static float[,] generateNoiseMap(int width, int height, NoiseSettings settings, Vector2 sampleCenter) 
    {
        float[,] noiseMap = new float[width, height];

        //Used for generating random noise, and to offset the noise
        System.Random prng = new System.Random(settings.seed);
        Vector2[] octaveOffsets = new Vector2[settings.octaves];

        //Default values for noise
        float amplitude = 1;
        float frequency = 1;
        float maxPossibleHeight = 0;

        for(int i = 0; i< settings.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000)+ settings.offset.x + sampleCenter.x;
            float offsetY = prng.Next(-100000, 100000)- settings.offset.y - sampleCenter.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
            maxPossibleHeight += amplitude;
            amplitude *= settings.persistance;
        }

        //Store the max and min generated noise values to interpolate the noise between 0 and 1 later
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        //Get the center of the texture so scaling starts from the midpoint and not a cornet
        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        //Loop through each pixel on the grid
        for(int x = 0; x<width; x++)
        {
            for(int y = 0; y<height; y++)
            {
                //Resetting the noise variables
                frequency = 1;
                amplitude = 1;
                float noiseHeight = 0;

                //For each octave of noise, generate noise then update amp, freq for next octave
                for(int i = 0; i< settings.octaves; i++)
                {
                    //Generating an x and y point based on the random offset, noise scale, and frequency
                    float sampleX = ((x-halfWidth + octaveOffsets[i].x) / settings.scale * frequency) ;
                    float sampleY = ((y-halfHeight + octaveOffsets[i].y) / settings.scale * frequency) ;

                    //Using the points to get a perlin noise value, and multiplying it by the amplitude of this octave
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY)*2-1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= settings.persistance;
                    frequency *= settings.lacunarity;
                }
                //Storing max and min noise height
                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }
                if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }
                noiseMap[x, y] = noiseHeight;
                if (settings.normalizeMode == NormalizeMode.Global)
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight / 1.6f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }

        if (settings.normalizeMode == NormalizeMode.Local)
        {
            //Normalize the noise map using the maximum and minimum noise values
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
            }
        }
        
        return noiseMap;
    }
}

[System.Serializable]
public class NoiseSettings
{
    public Noise.NormalizeMode normalizeMode;
    public float scale = 50;
    public int octaves = 6;
    [Range(0, 1)]
    public float persistance = .5f;
    public float lacunarity = 2;
    public int seed;
    public Vector2 offset;

    public void ValidateValues()
    {
        scale = Mathf.Max(0.01f, scale);
        persistance = Mathf.Clamp(persistance, 0, 1);
        lacunarity = Mathf.Max(1, lacunarity);
        octaves = Mathf.Max(1, octaves);

    }
}
