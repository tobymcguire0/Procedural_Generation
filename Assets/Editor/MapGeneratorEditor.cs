using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(GenerateMap))]
public class MapGeneratorEditor : Editor
{
    //Adding a button to generate noise, or an option to do it automatically
    public override void OnInspectorGUI()
    {
        GenerateMap mapGen = (GenerateMap)target;

        //Automatic updating
        if (DrawDefaultInspector())
        {
            if (mapGen.autoUpdate)
            {
                mapGen.GenerateNewMap();
            }
        }
        //Manual updating
        if (GUILayout.Button("Generate"))
        {
            mapGen.GenerateNewMap();
        }

    }
}
