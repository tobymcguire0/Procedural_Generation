using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        float topLeftX = (width - 1) * -0.5f;
        float topLeftZ = (height - 1) * 0.5f;

        MeshData meshData = new MeshData(width, height);
        int vertexIndex = 0;

        for(int x = 0; x<width; x++)
        {
            for(int y = 0; y<height; y++)
            {
                //Creating the world vertex of the current point with a height corresponding to the multiplier and heightmap
                meshData.verticies[vertexIndex] = new Vector3(topLeftX+x, heightCurve.Evaluate(heightMap[x, y])*heightMultiplier, topLeftZ-y);
                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);
                //Ignore right/bottom edges of the map
                if(x<width-1 && y < height - 1)
                {
                    //Adding the indexes of the points that create the two triangles for that square
                    meshData.AddTriangle(vertexIndex,vertexIndex+width+1,vertexIndex+width);
                    meshData.AddTriangle(vertexIndex + width + 1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }
        //Returning the meshData instead of the Mesh to help with threading later on (?)
        return meshData;
    }
}

public class MeshData
{
    //Data of all the verticies of the mesh
    public Vector3[] verticies;

    //1D triangle array that stores vertex data to form triangles
    //Ex. (0,3,2 , 3,0,1) are two triangles in a 2x2 matrix that create one square
    public int[] triangles;

    //UVs used to handle textures. Contains the percentages of each vertex
    public Vector2[] uvs;

    //The current index of the triangles array
    int triangleIndex;
    public MeshData(int meshWidth, int meshHeight)
    {
        verticies = new Vector3[meshWidth*meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];

    }
    //Reversed the direction of the triangles because it was giving me an upside down mesh
    public void AddTriangle(int a, int b,int c)
    {
        triangles[triangleIndex] = c;
        triangles[triangleIndex+1] = b;
        triangles[triangleIndex+2] = a;
        triangleIndex += 3;

    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = verticies;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}
