using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdateableData
{
    public float uniformScale = 1;
    public bool useFalloff;
    public bool useFlatShading;
    public AnimationCurve meshHeightControl;
    public float meshHeightMultiplier;

    public float minHeight
    {
        get
        {
            return uniformScale * meshHeightMultiplier * meshHeightControl.Evaluate(0);
        }
    }

    public float maxHeight
    {
        get
        {
            return uniformScale * meshHeightMultiplier * meshHeightControl.Evaluate(1);
        }
    }

}
