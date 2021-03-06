﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class MeshSettings : UpdateableData
{
    public float meshScale = 1;
    
    public bool useFlatShading;

    public const int levelOfSupportedLOD = 5;
    public const int numSupportedChunkSizes = 9;
    public const int numSupportedFlatChunkSizes = 3;
    public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

    [Range(0, numSupportedChunkSizes - 1)]
    public int chunkSizeIndex;
    [Range(0, numSupportedFlatChunkSizes - 1)]
    public int flatChunkSizeIndex;

    //The number of vertices per line of a mesh at the highest resolution. (LOD = 0). Includes 2 extra verts that are excluded from final mesh but used for normals
    public int numVertsPerLine
    {
        get
        {
            return supportedChunkSizes[(useFlatShading) ? flatChunkSizeIndex : chunkSizeIndex]+1;
        }
    }
    public float meshWorldSize
    {
        get
        {
            return (numVertsPerLine - 3) * meshScale;
        }
    }




}
