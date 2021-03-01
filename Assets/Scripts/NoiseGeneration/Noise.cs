using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static float[,] generateNoiseMap(int width, int height, float scale, int octaves, float persistance, float lacunarity, int seed, Vector2 offset) 
    {
        //Clamping the scale to never be less than 0
        scale = Mathf.Max(0.0001f, scale);
        float[,] noiseMap = new float[width, height];

        //Used for generating random noise, and to offset the noise
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for(int i = 0; i<octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000)+offset.x;
            float offsetY = prng.Next(-100000, 100000)+offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        //Store the max and min generated noise values to interpolate the noise between 0 and 1 later
        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        //Get the center of the texture so scaling starts from the midpoint and not a cornet
        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        //Loop through each pixel on the grid
        for(int x = 0; x<width; x++)
        {
            for(int y = 0; y<height; y++)
            {
                //Default values for noise
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                //For each octave of noise, generate noise then update amp, freq for next octave
                for(int i = 0; i<octaves; i++)
                {
                    //Generating an x and y point based on the random offset, noise scale, and frequency
                    float sampleX = ((x-halfWidth) / scale * frequency) + octaveOffsets[i].x;
                    float sampleY = ((y-halfHeight) / scale * frequency) + octaveOffsets[i].y;

                    //Using the points to get a perlin noise value, and multiplying it by the amplitude of this octave
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY)*2-1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }
                //Storing max and min noise height
                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }
                noiseMap[x, y] = noiseHeight;
            }
        }
        //Normalize the noise map using the maximum and minimum noise values
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }
        return noiseMap;
    }
}
