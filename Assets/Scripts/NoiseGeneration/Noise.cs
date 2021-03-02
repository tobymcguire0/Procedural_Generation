using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public enum NormalizeMode {Local, Global };
    public static float[,] generateNoiseMap(int width, int height, float scale, int octaves, float persistance, float lacunarity, int seed, Vector2 offset, NormalizeMode normalizeMode) 
    {
        //Clamping the scale to never be less than 0
        scale = Mathf.Max(0.0001f, scale);
        float[,] noiseMap = new float[width, height];

        //Used for generating random noise, and to offset the noise
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        //Default values for noise
        float amplitude = 1;
        float frequency = 1;
        float maxPossibleHeight = 0;

        for(int i = 0; i<octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000)+offset.x;
            float offsetY = prng.Next(-100000, 100000)-offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
            maxPossibleHeight += amplitude;
            amplitude *= persistance;
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
                for(int i = 0; i<octaves; i++)
                {
                    //Generating an x and y point based on the random offset, noise scale, and frequency
                    float sampleX = ((x-halfWidth + octaveOffsets[i].x) / scale * frequency) ;
                    float sampleY = ((y-halfHeight + octaveOffsets[i].y) / scale * frequency) ;

                    //Using the points to get a perlin noise value, and multiplying it by the amplitude of this octave
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY)*2-1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }
                //Storing max and min noise height
                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }else if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }
                noiseMap[x, y] = noiseHeight;
            }
        }
        //Normalize the noise map using the maximum and minimum noise values
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if(normalizeMode == NormalizeMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight/1.6f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight,0,int.MaxValue);
                }
                
            }
        }
        return noiseMap;
    }
}
