using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator
{
    //Creates and returns a point filter, no wrap texture from a color map.
    public static Texture2D texFromColorMap(Color[] colorMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();
        return texture;
    }
    //Same as color map, but first turns the height map into a color map to be used in texFromColorMap, then does the same thing
    public static Texture2D texFromHeightMap(float[,] heightMap, int width, int height)
    {
        Color[] colorMap = new Color[width * height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }
        return texFromColorMap(colorMap,width,height);
    }
}
