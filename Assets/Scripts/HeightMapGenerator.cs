using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator
{
    public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCenter)
    {
        AnimationCurve heightCurve_threadsafe = new AnimationCurve(settings.heightCurve.keys);
        float[,] values = Noise.generateNoiseMap(width, height, settings.noiseSettings, sampleCenter);
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for(int x = 0; x<width; x++)
        {
            for(int y = 0; y<height; y++)
            {
                values[x, y] *= settings.heightCurve.Evaluate(values[x, y]) * settings.heightMultiplier;

                if (values[x, y] > maxValue)
                {
                    maxValue = values[x, y];
                }
                if (values[x, y] < minValue)
                {
                    minValue = values[x, y];
                }
            }
        }
        return new HeightMap(values, minValue, maxValue);
    }
}
public struct HeightMap
{
    public readonly float[,] values;
    public readonly float minValue;
    public readonly float maxValue;
    public HeightMap(float[,] val, float min, float max)
    {
        values = val;
        minValue = min;
        maxValue = max;
    }
}