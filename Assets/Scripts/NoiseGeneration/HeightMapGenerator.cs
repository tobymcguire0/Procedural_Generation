﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator 
{
    public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCenter)
    {
        float[,] values = Noise.generateNoiseMap(width, height, settings.noiseSettings, sampleCenter);

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        AnimationCurve heightCurve_threadsafe = new AnimationCurve(settings.heightCurve.keys);
        for(int i = 0; i<width; i++)
        {
            for(int j = 0; j<height; j++)
            {
                values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j]) * settings.heightMultiplier;
                if (values[i, j] > maxValue)
                {
                    maxValue = values[i, j];
                }
                if (values[i, j] > minValue)
                {
                    minValue = values[i, j];
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
    public HeightMap(float[,] height, float min, float max)
    {
        values = height;
        minValue = min;
        maxValue = max;
    }
}